using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Audit.GetAuditChain;
using Zava.Scrum.Application.Features.FollowUps.ApplyApprovedChanges;
using Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;
using Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Tests.Features.Identity;

/// <summary>Identity rules and the audit chain, through the real mediator with fakes only at the ports (ADR 0017).</summary>
public sealed class ApprovalIdentityTests : IAsyncDisposable
{
    private static readonly Guid PriyaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid MarcusId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly WriterSpy _writer = new();
    private readonly InMemoryFollowUpRepository _followUps = new();
    private readonly InMemorySessionRepository _sessions = new();
    private readonly InMemoryApprovalRecordStore _records = new();
    private readonly ServiceProvider _sp;

    public ApprovalIdentityTests()
    {
        var members = new InMemoryMemberRepository();
        members.ReplaceAsync("t1", [
            new Member("m-priya", "t1", "priya", "Priya S.", "priya@zava.example", "Priya", 1, true, EntraObjectId: PriyaId),
            new Member("m-marcus", "t1", "marcus", "Marcus", "marcus@zava.example", "Marcus", 2, true, EntraObjectId: MarcusId),
            new Member("m-cast", "t1", "cast", "Priya S.", null, "Priya", 3, true)], default).GetAwaiter().GetResult();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddIdentityFakes(_records);
        services.AddSingleton<IFollowUpRepository>(_followUps);
        services.AddSingleton<ISessionRepository>(_sessions);
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<IWorkItemWriter>(_writer);
        services.AddSingleton<ITeamsMessenger>(new RecordingTeamsMessenger());
        services.AddSingleton<ICardOutbox>(new EmptyOutbox());
        services.AddSingleton<IClock>(new FixedClock(StandupHarness.Now));
        _sp = services.BuildServiceProvider();
    }

    private ISender Sender => _sp.GetRequiredService<ISender>();

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Approve_ResetProposalWithoutPortalAppAdmin_RefusesBeforeDecisionOrWrite(bool teamsAdmin, bool sent)
    {
        var followUp = FollowUp.Create(Guid.NewGuid(), "t1", Guid.NewGuid(), null, FollowUpKind.Followup, "restore",
            [new ProposedChange(1, ChangeOp.ResetDemoItem, "seed", "restore", DemoReset: new DemoWorkItemReset(Guid.NewGuid(),
                "Project", "Project\\Area", "demo-key-one", new Dictionary<string, string?> { ["System.Title"] = "seed" }))],
            new DateOnly(2026, 1, 8), StandupHarness.Now, forScrumMaster: true);
        if (sent)
        {
            followUp.MarkSent();
        }
        await _followUps.SaveAsync(followUp, default);

        var result = await Sender.Send(new ApproveFollowUpCommand(followUp.Id, teamsAdmin ? Who.TeamsAdmin() : Who.Portal()));

        Assert.Equal("demo.admin_required", result.Error!.Code);
        Assert.Empty(_records.All);
        Assert.Empty(_writer.Written);
        Assert.Equal(sent ? FollowUpStatus.Sent : FollowUpStatus.Pending, (await _followUps.GetAsync(followUp.Id, default))!.Status);
    }

