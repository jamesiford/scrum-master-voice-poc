using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Calendars.ApproveSprintCalendar;

/// <summary>
/// Approves a proposed sprint calendar: creates each meeting as a Teams meeting in the agent's calendar (<c>ICalendarWriter</c>), invites the roster,
/// and stores the join URLs on the team's schedule so the scheduler joins without a pasted link.
/// </summary>
/// <param name="CalendarId">The calendar.</param>
/// <param name="Decision">Who approved it and how that was proven.</param>
public sealed record ApproveSprintCalendarCommand(Guid CalendarId, DecisionContext Decision) : ICommand;
