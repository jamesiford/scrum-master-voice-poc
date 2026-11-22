using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Calendars;

/// <summary>Life-cycle of a proposed sprint calendar.</summary>
public enum SprintCalendarStatus
{
    /// <summary>Waiting for the Scrum Master.</summary>
    Proposed,

    /// <summary>Approved; some meetings may still be waiting to be created (a retry creates the rest).</summary>
    Approving,

    /// <summary>Approved and every meeting was created.</summary>
    Approved,

    /// <summary>Rejected; nothing was created.</summary>
    Rejected,
}

/// <summary>Everything persisted about a <see cref="SprintCalendar"/>.</summary>
/// <param name="Id">Calendar id.</param>
/// <param name="TeamId">Team id.</param>
/// <param name="IterationPath">The iteration path.</param>
/// <param name="TimeZone">The team's time zone, used to show times.</param>
/// <param name="CreatedAt">When proposed.</param>
/// <param name="Status">Status.</param>
/// <param name="Meetings">The meetings.</param>
/// <param name="DecidedBy">Who approved or rejected it.</param>
/// <param name="DecidedAt">When.</param>
/// <param name="ApplyError">Why the last attempt to create meetings stopped.</param>
public sealed record SprintCalendarData(
    Guid Id,
    string TeamId,
    string IterationPath,
    string TimeZone,
    DateTimeOffset CreatedAt,
    SprintCalendarStatus Status,
    IReadOnlyList<CalendarMeeting> Meetings,
    string? DecidedBy,
    DateTimeOffset? DecidedAt,
    string? ApplyError);

/// <summary>Raised when a sprint calendar is proposed. A handler sends the card to the Scrum Master.</summary>
/// <param name="CalendarId">The calendar.</param>
/// <param name="OccurredAt">When.</param>
public sealed record SprintCalendarProposed(Guid CalendarId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// A proposed sprint calendar. Like follow-ups, nothing is created in anyone's calendar unless a named person approved it: the only way to
/// get meetings to create is <see cref="Approve"/>.
/// </summary>
public sealed class SprintCalendar : AggregateRoot
{
    private SprintCalendar(SprintCalendarData data) => Data = data;

    /// <summary>Gets the persisted state.</summary>
    public SprintCalendarData Data { get; private set; }

    /// <summary>Gets the id.</summary>
    public Guid Id => Data.Id;

    /// <summary>Proposes a calendar and raises <see cref="SprintCalendarProposed"/>.</summary>
    /// <param name="id">Id.</param>
    /// <param name="teamId">Team.</param>
    /// <param name="iterationPath">Iteration path.</param>
    /// <param name="timeZone">The team's time zone.</param>
    /// <param name="meetings">The meetings.</param>
    /// <param name="now">Current time.</param>
    /// <returns>The calendar.</returns>
    public static SprintCalendar Propose(Guid id, string teamId, string iterationPath, string timeZone, IReadOnlyList<CalendarMeeting> meetings, DateTimeOffset now)
    {
        var calendar = new SprintCalendar(new SprintCalendarData(id, teamId, iterationPath, timeZone, now, SprintCalendarStatus.Proposed, meetings, null, null, null));
        calendar.Raise(new SprintCalendarProposed(id, now));
        return calendar;
    }

    /// <summary>Rehydrates a persisted calendar.</summary>
    /// <param name="data">Persisted state.</param>
    /// <returns>The calendar.</returns>
    public static SprintCalendar Restore(SprintCalendarData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new SprintCalendar(data);
    }

    /// <summary>Approves the calendar and returns the meetings still to create. A retry after a partial failure returns only the rest.</summary>
    /// <param name="approver">Who approved (display name).</param>
    /// <param name="now">Current time.</param>
    /// <returns>The meetings to create, or a validation or conflict error.</returns>
    public Result<IReadOnlyList<CalendarMeeting>> Approve(string approver, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(approver))
        {
            return Result<IReadOnlyList<CalendarMeeting>>.Failure(SprintCalendarErrors.ApproverRequired);
        }

        if (Data.Status is not (SprintCalendarStatus.Proposed or SprintCalendarStatus.Approving))
        {
            return Result<IReadOnlyList<CalendarMeeting>>.Failure(SprintCalendarErrors.AlreadyDecided(Data.Status));
        }

        Data = Data with { Status = SprintCalendarStatus.Approving, DecidedBy = approver.Trim(), DecidedAt = now };
        return Result<IReadOnlyList<CalendarMeeting>>.Success([.. Data.Meetings.Where(m => !m.Created)]);
    }

    /// <summary>Records that a meeting now exists in the calendar.</summary>
    /// <param name="key">The meeting key.</param>
    /// <param name="eventId">The calendar event id.</param>
    /// <param name="joinUrl">The Teams join URL.</param>
    public void RecordCreated(string key, string eventId, string joinUrl) =>
        Data = Data with { Meetings = [.. Data.Meetings.Select(m => m.Key == key ? m with { EventId = eventId, JoinUrl = joinUrl } : m)], ApplyError = null };

    /// <summary>Records why creating the meetings stopped; the calendar stays approving so a retry creates the rest.</summary>
    /// <param name="error">The reason.</param>
    public void RecordFailure(string error) => Data = Data with { ApplyError = error };

    /// <summary>Marks the calendar approved once every meeting exists.</summary>
    public void Complete()
    {
        if (Data.Status == SprintCalendarStatus.Approving && Data.Meetings.All(m => m.Created))
        {
            Data = Data with { Status = SprintCalendarStatus.Approved, ApplyError = null };
        }
    }

    /// <summary>Rejects the calendar. Nothing was created, so nothing is undone.</summary>
    /// <param name="approver">Who rejected it.</param>
    /// <param name="now">Current time.</param>
    /// <returns>Success, or a validation or conflict error.</returns>
    public Result Reject(string approver, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(approver))
        {
            return Result.Failure(SprintCalendarErrors.ApproverRequired);
        }

        if (Data.Status != SprintCalendarStatus.Proposed)
        {
            return Result.Failure(SprintCalendarErrors.AlreadyDecided(Data.Status));
        }

        Data = Data with { Status = SprintCalendarStatus.Rejected, DecidedBy = approver.Trim(), DecidedAt = now };
        return Result.Success();
    }
}

/// <summary>Errors for sprint calendars.</summary>
public static class SprintCalendarErrors
{
    /// <summary>Gets the error for a missing approver name.</summary>
    public static Error ApproverRequired { get; } = Error.Invalid("calendar.approver_required", "A named person must approve or reject the calendar.");

    /// <summary>Creates the error for a calendar that was already decided.</summary>
    /// <param name="status">The status.</param>
    /// <returns>The error.</returns>
    public static Error AlreadyDecided(SprintCalendarStatus status) => Error.Conflict("calendar.already_decided", $"The calendar is already {status}.");

    /// <summary>Creates the error for an unknown calendar.</summary>
    /// <param name="id">The id.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(Guid id) => Error.NotFound("calendar.not_found", $"Sprint calendar '{id}' does not exist.");

    /// <summary>Gets the error for a sprint with no working day or no iteration dates.</summary>
    public static Error NothingToSchedule { get; } = Error.NotFound("calendar.nothing_to_schedule", "No iteration with dates and a working day was found for the team.");

    /// <summary>Gets the error for Graph not being configured.</summary>
    public static Error NotConfigured { get; } = Error.Conflict("calendar.not_configured", "Microsoft Graph calendar is not configured (Calendar:Organizer). Download /api/calendar/{iteration}.ics and import it instead.");
}
