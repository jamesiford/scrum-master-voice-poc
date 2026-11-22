namespace Zava.Scrum.Domain.Calendars;

/// <summary>One meeting in a proposed sprint calendar.</summary>
/// <param name="Key">Stable key, <c>{kind}:{yyyy-MM-dd}</c>; it makes creating the meeting idempotent.</param>
/// <param name="Kind">The ceremony.</param>
/// <param name="Title">The meeting title.</param>
/// <param name="StartsAt">Start (UTC).</param>
/// <param name="EndsAt">End (UTC).</param>
/// <param name="EventId">The calendar event id once the meeting was created.</param>
/// <param name="JoinUrl">The Teams join URL once the meeting was created.</param>
public sealed record CalendarMeeting(string Key, MeetingKind Kind, string Title, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? EventId = null, string? JoinUrl = null)
{
    /// <summary>Gets a value indicating whether the meeting exists in the calendar.</summary>
    public bool Created => EventId is not null;

    /// <summary>Gets the date of the meeting in a time zone.</summary>
    /// <param name="zone">The zone.</param>
    /// <returns>The local date.</returns>
    public DateOnly LocalDate(TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(StartsAt, zone).DateTime);
}
