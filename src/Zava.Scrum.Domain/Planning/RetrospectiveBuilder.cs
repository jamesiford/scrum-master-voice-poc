using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Planning;

/// <summary>A start-stop-continue draft for a retrospective (SPEC M8).</summary>
/// <param name="Start">Things to start doing.</param>
/// <param name="Stop">Things to stop doing.</param>
/// <param name="Continue">Things to keep doing.</param>
public sealed record RetroDraft(IReadOnlyList<string> Start, IReadOnlyList<string> Stop, IReadOnlyList<string> Continue);

/// <summary>
/// Builds the factual start-stop-continue skeleton from what the standups and the sprint recorded. The model only phrases it,
/// so every figure and work item id comes from code (rule 3).
/// </summary>
public static class RetrospectiveBuilder
{
    private const int MaxLines = 5;

    /// <summary>Builds the draft.</summary>
    /// <param name="sprint">The current sprint.</param>
    /// <param name="sessions">The team's standup sessions, newest first.</param>
    /// <param name="prep">Planning prep for the sprint (readiness gaps).</param>
    /// <returns>The draft.</returns>
    public static RetroDraft Build(SprintSnapshot sprint, IReadOnlyList<StandupSession> sessions, PlanningPrep prep)
    {
        ArgumentNullException.ThrowIfNull(sprint);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(prep);

        var events = sessions.SelectMany(s => s.ActiveEvents).ToList();
        var blockers = events.Where(e => e.Type == StandupEventType.Blocker).Select(e => e.Text.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxLines).Select(t => $"Blocker raised in standup: {t}");
        var flagged = (sessions.Count > 0 ? sessions[0].PreBrief : null)?.Members.SelectMany(m => m.Items).Where(i => i.Flags.Count > 0).Take(MaxLines)
            .Select(i => $"#{i.Id} \"{i.Title}\" was flagged {string.Join(", ", i.Flags)}") ?? [];
        var parked = events.Where(e => e.Type == StandupEventType.ParkingLot).Select(e => e.Text.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxLines);

        var done = sprint.Items.Count(i => i.IsDone);
        var commitments = events.Count(e => e.Type == StandupEventType.Commitment);
        var keep = new List<string> { $"{done} of {sprint.Items.Count} sprint items are done." };
        if (commitments > 0)
        {
            keep.Add($"{commitments} commitments were recorded in standups.");
        }

        var start = prep.NotReady.Take(MaxLines).Select(g => $"#{g.WorkItemId} \"{g.Title}\" is not ready: {string.Join(", ", g.Reasons)}")
            .Concat(parked.Select(p => $"Follow up on parked topic: {p}"))
            .ToList();
        return new RetroDraft(start, [.. blockers, .. flagged], keep);
    }
}