    [Fact]
    public async Task Approve_TeamsMemberOnTheirOwnFollowUp_IsRecordedWithIdentityAndHash()
    {
        var f = await AddAsync("m-priya");

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(PriyaId)));

        Assert.True(result.IsSuccess);
        var decision = Assert.Single(_records.All, r => r.Kind == ApprovalRecordKind.Approved);
        Assert.Equal(PriyaId, decision.Actor.ObjectId);
        Assert.Equal(f.ComputeProposalHash(), decision.ProposalHash);
        Assert.Equal(f.CorrelationId, decision.CorrelationId);
        Assert.Equal(ApprovalChannel.Teams, decision.Channel);
    }

    [Fact]
    public async Task Approve_ADisplayNameThatMatchesTheRosterButNotTheObjectId_IsRefusedAndNothingIsWritten()
    {
        var f = await AddAsync("m-priya");

        // "Priya S." is the roster name, but this object id is nobody on the roster.
        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(Guid.NewGuid(), "Priya S.")));

        Assert.Equal("identity.not_on_roster", result.Error!.Code);
        Assert.Empty(_writer.Written);
        Assert.Empty(_records.All);
        Assert.Equal(FollowUpStatus.Sent, (await _followUps.GetAsync(f.Id, default))!.Status);
    }

    [Fact]
    public async Task Approve_ARosterMemberOnSomeoneElsesFollowUp_IsRefused()
    {
        var f = await AddAsync("m-priya");

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(MarcusId, "Marcus")));

        Assert.Equal(ErrorType.Forbidden, result.Error!.Type);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Approve_ATeamsAdministrator_MayDecideAnyFollowUp()
    {
        var f = await AddAsync("m-priya");

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsAdmin()));

        Assert.True(result.IsSuccess);
        Assert.Single(_writer.Written);
    }

    [Fact]
    public async Task Approve_FromAnotherTenant_IsRefusedEvenForAnAdministratorObjectId()
    {
        var f = await AddAsync("m-priya");
        var foreign = ActorIdentity.Create(Who.AdminId.ToString(), Guid.NewGuid().ToString(), "Dana", null, ActorSource.Teams, "bot-service-token").Value;

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, DecisionContext.FromTeams(foreign)));

        Assert.Equal("identity.wrong_tenant", result.Error!.Code);
        Assert.Empty(_writer.Written);
    }

    [Theory]
    [InlineData(AppRoles.Reader)]
    [InlineData(AppRoles.Admin)]
    public async Task Approve_PortalCallerWithoutTheApproverRole_IsRefused(string role)
    {
        var f = await AddAsync("m-priya");

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.Portal("Dana", role)));

        Assert.Equal("identity.approver_role_required", result.Error!.Code);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Approve_NoIdentityAtAll_IsInvalid()
    {
        var f = await AddAsync("m-priya");

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, null!));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Reject_AppendsARejectionRecordAndWritesNothing()
    {
        var f = await AddAsync("m-priya");

        await Sender.Send(new RejectFollowUpCommand(f.Id, Who.Portal()));

        Assert.Equal(ApprovalRecordKind.Rejected, Assert.Single(_records.All).Kind);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Apply_AfterApproval_AppendsAWriteRecordWithTheRevisionsAndNeverUpdatesTheDecision()
    {
        var f = await AddAsync("m-priya");

        await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.Portal()));

        Assert.Equal([ApprovalRecordKind.Approved, ApprovalRecordKind.Applied], _records.All.Select(r => r.Kind));
        var applied = _records.All[1];
        Assert.Equal(_records.All[0].Id, applied.DecisionRecordId);
        Assert.Equal(new AdoRevision(1, 2), Assert.Single(applied.AdoRevisions));
        Assert.Empty(_records.All[0].AdoRevisions);
    }

    [Fact]
    public async Task Apply_WhenTheApprovalRecordIsMissing_IsRefusedAndNothingIsWritten()
    {
        var f = await AddAsync("m-priya");
        var stored = await _followUps.GetAsync(f.Id, default);
        stored!.Approve(Who.TeamsAdmin().Actor, StandupHarness.Now);
        await _followUps.SaveAsync(stored, default);

        var result = await Sender.Send(new ApplyApprovedChangesCommand(f.Id));

        Assert.Equal("followup.approval_record_missing", result.Error!.Code);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Apply_WhenTheProposalChangedAfterApproval_IsRefused()
    {
        var f = await AddAsync("m-priya");
        _writer.Fail = true;
        await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.Portal()));
        _writer.Fail = false;
        var tampered = FollowUp.Restore(_followUps.Snapshot(f.Id) with { Changes = [new ProposedChange(1, ChangeOp.Comment, "something else", "x")] });
        await _followUps.SaveAsync(tampered, default);

        var result = await Sender.Send(new ApplyApprovedChangesCommand(f.Id));

        Assert.Equal("followup.approval_record_missing", result.Error!.Code);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Apply_LegacyApprovalWithoutIdentity_IsNeverWritten()
    {
        var legacy = new FollowUpSnapshot(Guid.NewGuid(), "t1", Guid.NewGuid(), "m-priya", FollowUpKind.Followup, "m", [new ProposedChange(1, ChangeOp.Comment, "x", "y")], FollowUpStatus.Approved, new DateOnly(2026, 1, 8), StandupHarness.Now, "Dana", null, StandupHarness.Now, null, null);
        await _followUps.SaveAsync(FollowUp.Restore(legacy), default);

        var result = await Sender.Send(new ApplyApprovedChangesCommand(legacy.Id));

        Assert.Equal("identity.legacy_unverified", result.Error!.Code);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task AuditChain_StandupApprovedInTeamsAndInThePortal_IsCompleteBackToTheSession()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t1", StandupHarness.Now, null);
        await _sessions.SaveAsync(session, default);
        var inTeams = await AddAsync("m-priya", session);
        var inPortal = await AddAsync("m-marcus", session);

        await Sender.Send(new ApproveFollowUpCommand(inTeams.Id, Who.TeamsUser(PriyaId)));
        await Sender.Send(new ApproveFollowUpCommand(inPortal.Id, Who.Portal()));
        var chain = await Sender.Send(new GetAuditChainQuery(Correlation.Tag(session.CorrelationId)));

        Assert.True(chain.IsSuccess);
        Assert.True(chain.Value.Complete, string.Join("; ", chain.Value.Missing));
        Assert.Equal(session.CorrelationId, chain.Value.CorrelationId);
        Assert.Equal(session.Id, chain.Value.Session!.Id);
        Assert.Equal(2, chain.Value.ApprovedCount);
        Assert.Equal(4, chain.Value.Records.Count);
        Assert.Equal([ApprovalChannel.Teams, ApprovalChannel.Portal], chain.Value.Records.Where(r => r.Kind == ApprovalRecordKind.Approved).Select(r => r.Channel));
        Assert.All(chain.Value.Records.Where(r => r.Kind == ApprovalRecordKind.Applied), r => Assert.NotEmpty(r.AdoRevisions));
        Assert.All(_writer.Written, w => Assert.EndsWith($"Correlation: {Correlation.Tag(session.CorrelationId)}", w.Attribution, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuditChain_StoredMeeting_PreservesRealMeetingIdentity()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t1", StandupHarness.Now, null);
        const string link = "https://teams.microsoft.com/l/meetup-join/chat/0";
        session.Start("acs", link, 15, StandupHarness.Now);
        session.RecordMeetingEvent("meeting", "chat", link, false, StandupHarness.Now);
        await _sessions.SaveAsync(session, default);

        var result = await Sender.Send(new GetAuditChainQuery(session.CorrelationId));

        Assert.True(result.IsSuccess);
        Assert.Equal("meeting", result.Value.Meeting?.MeetingId);
        Assert.Equal("chat", result.Value.Meeting?.ConversationId);
        Assert.Equal(link, result.Value.JoinUrl);
    }

    [Fact]
    public async Task AuditChain_ApprovedButNotYetWritten_IsCompleteSoFarAndStillCompleteAfterTheRetry()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t1", StandupHarness.Now, null);
        await _sessions.SaveAsync(session, default);
        var f = await AddAsync("m-priya", session);
        _writer.Fail = true;
        await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.Portal()));

        var before = await Sender.Send(new GetAuditChainQuery(session.CorrelationId));
        _writer.Fail = false;
        await Sender.Send(new ApplyApprovedChangesCommand(f.Id));
        var after = await Sender.Send(new GetAuditChainQuery(session.CorrelationId));

        Assert.True(before.Value.Complete);
        Assert.DoesNotContain(before.Value.Records, r => r.Kind == ApprovalRecordKind.Applied);
        Assert.True(after.Value.Complete);
        Assert.Contains(after.Value.Records, r => r.Kind == ApprovalRecordKind.Applied);
    }

    [Fact]
    public async Task AuditChain_AnAppliedFollowUpWithNoRecords_ListsWhatIsMissing()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t1", StandupHarness.Now, null);
        await _sessions.SaveAsync(session, default);
        var snapshot = new FollowUpSnapshot(Guid.NewGuid(), "t1", session.Id, "m-priya", FollowUpKind.Followup, "m", [new ProposedChange(1, ChangeOp.Comment, "x", "y")], FollowUpStatus.Applied, new DateOnly(2026, 1, 8), StandupHarness.Now, "Dana", null, StandupHarness.Now, StandupHarness.Now, null, ApprovedByIdentity: Who.TeamsAdmin().Actor, CorrelationId: session.CorrelationId);
        await _followUps.SaveAsync(FollowUp.Restore(snapshot), default);

        var chain = await Sender.Send(new GetAuditChainQuery(session.CorrelationId));

        Assert.False(chain.Value.Complete);
        Assert.Contains(chain.Value.Missing, m => m.StartsWith("decision record", StringComparison.Ordinal));
    }
    [Theory]
    [InlineData("not-a-trace-id")]
    [InlineData("zsa-12")]
    public async Task AuditChain_BadReference_IsInvalid(string reference)
    {
        var result = await Sender.Send(new GetAuditChainQuery(reference));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task AuditChain_UnknownCorrelation_IsNotFound()
    {
        var result = await Sender.Send(new GetAuditChainQuery(Correlation.New()));

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    public ValueTask DisposeAsync() => _sp.DisposeAsync();

    private async Task<FollowUp> AddAsync(string memberId, StandupSession? session = null)
    {
        var f = FollowUp.Create(
            Guid.NewGuid(), "t1", session?.Id ?? Guid.NewGuid(), memberId, FollowUpKind.Followup, "msg",
            [new ProposedChange(1, ChangeOp.Comment, "Back on track", "said so")], new DateOnly(2026, 1, 8), StandupHarness.Now, correlationId: session?.CorrelationId);
        f.MarkSent();
        await _followUps.SaveAsync(f, default);
        return f;
    }

    private sealed class EmptyOutbox : ICardOutbox
    {
        public Task<IReadOnlyList<SentCard>> ListAsync(Guid? sessionId, CancellationToken ct) => Task.FromResult<IReadOnlyList<SentCard>>([]);
    }
}
