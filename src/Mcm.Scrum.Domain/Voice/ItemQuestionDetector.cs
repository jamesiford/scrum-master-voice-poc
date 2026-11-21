using System.Text.RegularExpressions;

namespace Mcm.Scrum.Domain.Voice;

/// <summary>Recognises a teammate asking the agent about a work item without saying its name.</summary>
public static partial class ItemQuestionDetector
{
    /// <summary>Checks whether an utterance is a question about an item's facts.</summary>
    /// <param name="text">The transcribed utterance.</param>
    /// <returns><see langword="true"/> for a question such as "is that one overdue?".</returns>
    public static bool IsItemQuestion(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Contains('?', StringComparison.Ordinal) && Cue().IsMatch(text);

    [GeneratedRegex(@"\b(overdue|due( date)?|deadline|stale|blocked|assigned|who owns|who has|story points?|how many points|how long|what state|which sprint|when (is|was|does)|is that one|what was the|what's the)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Cue();
}
