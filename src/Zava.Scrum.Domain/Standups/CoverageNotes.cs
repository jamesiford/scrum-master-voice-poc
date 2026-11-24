using Zava.Scrum.Domain.Flags;

namespace Zava.Scrum.Domain.Standups;

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
            .Where(e => e.Type is StandupEventType.Blocker or StandupEventType.Commitment or StandupEventType.DateChange)
            .Select(e => $"{(e.Type == StandupEventType.DateChange ? "date change" : e.Type.ToString().ToLowerInvariant())}: {e.Text.Trim()}")
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

    /// <summary>Speakable label such as "story 5" or "task 14".</summary>
    /// <param name="item">The item.</param>
    /// <returns>The label.</returns>
    public static string Label(BriefItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return $"{(item.Type.Equals("User Story", StringComparison.OrdinalIgnoreCase) ? "story" : item.Type.ToLowerInvariant())} {item.Id}";
    }

    /// <summary>
    /// Builds the opening notes for a member's turn: what they promised last time, and which blocked teammate they were named to help
    /// (M9-R2, M9-R3). Both are facts from earlier standups, so the model only phrases the question.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>Instructions for the agent, or an empty string when there is nothing to carry over.</returns>
    public static string CarryOverNotes(MemberBrief member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var lines = new List<string>();
        foreach (var c in (member.PreviousCommitments ?? []).OrderByDescending(c => c.StandupsAgo))
        {
            var item = member.Items.FirstOrDefault(i => i.Id.Value == c.WorkItemId);
            var label = item is null ? $"item {c.WorkItemId}" : Label(item);
            var when = c.StandupsAgo == 1 ? "yesterday" : $"{c.StandupsAgo} standups ago";
            lines.Add($"Ask first about their earlier commitment: {when} they said \"{c.Text}\" about {label}. Ask whether it is done.");
        }

        foreach (var h in member.HelpRequests ?? [])
        {
            lines.Add($"Ask whether they have been able to help {h.BlockedMemberName} with {h.Type.ToLowerInvariant().Replace("user story", "story", StringComparison.Ordinal)} {h.WorkItemId}, {h.Title}, which is still blocked.");
        }

        return string.Join(" ", lines);
    }
    private static string Word(Flag f) => f switch
    {
        Flag.NoRecentUpdate => "no recent update",
        Flag.AtRisk => "at risk",
        Flag.MissedCommitment => "missed commitment",
        _ => f.ToString().ToLowerInvariant(),
    };

    private static int Rank(IReadOnlyList<Flag> flags) =>
        flags.Contains(Flag.Blocked) ? 0 : flags.Contains(Flag.Overdue) ? 1 : flags.Contains(Flag.AtRisk) ? 2 : flags.Contains(Flag.Stale) ? 3 : 4;
}
