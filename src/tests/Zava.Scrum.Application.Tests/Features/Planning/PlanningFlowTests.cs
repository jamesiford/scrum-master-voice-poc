using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Planning.ApprovePlan;
using Zava.Scrum.Application.Features.Planning.GetPlanningPack;
using Zava.Scrum.Application.Features.Planning.PreparePlanning;
using Zava.Scrum.Application.Features.Planning.RejectPlan;
using Zava.Scrum.Application.Features.Teams.UpdateMemberAvailability;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Planning;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features.Planning;

public sealed class PlanningFlowTests : IAsyncDisposable
{
    private static readonly DateTimeOffset T = StandupHarness.Now;
    private readonly WriterSpy _writer = new();
    private readonly RecordingTeamsMessenger _messenger = new();
    private readonly InMemoryPlanningPackRepository _packs = new();
    private readonly InMemoryFollowUpRepository _followUps = new();
    private readonly InMemoryMemberRepository _members = new();
    private ServiceProvider _sp = null!;

    private ISender Sender => _sp.GetRequiredService<ISender>();

    private static WorkItem Item(int id, double points, string state, string? owner, string? criteria = "Given, when, then") =>
        new(new WorkItemId(id), $"Item {id}", "User Story", state, null, owner, T, T, points, null, null, [], null, criteria);

