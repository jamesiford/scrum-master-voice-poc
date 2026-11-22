using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Planning;

namespace Zava.Scrum.Application.Features.Planning;

/// <summary>
/// Builds the Teams card for a planning pack and the card that replaces it after a decision. One place for the wording, so the first card
/// and its replacement never disagree (DRY). The card key is <c>planning:{packId}</c>; <see cref="TeamsMessage.FollowUpId"/> carries the pack id.
/// </summary>
internal static class PlanningCards
{
    private const int MaxListed = 8;

    /// <summary>Gets the key the card is sent and updated under.</summary>
    /// <param name="packId">The pack.</param>
    /// <returns>The dedupe key.</returns>
    public static string Key(Guid packId) => $"planning:{packId}";

    /// <summary>Builds the card with Approve plan and Reject.</summary>
    /// <param name="pack">The pack.</param>
    /// <returns>The message.</returns>
    public static TeamsMessage Initial(PlanningPackData pack) =>
        new(CardKind.PlanningPack, pack.TeamId, pack.Id, Key(pack.Id), $"Sprint planning: {pack.NextIterationPath ?? "next sprint"}", Summary(pack), Facts(pack), pack.Id);

    /// <summary>Builds the card that replaces the first one once the plan is decided.</summary>
    /// <param name="pack">The decided pack.</param>
    /// <param name="status">Plain-language outcome.</param>
    /// <returns>The message.</returns>
    public static TeamsMessage Result(PlanningPackData pack, string status) =>
        Initial(pack) with { Kind = CardKind.FollowUpResult, Facts = [.. Facts(pack), new CardFact("Status", status)] };

    private static string Summary(PlanningPackData pack)
    {
        var p = pack.Proposal;
        var illustrative = pack.VelocityIllustrative ? " (illustrative history)" : string.Empty;
        return $"{p.Selected.Count} items, {p.TotalPoints:0.#} points planned against a velocity of {p.Velocity.Low:0.#} to {p.Velocity.High:0.#}{illustrative}. Draft goal: {pack.DraftGoal}";
    }

    private static List<CardFact> Facts(PlanningPackData pack)
    {
        var p = pack.Proposal;
        var facts = p.Members.Select(m => new CardFact(m.DisplayName, $"{m.PlannedPoints:0.#} of {m.CapacityHigh:0.#} points ({m.AvailabilityPct}% available)")).ToList();
        facts.AddRange(p.Selected.Take(MaxListed).Select(s => new CardFact($"Plan #{s.WorkItemId}", $"{s.Title}: {s.Points:0.#} points, {Owner(s)}")));
        if (p.Selected.Count > MaxListed)
        {
            facts.Add(new CardFact("Plan", $"and {p.Selected.Count - MaxListed} more items"));
        }

        if (p.Excluded.Count > 0)
        {
            facts.Add(new CardFact("Not planned", $"{p.Excluded.Count} items, with reasons in the portal"));
        }

        facts.AddRange(p.Warnings.Select(w => new CardFact("Warning", w)));
        return facts;
    }

    private static string Owner(PlannedItem s) => s.OwnerName is null ? "no owner" : s.OwnerSuggested ? $"{s.OwnerName} (suggested)" : s.OwnerName;
}
