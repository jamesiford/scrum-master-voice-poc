using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>
/// Chooses which stored conversations a card goes to: only the conversations bound to the card's team (ADR 0018). A team with no bound
/// conversation gets no Teams delivery (the card stays in the outbox), so one team's cards never reach another team's channel.
/// </summary>
public static class CardRouting
{
    /// <summary>Selects the conversations for a card.</summary>
    /// <param name="message">The card.</param>
    /// <param name="storedKeys">The keys of every conversation the bot has stored.</param>
    /// <param name="team">The team the card belongs to.</param>
    /// <returns>The keys bound to the team for this kind of card.</returns>
    public static IReadOnlyList<string> Select(TeamsMessage message, IReadOnlyList<string> storedKeys, Team? team)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(storedKeys);
        if (team is null)
        {
            return [];
        }

        var planning = message.Kind == CardKind.PlanningPack;
        return storedKeys
            .Where(k => team.Bindings.Any(b => b.Matches(k) && (planning ? b.Purpose is BindingPurpose.Planning or BindingPurpose.Recap : b.Purpose is BindingPurpose.Recap or BindingPurpose.Standup)))
            .ToList();
    }
}
