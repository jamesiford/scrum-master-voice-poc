namespace Mcm.Scrum.Domain.Schedules;

/// <summary>Pure scheduling rule: prepare at T-10, start at T-1, give up 30 minutes after the start (SPEC M5).</summary>
public static class ScheduleEvaluator
{
    /// <summary>Minutes before the start that the standup is prepared.</summary>
    public const int PrepareLeadMinutes = 10;

    /// <summary>Minutes before the start that the standup is started.</summary>
    public const int StartLeadMinutes = 1;

    /// <summary>Minutes after the start when a missed standup is no longer started automatically.</summary>
    public const int GraceMinutes = 30;

    /// <summary>Decides what is due.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="localNow">The current time in the team's time zone.</param>
    /// <returns>The action; at most one per call, so a late scheduler still prepares before it starts.</returns>
    public static ScheduleAction Evaluate(Schedule schedule, DateTime localNow)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (!schedule.Enabled || !schedule.Days.Contains(localNow.DayOfWeek))
        {
            return ScheduleAction.None;
        }

        var today = DateOnly.FromDateTime(localNow);
        var start = today.ToDateTime(schedule.StartTime);
        if (localNow >= start.AddMinutes(GraceMinutes))
        {
            return ScheduleAction.None;
        }

        if (schedule.PreparedFor != today)
        {
            return localNow >= start.AddMinutes(-PrepareLeadMinutes) ? ScheduleAction.Prepare : ScheduleAction.None;
        }

        var canJoin = schedule.Mode == "acs" && !string.IsNullOrWhiteSpace(schedule.JoinUrl);
        return canJoin && schedule.StartedFor != today && localNow >= start.AddMinutes(-StartLeadMinutes) ? ScheduleAction.Start : ScheduleAction.None;
    }
}
