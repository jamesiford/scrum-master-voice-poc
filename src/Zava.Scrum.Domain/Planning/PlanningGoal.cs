using System.Text.RegularExpressions;

namespace Mcm.Scrum.Domain.Planning;

/// <summary>The draft sprint goal: a deterministic fallback, and the check that a model-phrased goal invents nothing (rule 3).</summary>
public static partial class PlanningGoal
{
    private const int GoalTitles = 3;

    /// <summary>Builds a plain goal from the selected items when no model phrasing is available.</summary>
    /// <param name="proposal">The proposal.</param>
    /// <returns>For example "Finish the carry-over and deliver Payment retry, Settlement validation and Plan summary."</returns>
    public static string Fallback(PlanningProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        var titles = proposal.Selected.Where(s => s.Source == PlanningSource.Backlog).Take(GoalTitles).Select(s => s.Title).ToList();
        var carry = proposal.Selected.Any(s => s.Source == PlanningSource.CarryOver);
        var delivered = titles.Count switch
        {
            0 => string.Empty,
            1 => titles[0],
            _ => string.Join(", ", titles.Take(titles.Count - 1)) + " and " + titles[^1],
        };
        return (carry, delivered.Length > 0) switch
        {
            (true, true) => $"Finish the carry-over work and deliver {delivered}.",
            (true, false) => "Finish the carry-over work.",
            (false, true) => $"Deliver {delivered}.",
            _ => "Agree the sprint goal in planning.",
        };
    }

    /// <summary>Checks that a model-phrased goal uses only numbers that appear in the proposal (item ids, points, titles, the total and the velocity range).</summary>
    /// <param name="goal">The model's text.</param>
    /// <param name="proposal">The proposal.</param>
    /// <returns><see langword="true"/> when every number in the goal is in the proposal.</returns>
    public static bool IsGrounded(string goal, PlanningProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentNullException.ThrowIfNull(proposal);
        var allowed = Numbers(string.Join(' ', proposal.Selected.Select(s => $"{s.WorkItemId} {s.Points} {s.Title}").Append($"{proposal.TotalPoints} {proposal.Velocity.Low} {proposal.Velocity.High}"))).ToHashSet();
        return Numbers(goal).All(allowed.Contains);
    }

    private static IEnumerable<string> Numbers(string text) => NumberRegex().Matches(text).Select(m => m.Value);

    [GeneratedRegex(@"\d+(?:\.\d+)?")]
    private static partial Regex NumberRegex();
}
