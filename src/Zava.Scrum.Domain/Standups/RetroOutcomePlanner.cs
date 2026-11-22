using Mcm.Scrum.Domain.FollowUps;
using Mcm.Scrum.Domain.Members;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>Turns a retrospective's agreed actions into proposed Azure DevOps tasks. Nothing is written until the Scrum Master approves (F7).</summary>
public static class RetroOutcomePlanner
{
    private const int MaxTitle = 100;

    /// <summary>The tag every retrospective task carries.</summary>
    public const string ActionTag = "retro-action";

    /// <summary>Plans one Task per action: tag <see cref="ActionTag"/>, in the next sprint, assigned to the agreed owner.</summary>
    /// <param name="summary">What the retrospective produced.</param>
    /// <param name="roster">The roster, for each owner's Azure DevOps name and demo key.</param>
    /// <param name="nextIterationPath">Where the tasks go.</param>
    /// <returns>The proposed changes, one per action.</returns>
    public static IReadOnlyList<ProposedChange> Plan(RetroSummary summary, IReadOnlyList<Member> roster, string nextIterationPath)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(roster);
        return summary.Actions.Take(RetroAgenda.MaxActions).Select(a =>
        {
            var owner = roster.FirstOrDefault(m => m.Id == a.OwnerMemberId);
            var title = "Retro action: " + (a.Text.Length <= MaxTitle ? a.Text : a.Text[..MaxTitle]);
            var item = new NewWorkItem("Task", title, a.Text, [ActionTag], nextIterationPath, owner?.AdoUniqueName ?? owner?.DisplayName ?? a.OwnerName);
            return ProposedChange.Create(item, $"Agreed in the retrospective; owner {a.OwnerName}.", owner?.Key);
        }).ToList();
    }

    /// <summary>Writes the one-line summary used on the approval card and the channel post.</summary>
    /// <param name="summary">What the retrospective produced.</param>
    /// <returns>For example "6 notes, 2 themes, 2 actions."</returns>
    public static string Headline(RetroSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return $"{summary.Notes.Count} notes, {summary.Themes.Count} themes, {summary.Actions.Count} actions.";
    }
}
