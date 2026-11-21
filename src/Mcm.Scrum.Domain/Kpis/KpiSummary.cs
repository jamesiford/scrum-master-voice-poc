namespace Mcm.Scrum.Domain.Kpis;

/// <summary>Team KPIs over a window of sprints, all computed in code (rule 3).</summary>
/// <param name="Sprints">How many sprints are included.</param>
/// <param name="SayDoRatio">Completed points divided by committed points over the window, 0 to 1 or more.</param>
/// <param name="Velocity">Average completed points per sprint.</param>
/// <param name="CarryOverItems">Total items carried over.</param>
/// <param name="AvgBlockerAgeDays">Mean of the sprints' average blocker age.</param>
/// <param name="AvgCycleTimeDays">Mean of the sprints' average cycle time.</param>
/// <param name="AvgAttendancePct">Mean standup attendance.</param>
/// <param name="AvgFollowUpClosurePct">Mean follow-up closure within 24 hours.</param>
/// <param name="AgentRunStandups">Total standups run by the agent.</param>
/// <param name="AvgStandupMinutes">Mean standup duration.</param>
public sealed record KpiSummary(
    int Sprints,
    double SayDoRatio,
    double Velocity,
    int CarryOverItems,
    double AvgBlockerAgeDays,
    double AvgCycleTimeDays,
    double AvgAttendancePct,
    double AvgFollowUpClosurePct,
    int AgentRunStandups,
    double AvgStandupMinutes);