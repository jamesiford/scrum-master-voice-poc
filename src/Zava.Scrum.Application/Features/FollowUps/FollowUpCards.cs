using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.FollowUps;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.FollowUps;

/// <summary>
/// Builds the Teams card for a follow-up. One place for titles, card kinds and the change rows, so the first card and the
/// card that replaces it after a decision never disagree (DRY).
/// </summary>
internal static class FollowUpCards
{
    /// <summary>Builds the card sent when the follow-up is created.</summary>
    /// <param name="f">The follow-up.</param>
    /// <param name="name">Display name of the member it is for, or "the team".</param>
    /// <returns>The message.</returns>
    public static TeamsMessage Initial(FollowUp f, string name)
    {
        var (kind, title) = f.Kind switch
        {
            FollowUpKind.Absent => (CardKind.Absent, $"Missed {f.Ceremony.Phrase()}: {name}"),
            FollowUpKind.Escalation => (CardKind.Escalation, "Needs your attention"),
            FollowUpKind.Unblock => (CardKind.Unblock, $"Can you help unblock this, {name}?"),
            FollowUpKind.RetroActions => (CardKind.RetroActions, "Retrospective actions"),
            FollowUpKind.ReviewPack => (CardKind.ReviewPack, "Sprint review pack"),
            _ => (CardKind.FollowUp, $"Follow-up for {name}"),
        };
        return new TeamsMessage(kind, f.TeamId, f.SessionId, Key(f), title, f.Message, Facts(f), f.Id, f.MemberId);
    }

    /// <summary>Builds the card that replaces the first one once a decision is made.</summary>
    /// <param name="f">The follow-up.</param>
    /// <param name="name">Display name of the member it is for, or "the team".</param>
    /// <param name="status">Plain-language status row.</param>
    /// <returns>The message.</returns>
    public static TeamsMessage Result(FollowUp f, string name, string status)
    {
        var initial = Initial(f, name);
        return initial with { Kind = CardKind.FollowUpResult, Facts = [.. initial.Facts, new CardFact("Status", status)] };
    }

    /// <summary>Gets the key a follow-up card is sent and updated under.</summary>
    /// <param name="f">The follow-up.</param>
    /// <returns>The dedupe key.</returns>
    public static string Key(FollowUp f) => $"followup:{f.Id}";

    private static List<CardFact> Facts(FollowUp f) =>
        f.Changes.Select(c => c.Op switch
        {
            ChangeOp.CreateWorkItem => new CardFact($"New {c.New?.Type ?? "item"}", c.Value),
            _ => new CardFact($"Work item {c.WorkItemId} ({c.Op})", c.Value),
        }).ToList();
}
