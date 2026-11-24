using System.Text.RegularExpressions;

namespace Zava.Scrum.Domain.Voice;

/// <summary>
/// Finds the date a member said in a date-change statement ("Friday", "next sprint", "tomorrow", "March 3"), as said and never parsed:
/// the recorded note keeps the member's own words, and nothing writes a date to Azure DevOps.
/// </summary>
public static partial class SpokenDates
{
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
        @"\b(?:(?:next|this|end of(?: the)?)\s+(?:week|sprint|month|day|monday|tuesday|wednesday|thursday|friday)|(?:on\s+|by\s+|until\s+)?(?:monday|tuesday|wednesday|thursday|friday|saturday|sunday)|tomorrow|tonight|today|(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\.?\s+\d{1,2}(?:st|nd|rd|th)?|\d{1,2}/\d{1,2}(?:/\d{2,4})?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Phrase();
}
