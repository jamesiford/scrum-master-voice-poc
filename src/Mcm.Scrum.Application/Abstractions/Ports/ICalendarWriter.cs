using Mcm.Scrum.Domain.Calendars;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>A meeting to create in the agent's calendar.</summary>
/// <param name="TransactionId">Stable id so creating the same meeting twice makes one (Graph <c>transactionId</c>).</param>
/// <param name="Subject">Title.</param>
/// <param name="StartsAt">Start (UTC).</param>
/// <param name="EndsAt">End (UTC).</param>
/// <param name="Attendees">Email addresses of the roster to invite.</param>
public sealed record MeetingInvite(string TransactionId, string Subject, DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<string> Attendees);

/// <summary>A meeting that now exists.</summary>
/// <param name="EventId">The calendar event id.</param>
/// <param name="JoinUrl">The Teams join URL.</param>
public sealed record CreatedMeeting(string EventId, string JoinUrl);

/// <summary>Creates Teams meetings in the agent's calendar. Called only for meetings returned by <see cref="SprintCalendar.Approve"/>.</summary>
public interface ICalendarWriter
{
    /// <summary>Gets a value indicating whether a calendar is configured; when not, the <c>.ics</c> download is the fallback.</summary>
    bool Enabled { get; }

    /// <summary>Creates one Teams online meeting and invites the attendees.</summary>
    /// <param name="invite">The meeting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created meeting, or an external-rejected error.</returns>
    Task<Result<CreatedMeeting>> CreateMeetingAsync(MeetingInvite invite, CancellationToken ct);
}

/// <summary>Settings for the sprint calendar (<c>Calendar:*</c> and <c>Team:HasSprintReview</c>). A port so handlers never read configuration.</summary>
public interface ICalendarSettings
{
    /// <summary>Gets the sprint planning length in minutes.</summary>
    int PlanningMinutes { get; }

    /// <summary>Gets the sprint review length in minutes.</summary>
    int ReviewMinutes { get; }

    /// <summary>Gets the sprint retrospective length in minutes.</summary>
    int RetroMinutes { get; }

    /// <summary>Gets a value indicating whether the team holds a sprint review.</summary>
    bool HasSprintReview { get; }
}
