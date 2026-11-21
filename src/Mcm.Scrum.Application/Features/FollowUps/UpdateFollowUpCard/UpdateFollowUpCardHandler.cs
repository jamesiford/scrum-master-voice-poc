using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Application.Features.FollowUps.UpdateFollowUpCard;

/// <summary>
/// Replaces the follow-up card with its outcome once it is approved, rejected or applied, so the buttons disappear and the
/// result is visible in Teams. It renders the follow-up's current state rather than the event, so the order in which the
/// approve and apply handlers run does not matter (notification handlers must be idempotent).
/// </summary>
internal sealed class UpdateFollowUpCardHandler(IFollowUpRepository followUps, IMemberRepository members, ITeamsMessenger messenger) :
    INotificationHandler<FollowUpApproved>,
    INotificationHandler<FollowUpRejected>,
    INotificationHandler<FollowUpApplied>
{
    public Task Handle(FollowUpApproved domainEvent, CancellationToken ct) => RefreshAsync(domainEvent.FollowUpId, ct);

    public Task Handle(FollowUpRejected domainEvent, CancellationToken ct) => RefreshAsync(domainEvent.FollowUpId, ct);

    public Task Handle(FollowUpApplied domainEvent, CancellationToken ct) => RefreshAsync(domainEvent.FollowUpId, ct);

    private async Task RefreshAsync(Guid id, CancellationToken ct)
    {
        var f = await followUps.GetAsync(id, ct);
        if (f is null)
        {
            return;
        }

        var name = f.MemberId is null ? "the team" : (await members.ListAsync(f.TeamId, ct)).FirstOrDefault(m => m.Id == f.MemberId)?.DisplayName ?? f.MemberId;
        var facts = f.Changes.Select(c => new CardFact($"Work item {c.WorkItemId} ({c.Op})", c.Value)).ToList();
        facts.Add(new CardFact("Status", Describe(f)));
        await messenger.UpdateAsync(new TeamsMessage(CardKind.FollowUpResult, f.TeamId, f.SessionId, $"followup:{f.Id}", $"Follow-up for {name}", f.Message, facts, f.Id, f.MemberId), ct);
    }

    private static string Describe(FollowUp f) => f.Status switch
    {
        FollowUpStatus.Approved => f.ApplyError is null ? $"Approved by {f.ApprovedBy}. Writing to Azure DevOps." : $"Approved by {f.ApprovedBy}. Azure DevOps write failed: {f.ApplyError}",
        FollowUpStatus.Applied => $"Approved by {f.ApprovedBy} and written to Azure DevOps.",
        FollowUpStatus.Rejected => $"Rejected by {f.RejectedBy}. Nothing was written.",
        _ => f.Status.ToString(),
    };
}
