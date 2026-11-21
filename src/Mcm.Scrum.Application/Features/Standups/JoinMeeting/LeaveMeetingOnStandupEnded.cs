using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Standups.JoinMeeting;

/// <summary>Hangs up the bridge's call when the standup ends. Safe when there is no call.</summary>
internal sealed class LeaveMeetingOnStandupEnded(IMeetingBridge bridge) : INotificationHandler<StandupEnded>
{
    public Task Handle(StandupEnded domainEvent, CancellationToken ct) => bridge.LeaveAsync(domainEvent.SessionId, ct);
}
