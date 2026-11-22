using System.Text.RegularExpressions;

namespace Zava.Scrum.Domain.Standups;

/// <summary>One theme: a shared keyword and the notes that contain it.</summary>
/// <param name="Keyword">The shared word (lower case).</param>
/// <param name="ItemIds">The notes in the group.</param>
public sealed record RetroTheme(string Keyword, IReadOnlyList<Guid> ItemIds);

/// <summary>
/// Groups retrospective notes by shared keywords in code. The model only names the groups afterwards, so a theme can never
/// contain a note it was not given.
/// </summary>
public static partial class ThemeGrouper
{
    private const int MinWordLength = 4;
    private const int MinGroupSize = 2;

    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "that", "this", "with", "have", "from", "they", "them", "there", "their", "what", "when", "where", "which", "were", "been", "being",
        "more", "much", "many", "some", "very", "just", "than", "then", "also", "into", "over", "about", "would", "could", "should", "keep",
        "stop", "start", "continue", "better", "need", "needs", "went", "well", "good", "great", "team", "think", "really", "like",
        "sprint", "sprints", "thing", "things", "people", "going", "doing", "make", "makes", "take", "takes", "getting", "every", "always", "never", "again", "still",
    };

    /// <summary>Groups notes that share a keyword. Largest groups first; a note joins the first group it fits.</summary>
    /// <param name="notes">The retrospective notes (active <see cref="StandupEventType.RetroItem"/> events).</param>
    /// <returns>The themes; notes that share nothing are in no theme.</returns>
    public static IReadOnlyList<RetroTheme> Group(IReadOnlyList<StandupEvent> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);
        var words = notes.ToDictionary(n => n.Id, n => Keywords(n.Text));
        var left = notes.Select(n => n.Id).ToList();
        var themes = new List<RetroTheme>();
        while (true)
        {
            var best = left
                .SelectMany(id => words[id])
                .GroupBy(w => w)
                .Select(g => (Word: g.Key, Count: g.Count()))
                .Where(x => x.Count >= MinGroupSize)
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Word, StringComparer.Ordinal)
                .FirstOrDefault();
            if (best.Word is null)
            {
                return themes;
            }

            var members = left.Where(id => words[id].Contains(best.Word)).ToList();
            themes.Add(new RetroTheme(best.Word, members));
            left.RemoveAll(members.Contains);
        }
    }

    private static HashSet<string> Keywords(string text) =>
        Words().Matches(text).Select(m => Stem(m.Value.ToLowerInvariant()))
            .Where(w => w.Length >= MinWordLength && !Stop.Contains(w))
            .ToHashSet(StringComparer.Ordinal);

    private static string Stem(string w) =>
        w.EndsWith("ies", StringComparison.Ordinal) ? w[..^3] + "y"
        : w.EndsWith('s') && !w.EndsWith("ss", StringComparison.Ordinal) && w.Length > MinWordLength ? w[..^1] : w;

    [GeneratedRegex(@"[\p{L}]+")]
    private static partial Regex Words();
}
