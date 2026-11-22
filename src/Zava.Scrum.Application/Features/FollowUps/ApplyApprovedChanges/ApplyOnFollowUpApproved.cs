using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.ApplyApprovedChanges;

/// <summary>Writes the approved changes when a follow-up is approved. Idempotent: applying twice writes once.</summary>
internal sealed class ApplyOnFollowUpApproved(ISender sender) : INotificationHandler<FollowUpApproved>
{
    public async Task Handle(FollowUpApproved domainEvent, CancellationToken ct) =>
        await sender.Send(new ApplyApprovedChangesCommand(domainEvent.FollowUpId), ct);
}
