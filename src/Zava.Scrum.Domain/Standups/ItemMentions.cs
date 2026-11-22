using System.Text.RegularExpressions;

namespace Zava.Scrum.Domain.Standups;

/// <summary>
/// Works out which work items a spoken update refers to, in code. A model guessing ids from speech linked answers to the wrong
/// item, so coverage never depends on it (rule 3). Matches "story 14", "task fourteen", "#14", or words that identify exactly one
/// of the speaker's items.
/// </summary>
public static partial class ItemMentions
{
    // One short word ("test", "load") is too generic to identify an item; two of them, or one long distinctive word, is enough.
    private const int DistinctiveLength = 7;

    private static readonly string[] Ones =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen",
        "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty",
    ];

    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "with", "from", "that", "this", "have", "page", "file", "job", "add", "update", "write", "prepare", "review", "attach", "export",
    };

    /// <summary>Finds the items mentioned.</summary>
    /// <param name="text">What was said.</param>
    /// <param name="own">The speaker's items, used for title matching.</param>
    /// <param name="knownIds">Every sprint item id; a number that is not one of them is ignored (rule 2).</param>
    /// <returns>Distinct ids in order of appearance.</returns>
    public static IReadOnlyList<int> Detect(string? text, IReadOnlyList<BriefItem> own, IReadOnlySet<int> knownIds)
    {
        ArgumentNullException.ThrowIfNull(own);
        ArgumentNullException.ThrowIfNull(knownIds);
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var found = new List<int>();
        foreach (Match m in Reference().Matches(text))
        {
            var raw = m.Groups["n"].Value;
            var id = int.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : Array.IndexOf(Ones, raw.ToLowerInvariant());
            if (id >= 0 && knownIds.Contains(id) && !found.Contains(id))
            {
                found.Add(id);
            }
        }

        foreach (var id in ByTitle(text, own).Where(id => !found.Contains(id)))
        {
            found.Add(id);
        }

        return found;
    }

    // A word that appears in exactly one of the speaker's titles identifies that item; "retry" alone does not when two items share it.
    private static IEnumerable<int> ByTitle(string text, IReadOnlyList<BriefItem> own)
    {
        var spoken = Words(text).ToHashSet();
        var titleWords = own.ToDictionary(i => i.Id.Value, i => Words(i.Title).Where(w => w.Length >= 4 && !Stop.Contains(w)).ToHashSet());
        foreach (var (id, words) in titleWords)
        {
            var unique = words.Where(w => titleWords.Count(o => o.Value.Contains(w)) == 1).ToList();
            var hits = unique.Where(spoken.Contains).ToList();
            if (hits.Count >= 2 || hits.Any(w => w.Length >= DistinctiveLength))
            {
                yield return id;
            }
        }
    }

    private static IEnumerable<string> Words(string text) =>
        NonLetters().Split(text.ToLowerInvariant()).Where(w => w.Length > 0).Select(w => w.Length > 4 && w.EndsWith('s') ? w[..^1] : w);

    [GeneratedRegex(@"\b(?:user\s+story|story|bug|task|item|ticket|issue|work\s+item|number|no\.?|#)\s*#?(?<n>\d{1,9}|zero|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty)\b|#(?<n>\d{1,9})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Reference();

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonLetters();
}
