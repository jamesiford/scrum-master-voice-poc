namespace Zava.Scrum.Domain.Schedules;

/// <summary>When a team's standup runs and how it starts (SPEC 15, <c>schedules</c> container).</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Enabled">Whether the scheduler acts on it.</param>
/// <param name="Days">Days the standup runs.</param>
/// <param name="StartTime">Local start time in the team's time zone.</param>
/// <param name="Mode">Bridge mode: <c>simulated</c> (prepare only; a person starts the room) or <c>acs</c> (prepare, then join the Teams meeting).</param>
/// <param name="JoinUrl">Teams meeting link, used by <c>acs</c> mode.</param>
/// <param name="PreparedFor">The date a session was last prepared for.</param>
/// <param name="PreparedSessionId">The session prepared for that date.</param>
/// <param name="StartedFor">The date the session was last started for.</param>
/// <param name="WaitingReason">Why a due standup is waiting (capacity or stagger), shown in the portal; cleared when it starts.</param>
/// <param name="Meetings">Teams meetings created from an approved sprint calendar (M9d): the join URL for each standup date, so the scheduler joins without a pasted link.</param>
public sealed record Schedule(
    string TeamId,
    bool Enabled,
    IReadOnlyList<DayOfWeek> Days,
    TimeOnly StartTime,
    string Mode,
    string? JoinUrl,
    DateOnly? PreparedFor = null,
    Guid? PreparedSessionId = null,
    DateOnly? StartedFor = null,
    IReadOnlyList<ScheduledMeeting>? Meetings = null,
    string? WaitingReason = null)
{
    /// <summary>Gets the join URL for a day: the calendar's meeting for that date, else the pasted <see cref="JoinUrl"/>.</summary>
    /// <param name="day">The standup date.</param>
    /// <returns>The URL, or <see langword="null"/> when there is none.</returns>
    public string? JoinUrlFor(DateOnly day) => Meetings?.FirstOrDefault(m => m.Date == day)?.JoinUrl ?? JoinUrl;

    /// <summary>Creates the default schedule: off, weekdays 09:30, simulated.</summary>
    /// <param name="teamId">Team id.</param>
    /// <returns>The schedule.</returns>
    public static Schedule Default(string teamId) =>
        new(teamId, false, [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday], new TimeOnly(9, 30), "simulated", null);
}
