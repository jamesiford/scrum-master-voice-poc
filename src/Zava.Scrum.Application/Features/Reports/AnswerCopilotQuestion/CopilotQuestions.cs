using System.Text.RegularExpressions;
using Zava.Scrum.Domain.Reports;

namespace Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

/// <summary>Maps supported natural-language starters to bounded read-only skills without model tool selection.</summary>
public static partial class CopilotQuestions
{
    /// <summary>Gets the five short conversation starters.</summary>
    public static IReadOnlyList<string> Starters { get; } =
    [
        "What is blocked on my team?",
        "What did I commit to?",
        "What is at risk in this sprint?",
        "What is the standup status today?",
        "Show my items and their state.",
    ];

    /// <summary>Reads an explicit team prefix and a supported skill.</summary>
    /// <param name="text">The question, optionally prefixed with <c>team id:</c>.</param>
    /// <param name="skill">The supported skill.</param>
    /// <param name="teamId">The explicit choice, never inferred from a binding.</param>
    /// <returns>Whether the question requests a supported skill.</returns>
    public static bool TryParse(string? text, out CopilotSkill skill, out string? teamId)
    {
        skill = default;
        teamId = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var question = text.Trim();
        var prefix = TeamPrefix().Match(question);
        if (prefix.Success)
        {
            teamId = prefix.Groups["team"].Value;
            question = question[prefix.Length..].Trim();
        }

        if (Blocked().IsMatch(question)) { skill = CopilotSkill.Blocked; }
        else if (Commitments().IsMatch(question)) { skill = CopilotSkill.Commitments; }
        else if (Risk().IsMatch(question)) { skill = CopilotSkill.SprintRisk; }
        else if (Status().IsMatch(question)) { skill = CopilotSkill.StandupStatus; }
        else if (MyItems().IsMatch(question)) { skill = CopilotSkill.MyItems; }
        else { return false; }
        return true;
    }

    [GeneratedRegex(@"^team\s+(?<team>[A-Za-z0-9_-]+)\s*:\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TeamPrefix();

    [GeneratedRegex(@"\bblock(?:ed|ing|ers?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Blocked();

    [GeneratedRegex(@"\b(?:commit(?:ted|ments?)?|promis(?:e|ed))\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Commitments();

    [GeneratedRegex(@"\b(?:risk|risky|overdue)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Risk();

    [GeneratedRegex(@"\bstandup\b.*\b(?:status|today)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Status();

    [GeneratedRegex(@"\bmy\s+(?:work\s+)?items\b|\bassigned\s+to\s+me\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MyItems();
}
