using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.FollowUps.ApplyApprovedChanges;
using Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Application.Tests.Features.Identity;

/// <summary>Azure DevOps is written as the approver, with no fallback to a service identity (ADR 0020), through the real mediator.</summary>
public sealed class DelegatedWriteTests
{
    private static readonly Guid PriyaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly WriterSpy _writer = new();
    private readonly InMemoryFollowUpRepository _followUps = new();
    private readonly InMemoryApprovalRecordStore _records = new();
    private readonly FakeApproverTokenSource _tokens = new();
    private readonly ServiceProvider _sp;

    public DelegatedWriteTests()
    {
        var members = new InMemoryMemberRepository();
        members.ReplaceAsync("t1", [new Member("m-priya", "t1", "priya", "Priya S.", "priya@zava.example", "Priya", 1, true, EntraObjectId: PriyaId)], default).GetAwaiter().GetResult();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddIdentityFakes(_records);
        services.AddSingleton<IApproverTokenSource>(_tokens);
        services.AddSingleton<IFollowUpRepository>(_followUps);
        services.AddSingleton<ISessionRepository>(new InMemorySessionRepository());
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<IWorkItemWriter>(_writer);
        services.AddSingleton<ITeamsMessenger>(new RecordingTeamsMessenger());
        services.AddSingleton<IClock>(new FixedClock(StandupHarness.Now));
        _sp = services.BuildServiceProvider();
    }

    private ISender Sender => _sp.GetRequiredService<ISender>();

    [Fact]
    public async Task Approve_WithTheApproversOwnToken_WritesWithADelegatedTokenForThatPerson()
    {
        var f = await AddAsync();

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(PriyaId)));

        Assert.True(result.IsSuccess);
        Assert.Equal([$"ado-for-{PriyaId}"], _writer.Tokens);
    }

    [Fact]
    public async Task Approve_WithNoToken_IsRefusedBeforeAnythingIsRecordedOrWritten()
    {
        var f = await AddAsync();
        var decision = Who.TeamsUser(PriyaId);
        Who.Signed = null;

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, decision));

        Assert.Equal("identity.delegated_token_required", result.Error!.Code);
        Assert.Empty(_records.All);
        Assert.Empty(_writer.Written);
        Assert.Equal(FollowUpStatus.Sent, (await _followUps.GetAsync(f.Id, default))!.Status);
    }

    [Fact]
    public async Task Approve_WhenEntraRefusesTheExchange_IsRefusedAndTheManagedIdentityIsNeverUsed()
    {
        var f = await AddAsync();
        _tokens.Refuse = true;

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(PriyaId)));

        Assert.Equal("delegation.exchange_failed", result.Error!.Code);
        Assert.Empty(_writer.Written);
        Assert.Empty(_records.All);
    }

    [Fact]
    public async Task Apply_RetriedBySomeoneElse_IsRefusedAndNothingIsWritten()
    {
        var f = await AddAsync();
        _tokens.Refuse = false;
        await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(PriyaId)));
        _writer.Written.Clear();
        _writer.Tokens.Clear();
        await ResetAppliedAsync(f.Id);
        _ = Who.TeamsUser(Guid.NewGuid(), "Someone else");

        var result = await Sender.Send(new ApplyApprovedChangesCommand(f.Id));

        Assert.Equal("identity.delegated_token_not_approver", result.Error!.Code);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Apply_ByTheApproverAfterTheExchangeFailed_WritesOnceAndLeavesNoErrorBehind()
    {
        var f = await AddAsync();
        await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(PriyaId)));
        await ResetAppliedAsync(f.Id);
        _writer.Written.Clear();
        _tokens.Refuse = true;
        _ = Who.TeamsUser(PriyaId);
        var refused = await Sender.Send(new ApplyApprovedChangesCommand(f.Id));
        _tokens.Refuse = false;

        var retried = await Sender.Send(new ApplyApprovedChangesCommand(f.Id));

        Assert.Equal("delegation.exchange_failed", refused.Error!.Code);
        Assert.True(retried.IsSuccess);
        Assert.Single(_writer.Written);
    }
    [Fact]
    public async Task Apply_WhenTheApproverCannotWriteTheItem_FailsWithTheReasonAndRecordsNoAppliedRecord()
    {
        var f = await AddAsync();
        _writer.Fail = true;
        _writer.FailWith = Error.Forbidden("ado.approver_cannot_write", "no write access. Nothing was written.");

        await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(PriyaId)));

        var stored = (await _followUps.GetAsync(f.Id, default))!;
        Assert.Contains("no write access", stored.ApplyError, StringComparison.Ordinal);
        Assert.NotEqual(FollowUpStatus.Applied, stored.Status);
        Assert.DoesNotContain(_records.All, r => r.Kind == ApprovalRecordKind.Applied);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Approve_WhenConditionalAccessNeedsAnInteractiveSignIn_IsRefusedWithThatCodeAndNothingIsRecorded()
    {
        var f = await AddAsync();
        _tokens.Refuse = true;
        _tokens.RefusalError = Error.NotApproved("delegation.interaction_required", "sign in again");

        var result = await Sender.Send(new ApproveFollowUpCommand(f.Id, Who.TeamsUser(PriyaId)));

        Assert.Equal("delegation.interaction_required", result.Error!.Code);
        Assert.Empty(_records.All);
        Assert.Empty(_writer.Written);
    }

    private async Task ResetAppliedAsync(Guid id)
    {
        var stored = (await _followUps.GetAsync(id, default))!;
        var snapshot = stored.ToSnapshot() with { Status = FollowUpStatus.Approved, AppliedAt = null };
        await _followUps.SaveAsync(FollowUp.Restore(snapshot), default);
    }

    private async Task<FollowUp> AddAsync()
    {
        var f = FollowUp.Create(
            Guid.NewGuid(), "t1", Guid.NewGuid(), "m-priya", FollowUpKind.Followup, "msg",
            [new ProposedChange(1, ChangeOp.Comment, "Back on track", "said so")], new DateOnly(2026, 1, 8), StandupHarness.Now);
        f.MarkSent();
        await _followUps.SaveAsync(f, default);
        return f;
    }
}
