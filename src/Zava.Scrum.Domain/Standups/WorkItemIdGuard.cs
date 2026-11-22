using System.Text.RegularExpressions;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Standups;

/// <summary>
/// Enforces rule 2: never invent work item ids. Any id-like reference in model text that is not
/// in the current sprint causes the text to be rejected before it reaches speech or a card.
/// </summary>
public static partial class WorkItemIdGuard
{
    /// <summary>Checks that every work item reference in the text is a known id.</summary>
    /// <param name="text">Model-written text.</param>
    /// <param name="knownIds">Ids in the current sprint.</param>
    /// <returns><see langword="true"/> when the text references only known ids (or none).</returns>
    public static bool IsGrounded(string text, IReadOnlySet<WorkItemId> knownIds)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(knownIds);
        return ReferenceRegex().Matches(text)
            .Select(m => int.Parse(m.Groups["id"].Value, System.Globalization.CultureInfo.InvariantCulture))
            .All(id => knownIds.Contains(new WorkItemId(id)));
    }

    // Matches "#4521", "story 4521", "bug 4521", "task 4521", "item 4521", "work item 4521".
    [GeneratedRegex(@"(?:#|\b(?:story|bug|task|item|id)\s+#?)(?<id>\d{1,9})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceRegex();
}
