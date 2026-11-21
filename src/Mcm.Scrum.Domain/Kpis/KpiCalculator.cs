namespace Mcm.Scrum.Domain.Kpis;

/// <summary>KPI maths (SPEC 10.4). Pure, so the numbers can be checked by hand against the fixture.</summary>
public static class KpiCalculator
{
    /// <summary>The smallest window the spec reports on.</summary>
    public const int MinSprints = 3;

    /// <summary>The largest window the spec reports on.</summary>
    public const int MaxSprints = 6;

    /// <summary>Takes the most recent sprints, clamped to three to six (or fewer if that is all there is).</summary>
    /// <param name="history">Finished sprints.</param>
    /// <param name="requested">Window requested.</param>
    /// <returns>The window, oldest first.</returns>
    public static IReadOnlyList<SprintHistory> Window(IEnumerable<SprintHistory> history, int requested)
    {
        ArgumentNullException.ThrowIfNull(history);
        var size = Math.Clamp(requested, MinSprints, MaxSprints);
        return history.OrderBy(s => s.End).TakeLast(size).ToList();
    }

    /// <summary>Summarises a window of sprints.</summary>
    /// <param name="window">The sprints, from <see cref="Window"/>.</param>
    /// <returns>The KPIs; zeros for an empty window.</returns>
    public static KpiSummary Summarize(IReadOnlyList<SprintHistory> window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (window.Count == 0)
        {
            return new KpiSummary(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var committed = window.Sum(s => s.CommittedPoints);
        return new KpiSummary(
            window.Count,
            committed == 0 ? 0 : window.Sum(s => s.CompletedPoints) / committed,
            window.Average(s => s.CompletedPoints),
            window.Sum(s => s.CarryOverItems),
            window.Average(s => s.AvgBlockerAgeDays),
            window.Average(s => s.AvgCycleTimeDays),
            window.Average(s => s.StandupAttendancePct),
            window.Average(s => s.FollowUpClosedWithin24hPct),
            window.Sum(s => s.AgentRunStandups),
            window.Average(s => s.AvgStandupMinutes));
    }

    /// <summary>Gets one sprint's say/do ratio.</summary>
    /// <param name="sprint">The sprint.</param>
    /// <returns>Completed divided by committed; zero when nothing was committed.</returns>
    public static double SayDo(SprintHistory sprint)
    {
        ArgumentNullException.ThrowIfNull(sprint);
        return sprint.CommittedPoints == 0 ? 0 : sprint.CompletedPoints / sprint.CommittedPoints;
    }
}
