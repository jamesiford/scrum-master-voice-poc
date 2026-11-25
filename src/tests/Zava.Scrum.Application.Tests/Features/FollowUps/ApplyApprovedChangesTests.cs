using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.FollowUps.ApplyApprovedChanges;
using Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;
using Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features.FollowUps;

public sealed class ApplyApprovedChangesTests : IAsyncDisposable
{
    private readonly FakeWriter _writer = new();
    private readonly FakeScopeReader _scope = new();
    private readonly RecordingTeamsMessenger _messenger = new();
    private readonly InMemoryFollowUpRepository _repo = new();
    private readonly InMemoryApprovalRecordStore _records = new();
    private readonly ServiceProvider _sp;

    public ApplyApprovedChangesTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddIdentityFakes(_records);
        services.AddSingleton<IFollowUpRepository>(_repo);
        services.AddSingleton<IWorkItemWriter>(_writer);
        services.AddSingleton<IWorkItemScopeReader>(_scope);
        services.AddSingleton<ITeamsMessenger>(_messenger);
        services.AddSingleton<IMemberRepository>(new InMemoryMemberRepository());
        services.AddSingleton<IClock>(new FixedClock(StandupHarness.Now));
        _sp = services.BuildServiceProvider();
    }

    private ISender Sender => _sp.GetRequiredService<ISender>();

    [Fact]
    public async Task Apply_Pending_IsRefusedAndNothingIsWritten()
    {
        var id = await AddAsync();

        var result = await Sender.Send(new ApplyApprovedChangesCommand(id));

        Assert.Equal(ErrorType.NotApproved, result.Error!.Type);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Apply_AnItemInAnotherTeamsAreaPath_IsRefusedAndNothingIsWritten()
    {
        _scope.Place(1, "p", "another-team");
        var id = await AddAsync();

        await Sender.Send(new ApproveFollowUpCommand(id, Who.TeamsAdmin("Dana")));
        var result = await Sender.Send(new ApplyApprovedChangesCommand(id));

        Assert.Equal("team.work_item_out_of_scope", result.Error!.Code);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Apply_Rejected_IsRefusedAndNothingIsWritten()
    {
        var id = await AddAsync();
        await Sender.Send(new RejectFollowUpCommand(id, Who.TeamsAdmin("Dana")));

        var result = await Sender.Send(new ApplyApprovedChangesCommand(id));

        Assert.Equal(ErrorType.NotApproved, result.Error!.Type);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Approve_WithoutApproverName_IsInvalidAndNothingIsWritten()
    {
        var id = await AddAsync();

        var result = await Sender.Send(new ApproveFollowUpCommand(id, null!));

        Assert.False(result.IsSuccess);
        Assert.Empty(_writer.Written);
    }

    [Fact]
    public async Task Approve_Pending_WritesOnceWithAttributionAndMarksApplied()
    {
        var id = await AddAsync();

        var approved = await Sender.Send(new ApproveFollowUpCommand(id, Who.TeamsAdmin("Dana")));
        var again = await Sender.Send(new ApplyApprovedChangesCommand(id));

        Assert.True(approved.IsSuccess);
        Assert.True(again.IsSuccess);
        var written = Assert.Single(_writer.Written);
        Assert.Equal("Dana", written.ApprovedBy);
        Assert.Contains("Approved by Dana in Teams", written.Attribution, StringComparison.Ordinal);
        Assert.Equal(FollowUpStatus.Applied, (await _repo.GetAsync(id, default))!.Status);
    }

    [Fact]
    public async Task Apply_WriterFails_StaysApprovedWithErrorAndRetrySucceeds()
    {
        var id = await AddAsync();
        _writer.Fail = true;
        await Sender.Send(new ApproveFollowUpCommand(id, Who.TeamsAdmin("Dana")));
        var stored = await _repo.GetAsync(id, default);
        Assert.Equal(FollowUpStatus.Approved, stored!.Status);
        Assert.NotNull(stored.ApplyError);

        _writer.Fail = false;
        var retry = await Sender.Send(new ApplyApprovedChangesCommand(id));

        Assert.True(retry.IsSuccess);
        Assert.Equal(FollowUpStatus.Applied, (await _repo.GetAsync(id, default))!.Status);
    }

    [Fact]
    public async Task Approve_ThenApplied_UpdatesCardWithoutActionsAndShowsOutcome()
    {
        var id = await AddAsync();

        await Sender.Send(new ApproveFollowUpCommand(id, Who.TeamsAdmin("Dana")));

        Assert.All(_messenger.Updated, m => Assert.Equal(CardKind.FollowUpResult, m.Kind));
        Assert.Contains("written to Azure DevOps", _messenger.Updated[^1].Facts[^1].Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reject_UpdatesCardToShowNothingWasWritten()
    {
        var id = await AddAsync();

        await Sender.Send(new RejectFollowUpCommand(id, Who.TeamsAdmin("Dana")));

        Assert.Contains("Nothing was written", Assert.Single(_messenger.Updated).Facts[^1].Value, StringComparison.Ordinal);
    }

    public ValueTask DisposeAsync() => _sp.DisposeAsync();

    private async Task<Guid> AddAsync()
    {
        var followUp = FollowUp.Create(
            Guid.NewGuid(), "t1", Guid.NewGuid(), "m-priya", FollowUpKind.Followup, "msg",
            [new ProposedChange(1, ChangeOp.Comment, "Back on track", "said so")], new DateOnly(2026, 1, 8), StandupHarness.Now);
        await _repo.SaveAsync(followUp, default);
        return followUp.Id;
    }

    private sealed class FakeWriter : IWorkItemWriter
    {
        public List<ApprovedChange> Written { get; } = [];

        public bool Fail { get; set; }

        public Task<Result<WorkItemRevision>> ApplyAsync(ApprovedChange change, AdoSettings target, CancellationToken ct)
        {
            if (Fail)
            {
                return Task.FromResult(Result<WorkItemRevision>.Failure(Error.ExternalRejected("ado.write_rejected", "rejected")));
            }

            Written.Add(change);
            return Task.FromResult(Result<WorkItemRevision>.Success(new WorkItemRevision(change.WorkItemId, 7)));
        }
    }
}
