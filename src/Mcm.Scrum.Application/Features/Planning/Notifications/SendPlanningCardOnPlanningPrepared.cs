using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Planning;

namespace Mcm.Scrum.Application.Features.Planning.Notifications;

/// <summary>Sends the planning card to the Scrum Master when a pack is prepared (outbox and Teams).</summary>
internal sealed class SendPlanningCardOnPlanningPrepared(IPlanningPackRepository packs, ITeamsMessenger messenger) : INotificationHandler<PlanningPrepared>
{
    public async Task Handle(PlanningPrepared domainEvent, CancellationToken ct)
    {
        var pack = await packs.GetAsync(domainEvent.PackId, ct);
        if (pack is null)
        {
            return;
        }

        await messenger.SendAsync(PlanningCards.Initial(pack.Data), ct);
    }
}
