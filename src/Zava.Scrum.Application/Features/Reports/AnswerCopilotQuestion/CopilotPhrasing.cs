using System.Text.RegularExpressions;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

/// <summary>Keeps model phrasing separate from code-owned figures, citations and links before any channel output.</summary>
internal static partial class CopilotPhrasing
{
    /// <summary>Checks that a short introduction contains no figures, item references or links.</summary>
    /// <param name="text">The proposed introductory sentence.</param>
    /// <returns>Whether it can precede the deterministic facts.</returns>
    public static bool IsSafe(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= 200 &&
        !NumberOrLink().IsMatch(text) && WorkItemIdGuard.IsGrounded(text, new HashSet<WorkItemId>());

    [GeneratedRegex(@"\d|https?|\b(?:zero|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety|hundred|thousand|million|percent|all|none|every|no)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NumberOrLink();
}
