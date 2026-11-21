using Mcm.Scrum.Domain.Flags;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>
/// Plain-language facts for the agent's turns: what was just noted and which flagged items nobody has covered. Built in code so
/// the model only phrases them (rule 3).
/// </summary>
public static class CoverageNotes
{
    /// <summary>Summarises blockers and commitments recorded from an update.</summary>
    /// <param name="events">The events recorded for the update.</param>
    /// <returns>For example "blocker: waiting on the vendor; commitment: finish retries", or an empty string.</returns>
    public static string Noted(IEnumerable<(StandupEventType Type, string Text)> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var parts = events
            .Where(e => e.Type is StandupEventType.Blocker or StandupEventType.Commitment)
            .Select(e => $"{e.Type.ToString().ToLowerInvariant()}: {e.Text.Trim()}")
            .ToList();
        return string.Join("; ", parts);
    }

    /// <summary>Lists flagged items that nobody has covered yet, most urgent first.</summary>
    /// <param name="brief">The pre-brief.</param>
    /// <param name="covered">Work item ids discussed so far.</param>
    /// <param name="memberId">Limit to one member, or <see langword="null"/> for everyone.</param>
    /// <returns>The uncovered flagged items.</returns>
    public static IReadOnlyList<BriefItem> UncoveredFlagged(PreBrief brief, IReadOnlySet<int> covered, string? memberId = null)
    {
        ArgumentNullException.ThrowIfNull(brief);
        ArgumentNullException.ThrowIfNull(covered);
        return brief.Members
            .Where(m => memberId is null || m.MemberId == memberId)
            .SelectMany(m => m.Items)
            .Where(i => i.Flags.Count > 0 && !covered.Contains(i.Id.Value))
            .OrderBy(i => Rank(i.Flags))
            .ToList();
    }

    /// <summary>Describes items in speakable words.</summary>
    /// <param name="items">The items.</param>
    /// <returns>For example "story 5 (overdue), bug 12 (blocked)", or "none".</returns>
    public static string Describe(IEnumerable<BriefItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var parts = items.Select(i => $"{i.Type.ToLowerInvariant().Replace("user story", "story", StringComparison.Ordinal)} {i.Id} ({string.Join(", ", i.Flags.Select(Word))})").ToList();
        return parts.Count == 0 ? "none" : string.Join(", ", parts);
    }

    private static string Word(Flag f) => f switch
    {
        Flag.NoRecentUpdate => "no recent update",
        _ => f.ToString().ToLowerInvariant(),
    };

    private static int Rank(IReadOnlyList<Flag> flags) =>
        flags.Contains(Flag.Blocked) ? 0 : flags.Contains(Flag.Overdue) ? 1 : flags.Contains(Flag.Stale) ? 2 : 3;
}
