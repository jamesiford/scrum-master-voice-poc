using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups.EndStandup;
using Zava.Scrum.Application.Features.Standups.MarkAbsent;
using Zava.Scrum.Application.Features.Standups.RecordStandupEvent;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features.FollowUps;

public sealed class EndStandupFollowUpsTests
{
    [Fact]
    public async Task End_WithCommitmentAndAbsence_CreatesFollowUpsAndCardsOnce()
    {
        await using var h = await StandupHarness.CreateAsync();
        await h.Sender.Send(new RecordStandupEventCommand(h.SessionId, StandupEventType.Commitment, "m-priya", "Finishing retries today", [1]));
        await h.Sender.Send(new MarkAbsentCommand(h.SessionId, "m-marcus"));

        await h.Sender.Send(new EndStandupCommand(h.SessionId));
        await h.Sender.Send(new EndStandupCommand(h.SessionId));

        var followUps = await h.Get<IFollowUpRepository>().ListBySessionAsync(h.SessionId, default);
        Assert.Contains(followUps, f => f.Kind == FollowUpKind.Followup && f.MemberId == "m-priya" && f.Changes.Count == 1 && f.Status == FollowUpStatus.Sent);
        Assert.Contains(followUps, f => f.Kind == FollowUpKind.Absent && f.MemberId == "m-marcus");
        var cards = ((RecordingTeamsMessenger)h.Get<ITeamsMessenger>()).Sent;
        Assert.Single(cards, c => c.Kind == CardKind.Recap);
        Assert.Equal(followUps.Count, cards.Count(c => c.Kind is CardKind.FollowUp or CardKind.Absent or CardKind.Escalation));
    }

    [Fact]
    public async Task End_WhenLeavingTheCallCancelsTheCallersToken_StillSendsRecapAndFollowUps()
    {
        using var callerToken = new CancellationTokenSource();
        var messenger = new CancellationAwareMessenger();
        await using var h = await StandupHarness.CreateAsync(configure: services =>
        {
            services.AddSingleton<ITeamsMessenger>(messenger);
            services.AddSingleton<IMeetingBridge>(new CancellingBridge(callerToken));
        });
        await h.Sender.Send(new MarkAbsentCommand(h.SessionId, "m-marcus"));

        await h.Sender.Send(new EndStandupCommand(h.SessionId), callerToken.Token);

        Assert.True(callerToken.IsCancellationRequested);
        Assert.Contains(messenger.Sent, c => c.Kind == CardKind.Recap);
        Assert.Contains(messenger.Sent, c => c.Kind == CardKind.Absent && c.MemberId == "m-marcus");
        Assert.NotEmpty(await h.Get<IFollowUpRepository>().ListBySessionAsync(h.SessionId, default));
    }

    // Models the bridge socket closing when the call is left: the gateway's token (RequestAborted) is cancelled by the hang-up itself.
    private sealed class CancellingBridge(CancellationTokenSource caller) : IMeetingBridge
    {
        public Task<Zava.Scrum.Domain.Common.Result> JoinAsync(MeetingJoinRequest request, CancellationToken ct) => Task.FromResult(Zava.Scrum.Domain.Common.Result.Success());

        public Task LeaveAsync(Guid sessionId, CancellationToken ct)
        {
            caller.Cancel();
            return Task.CompletedTask;
        }
    }

    // Like the real HTTP messenger, refuses to send once its token is cancelled.
    private sealed class CancellationAwareMessenger : ITeamsMessenger
    {
        public List<TeamsMessage> Sent { get; } = [];

        public Task UpdateAsync(TeamsMessage message, CancellationToken ct) => Task.CompletedTask;

        public Task SendAsync(TeamsMessage message, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }
}
