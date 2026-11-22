using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.FollowUps.SendFollowUps;

/// <summary>Sends follow-ups when a standup ends.</summary>
internal sealed class SendFollowUpsOnStandupEnded(ISender sender) : INotificationHandler<StandupEnded>
{
    public async Task Handle(StandupEnded domainEvent, CancellationToken ct) =>
        await sender.Send(new SendFollowUpsCommand(domainEvent.SessionId), ct);
}
