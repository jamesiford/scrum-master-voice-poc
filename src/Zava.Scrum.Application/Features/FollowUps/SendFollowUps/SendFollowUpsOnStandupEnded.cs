using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.FollowUps.SendFollowUps;

/// <summary>Sends follow-ups when a standup ends.</summary>
internal sealed class SendFollowUpsOnStandupEnded(ISender sender) : INotificationHandler<StandupEnded>
{
    public async Task Handle(StandupEnded domainEvent, CancellationToken ct) =>
        await sender.Send(new SendFollowUpsCommand(domainEvent.SessionId), ct);
}
