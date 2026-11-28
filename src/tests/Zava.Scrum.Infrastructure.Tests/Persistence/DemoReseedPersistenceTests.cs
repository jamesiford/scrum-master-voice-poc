using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Zava.Scrum.Application;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Demo.GetDemoReseed;
using Zava.Scrum.Application.Features.Demo.AbortDemoReseed;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Application.Features.Demo.ResetDemo;
using Zava.Scrum.Application.Features.Demo.OptInDemoScope;
using Zava.Scrum.Domain.Schedules;
using Zava.Scrum.Application.Features.Standups.StartStandup;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Ado;
using Zava.Scrum.Infrastructure.Demo;
using Zava.Scrum.Infrastructure.Persistence;
using Zava.Scrum.Infrastructure.Voice;

namespace Zava.Scrum.Infrastructure.Tests.Persistence;

/// <summary>Real mediator and file publication tests with read-only fake ADO; no cloud or existing local data is touched.</summary>
public sealed class DemoReseedPersistenceTests
{
    [Fact]
    public async Task Reseed_ItemStorage_AtomicallyHidesDemoRecordsWithoutGrowingPublication()
    {
        await using var fixture = new Fixture(itemStorage: true);
        await fixture.SeedAsync();
        var id = (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).Value;
        Assert.Equal(203, (await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default)).Count);
        Assert.Equal("demo.awaiting_apply", (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id))).Error!.Code);
        await fixture.MarkAppliedAsync(id);

        Assert.True((await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id))).IsSuccess);
        Assert.True((await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id))).IsSuccess);

        var state = await fixture.Publication.ReadAsync(default);
        Assert.False(state.Collections.ContainsKey("followups.json"));
        Assert.False(state.Collections.ContainsKey("cards.json"));
        Assert.False(state.Collections.ContainsKey("bot-activities.json"));
        Assert.Empty(state.Sessions.Values.SelectMany(s => s));
        Assert.Equal("real-team", Assert.Single(await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default)).TeamId);
        Assert.Empty(await fixture.Sessions.ListAsync("team-payments", default));
        Assert.Single(await fixture.Sessions.ListAsync("real-team", default));
        Assert.Equal(203, (await new FileTeamRecordStore(fixture.StoreOptions).QueryAsync("followups.json", null, null, null, null, default)).Count);
        fixture.AdvanceClock();
        var fresh = Session("team-payments") with { CreatedAt = new DateTimeOffset(2026, 11, 27, 0, 1, 0, TimeSpan.Zero) };
        await fixture.Sessions.UpsertAsync(fresh, default);
        Assert.Equal(fresh.Id, Assert.Single(await fixture.Sessions.ListAsync("team-payments", default)).Id);
        Assert.Empty((await fixture.Publication.ReadAsync(default)).Sessions.Values.SelectMany(s => s));
    }

    [Fact]
    public async Task Reseed_ApprovedAndApplied_PublishesOnlyExplicitDemoScopeAndSurvivesRestart()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var auditBefore = await fixture.SeedApprovalAsync();
        var staged = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision));
        Assert.True(staged.IsSuccess, staged.Error?.Message);
        var duplicate = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision));
        Assert.Equal(staged.Value, duplicate.Value);
        var before = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, staged.Value));
        Assert.Equal("demo.awaiting_apply", before.Error!.Code);
        Assert.Equal(203, (await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default)).Count);
        await fixture.MarkAppliedAsync(staged.Value);

        var commit = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, staged.Value));
        var replay = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, staged.Value));

        Assert.True(commit.IsSuccess, commit.Error?.Message);
        Assert.Equal(commit.Value, replay.Value);
        var restarted = new FileDemoStateStore(fixture.StoreOptions);
        var collections = new DemoPublicationCollectionStore(new FileCollectionStore(fixture.StoreOptions), restarted);
        var sessions = new DemoPublicationSessionStore(new FileSessionItemStore(fixture.StoreOptions), restarted, fixture.DemoOptions);
        var followups = await collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default);
        Assert.Equal("real-team", Assert.Single(followups).TeamId);
        var registry = await collections.ReadAsync<Dictionary<string, Team>>("teams.json", () => [], default);
        Assert.True(registry.ContainsKey("real-team"));
        Assert.False(registry.ContainsKey("team-b"));
        Assert.Equal(TeamStatus.Active, registry["team-payments"].Status);
        Assert.Empty(await sessions.ListAsync("team-payments", default));
        Assert.Empty(await sessions.ListAsync("team-b", default));
        Assert.Single(await sessions.ListAsync("real-team", default));
        var roster = await collections.ReadAsync<Dictionary<string, JsonElement>>("members.json", () => [], default);
        Assert.Equal(3, roster["team-payments"].GetArrayLength());
        Assert.True(roster.ContainsKey("real-team"));
        Assert.Equal("Completed", (await fixture.Sender.Send(new GetDemoReseedQuery("team-payments", staged.Value, fixture.Decision))).Value.State);
        Assert.Equal(auditBefore, await File.ReadAllBytesAsync(Path.Combine(fixture.StoreOptions.Value.DataPath, "approval-records.jsonl")));
        var history = new PersistentDemoTeamRemovalHistory(restarted);
        Assert.True(await history.IsRemovedTeamAsync("team-b", default));
        Assert.False(await history.IsRemovedTeamAsync("team-payments", default));
        Assert.False(await history.IsRemovedTeamAsync("real-team", default));
    }

    [Fact]
    public async Task Commit_FailureAfterMutationBeforeAtomicPublication_PreservesOldSnapshotAndCanRetry()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var id = (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).Value;
        await fixture.MarkAppliedAsync(id);
        fixture.Publication.FailAfterMutation = true;

        var failed = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id));

        Assert.False(failed.IsSuccess);
        Assert.Equal(203, (await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default)).Count);
        Assert.Single(await fixture.Sessions.ListAsync("team-payments", default));
        var state = await new FileDemoStateStore(fixture.StoreOptions).ReadAsync(default);
        Assert.False(state.Operations[id].Completed);
        fixture.Publication.FailAfterMutation = false;
        Assert.True((await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id))).IsSuccess);
    }

    [Fact]
    public async Task Stage_UnmarkedOrForeignAllowlistedDebris_FailsClosedWithoutJournalOrWrites()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var debris = await fixture.Teams.GetAsync("team-b", default);
        await fixture.Teams.SaveAsync(debris! with { IsDemo = false }, default);

        var unmarked = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision));
        await fixture.Teams.SaveAsync(debris! with { DemoTenantId = Guid.NewGuid() }, default);
        var foreign = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision));

        Assert.Equal("demo.reset_scope_refused", unmarked.Error!.Code);
        Assert.Equal("demo.reset_scope_refused", foreign.Error!.Code);
        Assert.Empty((await fixture.Publication.ReadAsync(default)).Operations);
        Assert.Equal(202, (await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default)).Count);
    }

    [Fact]
    public async Task Stage_InFlightCollectionWriteAndDemoSessionWrites_CannotResurrectResetData()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var stale = await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default);
        var id = (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).Value;

        await Assert.ThrowsAsync<DemoResetConflictException>(() => fixture.Collections.WriteAsync("followups.json", stale, default));
        await Assert.ThrowsAsync<DemoResetConflictException>(() => fixture.Sessions.UpsertAsync(Session("team-payments"), default));
        await fixture.MarkAppliedAsync(id);
        Assert.True((await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id))).IsSuccess);
        await Assert.ThrowsAsync<DemoResetConflictException>(() => fixture.Collections.WriteAsync("followups.json", stale, default));
    }

    [Fact]
    public async Task Stage_UnrelatedTeamWritesRemainAllowed_AndUndecidedAbortReleasesGuard()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var id = (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).Value;
        using var repository = new JsonFileFollowUpRepository(fixture.Collections);
        var real = (await repository.ListAsync("real-team", null, default)).Single();
        real.MarkSent();
        await repository.SaveAsync(real, default);
        await fixture.Sessions.UpsertAsync(Session("real-team"), default);

        var abort = await fixture.Sender.Send(new AbortDemoReseedCommand("team-payments", id, fixture.Decision));
        var replay = await fixture.Sender.Send(new AbortDemoReseedCommand("team-payments", id, fixture.Decision));

        Assert.True(abort.IsSuccess);
        Assert.Equal(abort.Value, replay.Value);
        Assert.Equal(202, (await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default)).Count);
        Assert.Equal(FollowUpStatus.Sent, (await repository.GetAsync(real.Id, default))!.Status);
        Assert.Equal(TeamStatus.Active, (await fixture.Teams.GetAsync("team-payments", default))!.Status);
        await fixture.Sessions.UpsertAsync(Session("team-payments"), default);
        Assert.Equal("Aborted", (await fixture.Sender.Send(new GetDemoReseedQuery("team-payments", id, fixture.Decision))).Value.State);
    }

    [Fact]
    public async Task Abort_AppliedReset_MustResumeWithoutReleasingGuard()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var id = (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).Value;
        await fixture.MarkAppliedAsync(id);

        var abort = await fixture.Sender.Send(new AbortDemoReseedCommand("team-payments", id, fixture.Decision));

        Assert.Equal("demo.reset_must_resume", abort.Error!.Code);
        Assert.False((await fixture.Publication.ReadAsync(default)).Operations[id].Completed);
        Assert.True((await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id))).IsSuccess);
    }

    [Fact]
    public async Task Stage_UnselectedRealTeamSharesAdoArea_RefusesBeforeJournal()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var target = (await fixture.Teams.GetAsync("team-payments", default))!;
        var real = (await fixture.Teams.GetAsync("real-team", default))!;
        await fixture.Teams.SaveAsync(real with { Ado = target.Ado }, default);

        var refused = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision));

        Assert.Equal("demo.shared_ado_scope", refused.Error!.Code);
        Assert.Empty((await fixture.Publication.ReadAsync(default)).Operations);
    }

    [Fact]
    public async Task Apply_ExistingApprovedOrdinaryChangeDuringStagedReset_RefusesBeforeDelegationOrAdo()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        Assert.True((await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).IsSuccess);
        var target = (await fixture.Teams.GetAsync("team-payments", default))!;
        var old = FollowUp.Create(Guid.NewGuid(), target.Id, Guid.NewGuid(), null, FollowUpKind.Followup, "ordinary",
            [new ProposedChange(1, ChangeOp.State, "Active", "ordinary")], new DateOnly(2026, 11, 27), DateTimeOffset.UnixEpoch);
        old.Approve(fixture.Decision.Actor, DateTimeOffset.UnixEpoch);
        var context = new DelegatedWriteContext(fixture.Decision.Actor, _ => throw new InvalidOperationException("Delegation must not be acquired."));

        var result = await fixture.Writer.ApplyAsync(old.GetApprovedChanges().Value.Single(), target.Ado, context, default);

        Assert.Equal("demo.reset_write_guard", result.Error!.Code);
    }

    [Fact]
    public async Task OptIn_ExactReviewedSeedAndNamedDebris_AtomicallyMarksOnlyScopeAndIsIdempotent()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        foreach (var id in new[] { "team-payments", "team-b" })
        {
            var team = (await fixture.Teams.GetAsync(id, default))!;
            await fixture.Teams.SaveAsync(team with { IsDemo = false, DemoTenantId = null }, default);
        }
        var realBefore = (await fixture.Teams.GetAsync("real-team", default))!;

        var result = await fixture.Sender.Send(new OptInDemoScopeCommand(fixture.Manifest, fixture.Decision));
        var epoch = (await fixture.Publication.ReadAsync(default)).Epoch;
        var replay = await fixture.Sender.Send(new OptInDemoScopeCommand(fixture.Manifest, fixture.Decision));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.True(replay.IsSuccess);
        Assert.Equal(epoch, (await fixture.Publication.ReadAsync(default)).Epoch);
        Assert.True((await fixture.Teams.GetAsync("team-payments", default))!.IsDemo);
        Assert.True((await fixture.Teams.GetAsync("team-b", default))!.IsDemo);
        Assert.Equal(realBefore, await fixture.Teams.GetAsync("real-team", default));
    }

    [Fact]
    public async Task OptIn_ChangedFingerprintOrForeignTenant_RefusesWithoutPartialMarkers()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var target = (await fixture.Teams.GetAsync("team-payments", default))!;
        await fixture.Teams.SaveAsync(target with { IsDemo = false, DemoTenantId = null }, default);
        var debris = (await fixture.Teams.GetAsync("team-b", default))!;
        await fixture.Teams.SaveAsync(debris with { DemoTenantId = Guid.NewGuid() }, default);

        var foreign = await fixture.Sender.Send(new OptInDemoScopeCommand(fixture.Manifest, fixture.Decision));
        var changed = await fixture.Sender.Send(new OptInDemoScopeCommand(fixture.Manifest with { Organisation = "https://dev.azure.com/other" }, fixture.Decision));

        Assert.Equal("demo.manifest_changed", foreign.Error!.Code);
        Assert.Equal("demo.manifest_refused", changed.Error!.Code);
        Assert.False((await fixture.Teams.GetAsync("team-payments", default))!.IsDemo);
    }

    [Fact]
    public async Task LegacyReset_ExplicitDemoScope_PreservesRealDataRegistrySchedulesAndAppendOnlyAudit()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var auditBefore = await fixture.SeedApprovalAsync();
        var day = new DateOnly(2026, 11, 27);
        await fixture.Collections.WriteAsync("schedules.json", new List<Schedule>
        {
            Schedule.Default("team-payments") with { PreparedFor = day, StartedFor = day },
            Schedule.Default("real-team") with { PreparedFor = day, StartedFor = day },
        }, default);

        var result = await fixture.Sender.Send(new ResetDemoCommand(fixture.Decision));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("real-team", Assert.Single(await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default)).TeamId);
        Assert.Single(await fixture.Sessions.ListAsync("real-team", default));
        Assert.Empty(await fixture.Sessions.ListAsync("team-payments", default));
        Assert.NotNull(await fixture.Teams.GetAsync("team-b", default));
        var schedules = await fixture.Collections.ReadAsync<List<Schedule>>("schedules.json", () => [], default);
        Assert.Null(schedules.Single(s => s.TeamId == "team-payments").PreparedFor);
        Assert.Equal(day, schedules.Single(s => s.TeamId == "real-team").PreparedFor);
        Assert.Equal(auditBefore, await File.ReadAllBytesAsync(Path.Combine(fixture.StoreOptions.Value.DataPath, "approval-records.jsonl")));
    }

    [Fact]
    public async Task LegacyReset_UnmarkedScopeOrRunningTake_FailsClosed()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var target = (await fixture.Teams.GetAsync("team-payments", default))!;
        await fixture.Teams.SaveAsync(target with { IsDemo = false }, default);
        var unmarked = await fixture.Sender.Send(new ResetDemoCommand(fixture.Decision));
        await fixture.Teams.SaveAsync(target, default);
        await fixture.Sessions.UpsertAsync(Session(target.Id) with { Status = StandupStatus.Live, StartedAt = DateTimeOffset.UnixEpoch }, default);
        var running = await fixture.Sender.Send(new ResetDemoCommand(fixture.Decision));

        Assert.Equal("demo.reset_scope_refused", unmarked.Error!.Code);
        Assert.Equal("demo.session_running", running.Error!.Code);
        Assert.Equal(202, (await fixture.Collections.ReadAsync<List<FollowUpSnapshot>>("followups.json", () => [], default)).Count);
    }

    [Fact]
    public async Task Stage_RunningTake_RefusesAndStagedStartReturnsExplicitConflict()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var old = (await fixture.Sessions.ListAsync("team-payments", default)).Single();
        await fixture.Sessions.UpsertAsync(old with { Status = StandupStatus.Live, StartedAt = DateTimeOffset.UnixEpoch }, default);
        var running = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision));
        Assert.Equal("demo.session_running", running.Error!.Code);
        await fixture.Sessions.UpsertAsync(old, default);
        var id = (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).Value;

        var started = await fixture.Sender.Send(new StartStandupCommand(old.Id, "simulated"));

        Assert.Equal("demo.reset_in_progress", started.Error!.Code);
        fixture.AdvanceClock();
        var preparedDuringGuard = Session("team-payments") with { CreatedAt = new DateTimeOffset(2026, 11, 27, 0, 1, 0, TimeSpan.Zero) };
        fixture.AdvanceClock();
        await fixture.MarkAppliedAsync(id);
        Assert.True((await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id))).IsSuccess);
        await Assert.ThrowsAsync<DemoResetConflictException>(() => fixture.Sessions.UpsertAsync(old, default));
        await Assert.ThrowsAsync<DemoResetConflictException>(() => fixture.Sessions.UpsertAsync(Session("team-payments"), default));
        await Assert.ThrowsAsync<DemoResetConflictException>(() => fixture.Sessions.UpsertAsync(preparedDuringGuard, default));
    }

    [Fact]
    public async Task ApplyAndCommit_RunningTakeInGuardedState_RefuseBeforeAdoOrPublication()
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var id = (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).Value;
        await fixture.MarkAppliedAsync(id);
        var state = await fixture.Publication.ReadAsync(default);
        var operation = state.Operations[id];
        var proposal = FollowUp.Restore(operation.Approval);
        Assert.True(proposal.Approve(fixture.Decision.Actor, DateTimeOffset.UnixEpoch, ApprovalChannel.Portal).IsSuccess);
        var approved = proposal.GetApprovedChanges().Value[0];
        await fixture.Publication.UpdateAsync(current =>
        {
            current.Sessions["team-payments"] = [Session("team-payments") with { Status = StandupStatus.Live, StartedAt = DateTimeOffset.UnixEpoch }];
            return true;
        }, default);
        var context = new DelegatedWriteContext(fixture.Decision.Actor, _ => throw new InvalidOperationException("No delegation may be acquired."));

        var applied = await fixture.Writer.ApplyAsync(approved, operation.Target.Ado, context, default);
        var committed = await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision, id));

        Assert.Equal("demo.session_running", applied.Error!.Code);
        Assert.Equal("demo.session_running", committed.Error!.Code);
        Assert.False((await fixture.Publication.ReadAsync(default)).Operations[id].Completed);
    }

    [Theory]
    [InlineData("Org")]
    [InlineData("Project")]
    [InlineData("Team")]
    [InlineData("AreaPath")]
    public async Task ApplyAndCommit_ConnectionChangedAfterStage_RefuseBeforeDelegationAndPublication(string field)
    {
        await using var fixture = new Fixture();
        await fixture.SeedAsync();
        var id = (await fixture.Sender.Send(new ReseedDemoCommand("team-payments", fixture.Decision))).Value;
        await fixture.MarkAppliedAsync(id);
        var operation = (await fixture.Publication.ReadAsync(default)).Operations[id];
        var original = operation.Target;
        var changed = original.Ado with
        {
            Org = field == "Org" ? "https://dev.azure.com/real" : original.Ado.Org,
            Project = field == "Project" ? "Real-Project" : original.Ado.Project,
            Team = field == "Team" ? "Real Team" : original.Ado.Team,
            AreaPath = field == "AreaPath" ? "Real-Project\\Real" : original.Ado.AreaPath,
        };
        await Assert.ThrowsAsync<DemoResetConflictException>(() => fixture.Teams.SaveAsync(original with { Ado = changed }, default));
        // Simulate an out-of-band connection edit despite the normal repository guard.
        await fixture.Publication.UpdateAsync(state =>
        {
            var registry = state.Collections["teams.json"].Deserialize<Dictionary<string, Team>>(JsonFile.SerializerOptions)!;
            registry[original.Id] = registry[original.Id] with { Ado = changed };
            state.Collections["teams.json"] = JsonSerializer.SerializeToElement(registry, JsonFile.SerializerOptions);
            return true;
        }, default);
        var followup = FollowUp.Restore(operation.Approval);
        Assert.True(followup.Approve(fixture.Decision.Actor, DateTimeOffset.UnixEpoch, ApprovalChannel.Portal).IsSuccess);
        var context = new DelegatedWriteContext(fixture.Decision.Actor, _ => throw new InvalidOperationException("No delegation or ADO route may be acquired."));

        var apply = await fixture.Writer.ApplyAsync(followup.GetApprovedChanges().Value[0], changed, context, default);
        var commit = await fixture.Sender.Send(new ReseedDemoCommand(original.Id, fixture.Decision, id));

        Assert.Equal("demo.reset_changed", apply.Error!.Code);
        Assert.Equal("demo.reset_changed", commit.Error!.Code);
        Assert.False((await fixture.Publication.ReadAsync(default)).Operations[id].Completed);
    }

    private static StandupSessionSnapshot Session(string team) =>
        new(Guid.NewGuid(), team, DateTimeOffset.UnixEpoch, StandupStatus.Prepared, null, null, null, 15, null, null,
            new Dictionary<string, Attendance>(), [], [], []);

    private sealed class Fixture : IAsyncDisposable
    {
        private static readonly string[] PreservedRoster = ["unchanged"];
        private readonly string _path = Path.Combine(FindRoot(), ".m11-plan", "reset-test-" + Guid.NewGuid().ToString("N"));
        private readonly ServiceProvider _provider;
        private readonly Factory _factory;
        private readonly DemoAdoResetGate _gate = new();
        private readonly Clock _clock = new();
        public IOptions<StoreOptions> StoreOptions { get; }
        public IOptions<DemoOptions> DemoOptions { get; }
        public FaultStore Publication { get; }
        public ICollectionStore Collections { get; }
        private readonly DemoPublicationCollectionStore _legacyCollections;
        private readonly TeamRecordMigration? _migration;
        public DemoPublicationSessionStore Sessions { get; }
        public JsonFileTeamRepository Teams { get; }
        public ISender Sender => _provider.GetRequiredService<ISender>();
        public RoutedWorkItemWriter Writer => new(_factory, Publication, _gate);
        public DemoResetScopeManifest Manifest => new(Decision.Actor.TenantId, "https://dev.azure.com/demo", ["team-payments"], ["team-b"],
            [new("team-payments", DemoAdo), new("team-b", DemoAdo)]);
        private static AdoSettings DemoAdo => new("https://dev.azure.com/demo", "Zava-Scrum-Demo", "Payments (Demo)", "Zava-Scrum-Demo\\Payments");
        public DecisionContext Decision { get; } = new(new ActorIdentity(Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Admin", "admin@example.invalid", ActorSource.Portal, "entra-bearer"),
            ApprovalChannel.Portal, AuthenticationContext.Of("entra-bearer"), [AppRoles.Admin]);

        public Fixture(bool itemStorage = false)
        {
            StoreOptions = Options.Create(new StoreOptions { DataPath = _path });
            DemoOptions = Options.Create(new DemoOptions
            {
                ResetTenantId = Decision.Actor.TenantId,
                ResetTeamIds = ["team-payments", "team-b"],
                SeedFile = Path.Combine(FindRoot(), "demo", "seed-ado.json")
            });
            Publication = new FaultStore(new FileDemoStateStore(StoreOptions));
            _legacyCollections = new DemoPublicationCollectionStore(new FileCollectionStore(StoreOptions), Publication);
            var items = new FileTeamRecordStore(StoreOptions);
            Collections = itemStorage ? new RecordCollectionStore(_legacyCollections, items, Publication) : _legacyCollections;
            if (itemStorage)
                _migration = new TeamRecordMigration(_legacyCollections, items, Publication, new FileSessionItemStore(StoreOptions), NullLogger<TeamRecordMigration>.Instance);
            Sessions = new DemoPublicationSessionStore(new FileSessionItemStore(StoreOptions), Publication, DemoOptions);
            var ado = Options.Create(new AdoOptions { Org = "https://dev.azure.com/demo" });
            Teams = new JsonFileTeamRepository(Collections, Options.Create(new TeamDefaultsOptions()), ado, Options.Create(new FlagsOptions()));
            _factory = new Factory(DemoOptions.Value.SeedFile);
            var plan = new DemoReseedPlan(_factory, DemoOptions, ado, Options.Create(new AzureOptions { TenantId = Decision.Actor.TenantId.ToString() }),
                Options.Create(new TeamDefaultsOptions()));
            var reset = new PersistentDemoReseed(Collections, Publication, Teams, new SeedFileRosterSource(DemoOptions), Sessions, plan, _clock, DemoOptions, _gate);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddSingleton<IDemoReseed>(reset);
            services.AddSingleton<ISessionRepository>(new SessionRepository(Sessions));
            services.AddSingleton<ITeamRepository>(Teams);
            services.AddSingleton<IRetroSettings>(new RetroOptions());
            services.AddSingleton<IClock>(_clock);
            _provider = services.BuildServiceProvider();
        }

        public async Task SeedAsync()
        {
            var target = new Team("team-payments", "Payments (Demo)", "UTC", 15,
                new AdoSettings("https://dev.azure.com/demo", "Zava-Scrum-Demo", "Payments (Demo)", "Zava-Scrum-Demo\\Payments"),
                new TeamBehaviours(), "goal", IsDemo: true, DemoTenantId: Decision.Actor.TenantId);
            await Teams.SaveAsync(target, default);
            await Teams.SaveAsync(target with { Id = "team-b" }, default);
            await Teams.SaveAsync(target with
            {
                Id = "real-team",
                IsDemo = false,
                DemoTenantId = Guid.NewGuid(),
                Ado = new AdoSettings(target.Ado.Org, "Real-Project", "Real Team", "Real-Project\\Area")
            }, default);
            var followups = Enumerable.Range(0, 201).Select(i => FollowUp.Create(Guid.NewGuid(), i < 191 ? "team-payments" : "team-b",
                Guid.NewGuid(), null, FollowUpKind.Followup, "debris", [], new DateOnly(2026, 11, 27), DateTimeOffset.UnixEpoch).ToSnapshot()).ToList();
            followups.Add(FollowUp.Create(Guid.NewGuid(), "real-team", Guid.NewGuid(), null, FollowUpKind.Followup, "preserve", [],
                new DateOnly(2026, 11, 27), DateTimeOffset.UnixEpoch).ToSnapshot());
            await _legacyCollections.WriteAsync("followups.json", followups, default);
            await Collections.WriteAsync("members.json", new Dictionary<string, object> { ["real-team"] = PreservedRoster }, default);
            foreach (var team in new[] { "team-payments", "team-b", "real-team" })
            {
                await Sessions.UpsertAsync(Session(team), default);
            }
            if (_migration is not null) await _migration.RunAsync(default);
        }

        public void AdvanceClock() => _clock.UtcNow = _clock.UtcNow.AddMinutes(1);

        public async Task MarkAppliedAsync(Guid id)
        {
            var dto = (await Sender.Send(new GetDemoReseedQuery("team-payments", id, Decision))).Value;
            using var repository = new JsonFileFollowUpRepository(Collections);
            var followUp = (await repository.GetAsync(dto.FollowUpId, default))!;
            Assert.True(followUp.Approve(Decision.Actor, DateTimeOffset.UnixEpoch, ApprovalChannel.Portal).IsSuccess);
            Assert.True(followUp.GetApprovedChanges().IsSuccess);
            followUp.MarkApplied(DateTimeOffset.UnixEpoch);
            await repository.SaveAsync(followUp, default);
        }

        public async Task<byte[]> SeedApprovalAsync()
        {
            using var records = new FileApprovalRecordStore(StoreOptions);
            await records.AppendAsync(new ApprovalRecord(Guid.NewGuid(), "demo-correlation", Guid.NewGuid(), "team-payments",
                ApprovalRecordKind.Approved, "original-hash", Decision.Actor, Decision.Channel, DateTimeOffset.UnixEpoch,
                Decision.Authentication, null, []), default);
            return await File.ReadAllBytesAsync(Path.Combine(StoreOptions.Value.DataPath, "approval-records.jsonl"));
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            _factory.Dispose();
            _gate.Dispose();
            Teams.Dispose();
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, recursive: true);
            }
        }
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 27, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class FaultStore(IDemoStateStore inner) : IDemoStateStore
    {
        public bool FailAfterMutation { get; set; }
        public Task<DemoState> ReadAsync(CancellationToken ct) => inner.ReadAsync(ct);
        public Task<T> UpdateAsync<T>(Func<DemoState, T> update, CancellationToken ct) => inner.UpdateAsync(state =>
        {
            var result = update(state);
            if (FailAfterMutation)
            {
                throw new IOException("Injected interruption before atomic publication.");
            }

            return result;
        }, ct);
    }

    private sealed class Factory : IAdoClientFactory, IDisposable
    {
        private readonly HttpClient _http;
        public Factory(string seed) => _http = new HttpClient(new Transport(seed)) { BaseAddress = new Uri("https://dev.azure.com/demo/") };
        public HttpClient For(string organisationUrl) => _http;
        public AdoRestClient ReaderFor(string organisationUrl) => throw new NotSupportedException();
        public AdoWorkItemWriter DelegatedWriterFor(string organisationUrl, string accessToken) => throw new NotSupportedException();
        public HttpClient DelegatedClientFor(string organisationUrl, string accessToken) => throw new NotSupportedException();
        public void Dispose() => _http.Dispose();
    }

    private sealed class Transport : HttpMessageHandler
    {
        private readonly List<string> _keys;
        public Transport(string seed)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(seed));
            _keys = doc.RootElement.GetProperty("workItems").EnumerateArray().Select(i => i.GetProperty("key").GetString()!).ToList();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var json = path.Contains("classificationnodes", StringComparison.Ordinal) ? "{}"
                : request.Method == HttpMethod.Post ? JsonSerializer.Serialize(new { workItems = Enumerable.Range(1, _keys.Count).Select(id => new { id }) })
                : JsonSerializer.Serialize(new
                {
                    fields = new Dictionary<string, string>
                    {
                        [AdoFields.Tags] = "zava-demo; demo-key-" + _keys[int.Parse(path.Split('/')[^1], System.Globalization.CultureInfo.InvariantCulture) - 1],
                        ["System.TeamProject"] = "Zava-Scrum-Demo",
                        [AdoFields.AreaPath] = "Zava-Scrum-Demo\\Payments",
                    }
                });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ZavaScrum.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
