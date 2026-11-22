using System.Text.RegularExpressions;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Domain.Ceremonies;

/// <summary>
/// What a ceremony hands the Scrum Master: figures computed in code (rule 3), a plain summary, and proposed changes that become a
/// follow-up. Nothing in a pack is written to Azure DevOps until a person approves that follow-up (F7).
/// </summary>
/// <param name="Kind">The ceremony.</param>
/// <param name="IterationPath">The iteration the figures describe.</param>
/// <param name="Summary">Deterministic summary; a model may rephrase it but never changes a number.</param>
/// <param name="Facts">Computed figures.</param>
/// <param name="Items">Proposed changes with their descriptions.</param>
public sealed partial record CeremonyPack(
    CeremonyKind Kind,
    string IterationPath,
    string Summary,
    IReadOnlyList<PackFact> Facts,
    IReadOnlyList<PackItem> Items)
{
    /// <summary>Gets the proposed changes, in order.</summary>
    public IReadOnlyList<ProposedChange> Changes => [.. Items.Select(i => i.Change)];

    /// <summary>Gets the kind of follow-up this pack becomes.</summary>
    public FollowUpKind FollowUpKind => Kind switch
    {
        CeremonyKind.SprintPlanning => FollowUpKind.PlanningPack,
        CeremonyKind.SprintReview => FollowUpKind.ReviewPack,
        _ => FollowUpKind.RetroActions,
    };

    /// <summary>Checks that model-written text uses only numbers the pack computed (rule 3).</summary>
    /// <param name="text">The model's phrasing.</param>
    /// <returns><see langword="true"/> when every number in the text appears in the pack's summary, facts or items.</returns>
    public bool IsGrounded(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var allowed = Numbers(string.Join(' ', [Summary, .. Facts.Select(f => f.Label + ' ' + f.Value), .. Items.Select(i => i.Text)])).ToHashSet();
        return Numbers(text).All(allowed.Contains);
    }

    private static IEnumerable<string> Numbers(string text) => NumberRegex().Matches(text).Select(m => m.Value);

    [GeneratedRegex(@"\d+(?:\.\d+)?")]
    private static partial Regex NumberRegex();
}
