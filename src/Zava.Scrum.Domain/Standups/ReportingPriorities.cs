using Mcm.Scrum.Domain.Flags;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>Works out what a member should report on, most urgent first, so the agent can answer "what am I supposed to update?".</summary>
public static class ReportingPriorities
{
    private const int MaxItems = 3;

    /// <summary>Builds the speakable list.</summary>
    /// <param name="member">The member and their open items.</param>
    /// <param name="covered">Item ids already discussed.</param>
    /// <returns>For example "task 14, overdue: say the new date; story 1, stale: say if it is on track", or a general prompt.</returns>
    public static string For(MemberBrief member, IReadOnlySet<int> covered)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(covered);
        var open = member.Items.Where(i => !covered.Contains(i.Id.Value)).ToList();
        if (open.Count == 0)
        {
            return "Everything assigned to them is already covered. Ask if there is anything else, such as a blocker.";
        }

        var ordered = open.OrderBy(i => i.Flags.Count == 0 ? 9 : Rank(i.Flags)).ToList();
        var lines = ordered.Take(MaxItems).Select(Line).ToList();
        var more = ordered.Count > MaxItems ? $"; and {ordered.Count - MaxItems} more open items they can mention briefly" : string.Empty;
        return string.Join("; ", lines) + more + ". Also ask whether anything is blocking them.";
    }

    private static string Line(BriefItem i)
    {
        var label = CoverageNotes.Label(i);
        if (i.Flags.Contains(Flag.Blocked))
        {
            return $"{label}, {i.Title}, is blocked: what is needed and who can help";
        }

        if (i.Flags.Contains(Flag.Overdue))
        {
            return $"{label}, {i.Title}, is past its due date: what is the new date";
        }

        if (i.Flags.Contains(Flag.AtRisk))
        {
            return $"{label}, {i.Title}, is due soon and at risk: will it finish in time";
        }

        if (i.Flags.Contains(Flag.Stale))
        {
            return $"{label}, {i.Title}, has been {i.State} for {i.DaysInState} days: is it on track";
        }

        return i.Flags.Count > 0
            ? $"{label}, {i.Title}: where does it stand"
            : $"{label}, {i.Title}: a quick status";
    }

    private static int Rank(IReadOnlyList<Flag> flags) =>
        flags.Contains(Flag.Blocked) ? 0 : flags.Contains(Flag.Overdue) ? 1 : flags.Contains(Flag.AtRisk) ? 2 : flags.Contains(Flag.Stale) ? 3 : 4;
}