    private async Task StartAsync(string? goalReply = null, string? nextIteration = "P\\Sprint 43")
    {
        var sprint = new SprintSnapshot("P\\Sprint 42", T, T, [Item(1, 5, "Active", "daniel"), Item(2, 8, "Active", "maya")]);
        IReadOnlyList<WorkItem> backlog = [Item(10, 5, "New", null), Item(11, 5, "New", "maya"), Item(12, 5, "New", null), Item(13, 5, "New", null), Item(14, 5, "New", null), Item(15, 5, "New", null), Item(16, 13, "New", null), Item(17, 3, "New", null, criteria: null)];
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "Payments", "UTC", 15, new AdoSettings("o", "P", "t", "a"), new TeamBehaviours(), "Goal"), default);
        await _members.ReplaceAsync("t1", [
            new Member("m-daniel", "t1", "daniel", "Daniel Park", "daniel@x.com", "Daniel", 1, true),
            new Member("m-maya", "t1", "maya", "Maya Lin", null, "Maya", 2, true),
            new Member("m-admin", "t1", "admin", "Admin", null, "Admin", 3, true),
        ], default);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IMemberRepository>(_members);
        services.AddSingleton<IWorkItemReader>(new FakeWorkItemReader(Result<SprintSnapshot>.Success(sprint)));
        services.AddSingleton<IBacklogReader>(new FakeBacklogReader(backlog));
        services.AddSingleton<IIterationReader>(new FakeIterationReader(nextIteration));
        services.AddSingleton<IKpiHistorySource>(new FakeKpiHistorySource(32, 35, 34, 40, 38, 40));
        services.AddSingleton<IDraftingAgent>(new FakeDraftingAgent(goalReply));
        services.AddSingleton<IPlanningPackRepository>(_packs);
        services.AddSingleton<IFollowUpRepository>(_followUps);
        services.AddSingleton<ITeamsMessenger>(_messenger);
        services.AddSingleton<IWorkItemWriter>(_writer);
        services.AddSingleton<IClock>(new FixedClock(T));
        _sp = services.BuildServiceProvider();
    }

    private async Task<PlanningPackData> PrepareAsync() =>
        (await Sender.Send(new GetPlanningPackQuery("t1", (await Sender.Send(new PreparePlanningCommand("t1"))).Value))).Value;

    [Fact]
    public async Task Prepare_StoresThePackAndSendsOnePlanningCardWithoutWriting()
    {
        await StartAsync();

        var pack = await PrepareAsync();

        Assert.Equal(PlanningPackStatus.Prepared, pack.Status);
        Assert.Equal([1, 2, 10, 11, 12, 13, 14], pack.Proposal.Selected.Select(s => s.WorkItemId));
        Assert.Equal(38, pack.Proposal.TotalPoints);
        Assert.Equal([15, 16, 17], pack.Proposal.Excluded.Select(e => e.WorkItemId).Order());
        Assert.True(pack.VelocityIllustrative);
        var card = Assert.Single(_messenger.Sent);
        Assert.Equal((CardKind.PlanningPack, $"planning:{pack.Id}", (Guid?)pack.Id), (card.Kind, card.DedupeKey, card.FollowUpId));
        Assert.Empty(_writer.Written);
        Assert.Empty(await _followUps.ListAsync("t1", null, default));
    }

    [Fact]
    public async Task Prepare_NoModel_UsesTheCodeGoal()
    {
        await StartAsync();

        var pack = await PrepareAsync();

        Assert.False(pack.GoalFromModel);
        Assert.StartsWith("Finish the carry-over work and deliver Item 10", pack.DraftGoal, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Prepare_ModelGoalUsingOnlyKnownNumbers_IsUsed()
    {
        await StartAsync("{\"goal\":\"Finish the carry-over and deliver Item 10 and Item 11.\"}");

        var pack = await PrepareAsync();

        Assert.True(pack.GoalFromModel);
        Assert.Equal("Finish the carry-over and deliver Item 10 and Item 11.", pack.DraftGoal);
    }

    [Theory]
    [InlineData("{\"goal\":\"Ship story 9999 this sprint.\"}")]
    [InlineData("{\"goal\":\"Deliver 77 points.\"}")]
    [InlineData("not json")]
    public async Task Prepare_ModelGoalWithAnInventedIdOrNumber_FallsBackToTheCodeGoal(string reply)
    {
        await StartAsync(reply);

        var pack = await PrepareAsync();

        Assert.False(pack.GoalFromModel);
        Assert.StartsWith("Finish the carry-over work", pack.DraftGoal, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Approve_WritesMoveAndAssignWithAttributionOnlyAfterApprovalAndUpdatesTheCard()
    {
        await StartAsync();
        var pack = await PrepareAsync();
        Assert.Empty(_writer.Written);

        var result = await Sender.Send(new ApprovePlanCommand(pack.Id, "Sam", [], ApprovalChannel.Teams));

        Assert.True(result.IsSuccess);
        Assert.Equal(pack.Proposal.Selected.Count, _writer.Written.Count(w => w.Op == ChangeOp.SetIteration));
        Assert.All(_writer.Written.Where(w => w.Op == ChangeOp.SetIteration), w => Assert.Equal("P\\Sprint 43", w.Value));
        var assigned = _writer.Written.Where(w => w.Op == ChangeOp.Assign).ToList();
        Assert.Equal(pack.Proposal.Selected.Count(s => s.OwnerSuggested), assigned.Count);
        Assert.All(assigned, a => Assert.NotNull(a.OwnerKey));
        Assert.All(_writer.Written, w => Assert.Equal("Approved by Sam in Teams, proposed by Zava Scrum Agent from the 2026-01-08 sprint planning.", w.Attribution));
        var stored = (await _packs.GetAsync(pack.Id, default))!.Data;
        Assert.Equal((PlanningPackStatus.Approved, "Sam", (Guid?)result.Value), (stored.Status, stored.DecidedBy, stored.FollowUpId));
        var followUp = (await _followUps.GetAsync(result.Value, default))!;
        Assert.Equal((FollowUpStatus.Applied, true, CeremonyKind.SprintPlanning), (followUp.Status, followUp.ForScrumMaster, followUp.Ceremony));
        var update = Assert.Single(_messenger.Updated);
        Assert.Equal($"planning:{pack.Id}", update.DedupeKey);
        Assert.Contains("written to Azure DevOps", update.Facts[^1].Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Approve_ASubset_WritesOnlyThoseItems()
    {
        await StartAsync();
        var pack = await PrepareAsync();

        await Sender.Send(new ApprovePlanCommand(pack.Id, "Sam", [1, 10], ApprovalChannel.Portal));

        Assert.Equal([1, 10], _writer.Written.Where(w => w.Op == ChangeOp.SetIteration).Select(w => w.WorkItemId));
        Assert.All(_writer.Written, w => Assert.Equal("Approved by Sam in the portal, proposed by Zava Scrum Agent from the 2026-01-08 sprint planning.", w.Attribution));
    }

    [Fact]
    public async Task Approve_AnItemOutsideThePlan_IsRefusedAndNothingIsWritten()
    {
        await StartAsync();
        var pack = await PrepareAsync();

        var result = await Sender.Send(new ApprovePlanCommand(pack.Id, "Sam", [16], ApprovalChannel.Portal));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(_writer.Written);
        Assert.Equal(PlanningPackStatus.Prepared, (await _packs.GetAsync(pack.Id, default))!.Data.Status);
    }

    [Fact]
    public async Task Approve_WithoutAName_IsInvalidAndNothingIsWritten()
    {
        await StartAsync();
        var pack = await PrepareAsync();

        var result = await Sender.Send(new ApprovePlanCommand(pack.Id, " ", [], ApprovalChannel.Portal));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Approve_Twice_WritesOnce()
    {
        await StartAsync();
        var pack = await PrepareAsync();
        await Sender.Send(new ApprovePlanCommand(pack.Id, "Sam", [], ApprovalChannel.Portal));
        var written = _writer.Written.Count;

        var again = await Sender.Send(new ApprovePlanCommand(pack.Id, "Sam", [], ApprovalChannel.Portal));

        Assert.Equal(ErrorType.Conflict, again.Error!.Type);
        Assert.Equal(written, _writer.Written.Count);
    }

    [Fact]
    public async Task Approve_NoNextIteration_IsAConflictAndNothingIsWritten()
    {
        await StartAsync(nextIteration: null);
        var pack = await PrepareAsync();

        var result = await Sender.Send(new ApprovePlanCommand(pack.Id, "Sam", [], ApprovalChannel.Portal));

        Assert.Equal("planning.no_next_iteration", result.Error!.Code);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Approve_WriterRejects_StaysApprovedAndTheCardShowsTheError()
    {
        await StartAsync();
        var pack = await PrepareAsync();
        _writer.Fail = true;

        var result = await Sender.Send(new ApprovePlanCommand(pack.Id, "Sam", [], ApprovalChannel.Teams));

        Assert.True(result.IsSuccess);
        Assert.Equal(FollowUpStatus.Approved, (await _followUps.GetAsync(result.Value, default))!.Status);
        Assert.Contains("write failed", _messenger.Updated[^1].Facts[^1].Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reject_WritesNothingCreatesNoFollowUpAndUpdatesTheCard()
    {
        await StartAsync();
        var pack = await PrepareAsync();

        var result = await Sender.Send(new RejectPlanCommand(pack.Id, "Sam"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_writer.Written);
        Assert.Empty(await _followUps.ListAsync("t1", null, default));
        Assert.Equal(PlanningPackStatus.Rejected, (await _packs.GetAsync(pack.Id, default))!.Data.Status);
        Assert.Contains("Nothing was written", Assert.Single(_messenger.Updated).Facts[^1].Value, StringComparison.Ordinal);
        Assert.Equal(ErrorType.Conflict, (await Sender.Send(new ApprovePlanCommand(pack.Id, "Sam", [], ApprovalChannel.Portal))).Error!.Type);
    }

    [Fact]
    public async Task Prepare_AfterLoweringAvailability_ShrinksThatMembersCapacity()
    {
        await StartAsync();
        await Sender.Send(new UpdateMemberAvailabilityCommand("t1", "m-maya", 50));

        var pack = await PrepareAsync();

        var maya = pack.Proposal.Members.Single(m => m.MemberId == "m-maya");
        Assert.Equal((50, 6.7), (maya.AvailabilityPct, maya.CapacityHigh));
        Assert.Contains(pack.Proposal.Warnings, w => w.StartsWith("Maya Lin has", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task UpdateAvailability_OutOfRange_IsInvalid(int percent)
    {
        await StartAsync();

        Assert.Equal(ErrorType.Validation, (await Sender.Send(new UpdateMemberAvailabilityCommand("t1", "m-maya", percent))).Error!.Type);
    }

    [Fact]
    public async Task GetPack_NoPackYet_IsNotFoundAndLatestIsReturnedAfterPreparing()
    {
        await StartAsync();
        Assert.Equal(ErrorType.NotFound, (await Sender.Send(new GetPlanningPackQuery("t1"))).Error!.Type);

        var pack = await PrepareAsync();

        Assert.Equal(pack.Id, (await Sender.Send(new GetPlanningPackQuery("t1"))).Value.Id);
    }

    public ValueTask DisposeAsync() => _sp.DisposeAsync();
}
