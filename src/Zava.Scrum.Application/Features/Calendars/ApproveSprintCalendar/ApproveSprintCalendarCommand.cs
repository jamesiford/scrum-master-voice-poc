using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Calendars.ApproveSprintCalendar;

/// <summary>
/// Approves a proposed sprint calendar: creates each meeting as a Teams meeting in the agent's calendar (<c>ICalendarWriter</c>), invites the roster,
/// and stores the join URLs on the team's schedule so the scheduler joins without a pasted link.
/// </summary>
/// <param name="CalendarId">The calendar.</param>
/// <param name="Approver">Who approved it (display name).</param>
public sealed record ApproveSprintCalendarCommand(Guid CalendarId, string Approver) : ICommand;
