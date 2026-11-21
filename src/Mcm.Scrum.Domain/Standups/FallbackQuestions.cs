using Mcm.Scrum.Domain.Flags;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>Deterministic questions built from flags. Used when the model is unavailable or returns an ungrounded id.</summary>
public static class FallbackQuestions
{
    /// <summary>Builds a question for one member from their most important flagged item.</summary>
    /// <param name="brief">The member's items.</param>
    /// <returns>A question that names the item, or a general one when nothing is flagged.</returns>
    public static string For(IReadOnlyList<BriefItem> brief)
    {
        ArgumentNullException.ThrowIfNull(brief);
        var flagged = MostUrgent(brief);

        if (flagged is null)
        {
            return brief.Count == 0
                ? "You have nothing assigned in the sprint. Is there anything you can pick up or help with?"
                : "Anything that could get in the way of finishing your work this sprint?";
        }

        var label = CoverageNotes.Label(flagged);
        return Primary(flagged.Flags) switch
        {
            Flag.Blocked => $"{label}, {flagged.Title}, is blocked. What is needed to unblock it, and who can help?",
            Flag.Overdue => $"{label}, {flagged.Title}, is past its due date. What is the new date?",
            Flag.Stale => $"{label}, {flagged.Title}, has been {flagged.State} for {flagged.DaysInState} days. Still on track?",
            Flag.NoRecentUpdate => $"{label}, {flagged.Title}, has had no update since yesterday. Where does it stand?",
            _ => $"Can you give an update on {label}, {flagged.Title}?",
        };
    }

    /// <summary>Picks the most urgent flagged item: blocked, overdue, stale, then the rest.</summary>
    /// <param name="items">Candidate items.</param>
    /// <returns>The item, or <see langword="null"/> when none is flagged.</returns>
    public static BriefItem? MostUrgent(IReadOnlyList<BriefItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Where(i => i.Flags.Count > 0).OrderBy(i => Rank(i.Flags)).FirstOrDefault();
    }

    /// <summary>Builds the question for one specific flagged item.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The question.</returns>
    public static string ForItem(BriefItem item) => For([item]);

    private static int Rank(IReadOnlyList<Flag> flags) => Primary(flags) switch
    {
        Flag.Blocked => 0,
        Flag.Overdue => 1,
        Flag.Stale => 2,
        _ => 3,
    };

    private static Flag Primary(IReadOnlyList<Flag> flags) =>
        flags.Contains(Flag.Blocked) ? Flag.Blocked
        : flags.Contains(Flag.Overdue) ? Flag.Overdue
        : flags.Contains(Flag.Stale) ? Flag.Stale
        : flags[0];
}
