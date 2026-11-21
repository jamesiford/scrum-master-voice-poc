namespace Mcm.Scrum.Domain.Kpis;

/// <summary>What happened in one finished sprint (SPEC 9.5, 10.4).</summary>
/// <param name="Name">Sprint name.</param>
/// <param name="Start">First day.</param>
/// <param name="End">Last day.</param>
/// <param name="CommittedPoints">Points committed at the start.</param>
/// <param name="CompletedPoints">Points completed.</param>
/// <param name="CarryOverItems">Items carried into the next sprint.</param>
/// <param name="AvgBlockerAgeDays">Average age of blockers in days.</param>
/// <param name="AvgCycleTimeDays">Average story cycle time in days.</param>
/// <param name="StandupAttendancePct">Average standup attendance, 0 to 100.</param>
/// <param name="FollowUpClosedWithin24hPct">Share of follow-ups closed within 24 hours, 0 to 100.</param>
/// <param name="AgentRunStandups">Standups run by the agent.</param>
/// <param name="AvgStandupMinutes">Average standup duration in minutes.</param>
public sealed record SprintHistory(
    string Name,
    DateOnly Start,
    DateOnly End,
    double CommittedPoints,
    double CompletedPoints,
    int CarryOverItems,
    double AvgBlockerAgeDays,
    double AvgCycleTimeDays,
    double StandupAttendancePct,
    double FollowUpClosedWithin24hPct,
    int AgentRunStandups,
    double AvgStandupMinutes);
