using System.Text.RegularExpressions;

namespace Zava.Scrum.Tools.SeedAdo;

/// <summary>
/// The rebrand mapping table (docs/rebrand): the old customer name becomes the generic brand. Applied in table order, case-sensitively, and
/// <c>MCM_</c> before <c>MCM</c> so constants and environment variables keep their upper-case style.
/// </summary>
internal static partial class RebrandMap
{
    private static readonly (string Find, string Replace)[] Rules =
    [
        ("Midland Credit Management", "Zava"), ("Midland Credit", "Zava"), ("Midland", "Zava"),
        ("an MCM", "a Zava"), ("An MCM", "A Zava"), ("MCM's", "Zava's"),
        ("MCM_", "ZAVA_"), ("MCM", "Zava"), ("Mcm", "Zava"), ("mcm", "zava"),
        ("Encore Capital", "Zava"), ("Encore", "Zava"),
    ];

    /// <summary>Gets a value indicating whether the text contains a token to replace (any case).</summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true"/> when it needs a change.</returns>
    public static bool NeedsChange(string? text) => !string.IsNullOrEmpty(text) && Token().IsMatch(text);

    /// <summary>Applies the mapping table.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The rebranded text.</returns>
    public static string Apply(string text)
    {
        foreach (var (find, replace) in Rules)
        {
            text = text.Replace(find, replace, StringComparison.Ordinal);
        }

        return text;
    }

    [GeneratedRegex("mcm|midland|encore", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Token();
}
