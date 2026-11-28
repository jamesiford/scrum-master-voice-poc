using System.Text.RegularExpressions;

namespace Zava.Scrum.Domain.Voice;

/// <summary>
/// Finds the date a member said in a date-change statement ("Friday", "next sprint", "tomorrow", "March 3"), as said and never parsed:
/// the recorded note keeps the member's own words, and nothing writes a date to Azure DevOps.
/// </summary>
public static partial class SpokenDates
{
    /// <summary>Enumerates bounded candidate phrases in transcript order, including old dates a model must not mistake for revised targets.</summary>
    /// <param name="text">The unmodified transcript.</param>
    /// <returns>At most sixteen verbatim phrases; no model-produced dates.</returns>
    public static IReadOnlyList<string> FindAll(string? text) =>
        string.IsNullOrWhiteSpace(text) ? [] : Phrase().Matches(text).Take(16).Select(m => m.Value.Trim()).ToArray();

    /// <summary>Finds the first date phrase in a statement.</summary>
    /// <param name="text">What was said.</param>
    /// <returns>The phrase as spoken, or <see langword="null"/> when none is found.</returns>
    public static string? Find(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var m = Phrase().Match(text);
        return m.Success ? m.Value.Trim() : null;
    }

    [GeneratedRegex(
        @"\b(?:(?:end of(?: the)?\s+(?:(?:next|this)\s+)?(?:week|sprint|month|day))|(?:next|this)\s+(?:week|sprint|month|day|monday|tuesday|wednesday|thursday|friday|saturday|sunday)|(?:on\s+|by\s+|until\s+)?(?:monday|tuesday|wednesday|thursday|friday|saturday|sunday)|tomorrow|tonight|today|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\.?\s+\d{1,2}(?:st|nd|rd|th)?|\d{1,2}/\d{1,2}(?:/\d{2,4})?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Phrase();
}
