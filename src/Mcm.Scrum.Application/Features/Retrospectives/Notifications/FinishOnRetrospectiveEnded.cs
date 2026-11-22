using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Retrospectives.CreateRetroOutcome;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Retrospectives.Notifications;

/// <summary>When a retrospective ends, hangs up the bridge's call and creates the follow-up. Both steps are idempotent.</summary>
internal sealed class FinishOnRetrospectiveEnded(IMeetingBridge bridge, ISender sender) : INotificationHandler<RetrospectiveEnded>
{
    public async Task Handle(RetrospectiveEnded domainEvent, CancellationToken ct)
    {
        await bridge.LeaveAsync(domainEvent.SessionId, ct);
        await sender.Send(new CreateRetroOutcomeCommand(domainEvent.SessionId), ct);
    }
}
