namespace Zava.Scrum.Domain.Calendars;

/// <summary>
/// Proposes a sprint's meetings from its dates and the team's settings (CEREMONIES O2). Pure: dates are dates (Azure DevOps iteration
/// dates have no time of day), the standup time is local to the team's time zone, and the result is in UTC.
/// </summary>
/// <remarks>
/// Layout: a standup on every working day at the standup time. On the first working day sprint planning follows the standup; on the last
/// working day the review (when held) and the retrospective follow it, back to back.
/// </remarks>
public static class SprintCalendarPlanner
{
    /// <summary>Plans the meetings.</summary>
    /// <param name="iterationName">Short iteration name used in titles, for example <c>Sprint 43</c>.</param>
    /// <param name="start">First day of the sprint.</param>
    /// <param name="end">Last day of the sprint.</param>
    /// <param name="zone">The team's time zone.</param>
    /// <param name="standupAt">Local standup time.</param>
    /// <param name="workingDays">Days the team works.</param>
    /// <param name="durations">Meeting lengths.</param>
    /// <returns>The meetings in time order; empty when the sprint has no working day.</returns>
    public static IReadOnlyList<CalendarMeeting> Plan(
        string iterationName,
        DateOnly start,
        DateOnly end,
        TimeZoneInfo zone,
        TimeOnly standupAt,
        IReadOnlyCollection<DayOfWeek> workingDays,
        CalendarDurations durations)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(workingDays);
        ArgumentNullException.ThrowIfNull(durations);
        var days = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (workingDays.Contains(d.DayOfWeek))
            {
                days.Add(d);
            }
        }

        var meetings = new List<CalendarMeeting>();
        foreach (var day in days)
        {
            var at = day.ToDateTime(standupAt);
            meetings.Add(Meeting(MeetingKind.Standup, $"{iterationName}: Daily standup", day, at, durations.StandupMinutes, zone));
            at = at.AddMinutes(durations.StandupMinutes);
            if (day == days[0])
            {
                meetings.Add(Meeting(MeetingKind.Planning, $"{iterationName}: Sprint planning", day, at, durations.PlanningMinutes, zone));
            }

            if (day != days[^1])
            {
                continue;
            }

            if (durations.HasSprintReview)
            {
                meetings.Add(Meeting(MeetingKind.Review, $"{iterationName}: Sprint review", day, at, durations.ReviewMinutes, zone));
                at = at.AddMinutes(durations.ReviewMinutes);
            }

            meetings.Add(Meeting(MeetingKind.Retrospective, $"{iterationName}: Sprint retrospective", day, at, durations.RetroMinutes, zone));
        }

        return [.. meetings.OrderBy(m => m.StartsAt)];
    }

    private static CalendarMeeting Meeting(MeetingKind kind, string title, DateOnly day, DateTime localStart, int minutes, TimeZoneInfo zone)
    {
        // A local time skipped by a daylight-saving jump moves forward to the first valid time.
        var local = zone.IsInvalidTime(localStart) ? localStart.AddHours(1) : localStart;
        var starts = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
        return new CalendarMeeting($"{kind}:{day:yyyy-MM-dd}", kind, title, starts, starts.AddMinutes(minutes));
    }
}
