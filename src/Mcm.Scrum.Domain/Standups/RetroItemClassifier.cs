using System.Text.RegularExpressions;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>
/// Sorts what a member says in a retrospective into start, stop or continue with plain rules, never a model, so the board
/// and the follow-up depend on code (rule 3). The earliest keyword in the sentence wins; no keyword means unclear and the agent asks.
/// </summary>
public static partial class RetroItemClassifier
{
    /// <summary>What the agent asks when a note is unclear.</summary>
    public const string ClarifyingQuestion = "Is that something to start, stop or keep doing?";

    private const int MinWords = 3;

    /// <summary>Gets the category of one note.</summary>
    /// <param name="text">The note.</param>
    /// <returns>The category, or <see langword="null"/> when unclear.</returns>
    public static RetroCategory? Classify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        RetroCategory? best = null;
        var bestAt = int.MaxValue;
        foreach (var (category, regex) in new[] { (RetroCategory.Start, StartWords()), (RetroCategory.Stop, StopWords()), (RetroCategory.Continue, ContinueWords()) })
        {
            var m = regex.Match(text);
            if (m.Success && m.Index < bestAt)
            {
                best = category;
                bestAt = m.Index;
            }
        }

        return best;
    }

    /// <summary>Splits an utterance into separate notes at sentence ends, semicolons, "but" and "also", and drops fragments.</summary>
    /// <param name="utterance">What the member said.</param>
    /// <returns>The notes, each at least three words, trimmed and capitalised.</returns>
    public static IReadOnlyList<string> Split(string? utterance)
    {
        if (string.IsNullOrWhiteSpace(utterance))
        {
            return [];
        }

        return Boundary().Split(utterance)
            .Select(Clean)
            .Where(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= MinWords)
            .ToList();
    }

    private static string Clean(string s)
    {
        var t = Lead().Replace(s.Trim(), string.Empty).Trim(' ', '.', ',', '!', '?', ';');
        return t.Length == 0 ? t : char.ToUpperInvariant(t[0]) + t[1..];
    }

    [GeneratedRegex(@"(?<=[.!?;])\s+|\s+(?:but|and also|also)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex Boundary();

    [GeneratedRegex(@"^(?:actually|sorry|no|um|uh|well|so|and|also|but)\b[,\s]*", RegexOptions.IgnoreCase)]
    private static partial Regex Lead();

    [GeneratedRegex(@"\b(?:start|begin|try|missing|lack|lacking|wish|add|introduce|should have)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StartWords();

    [GeneratedRegex(@"\b(?:stop|quit|less|fewer|too (?:many|much|long|slow)|waste|wasting|avoid|drop|do better|improve)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StopWords();

    [GeneratedRegex(@"\b(?:keep|continue|more of|went well|worked|working well|good|great|liked|love|loved|enjoyed)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ContinueWords();
}
