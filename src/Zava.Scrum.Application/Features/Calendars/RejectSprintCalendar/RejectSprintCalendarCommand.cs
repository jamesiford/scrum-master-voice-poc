using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Calendars.RejectSprintCalendar;

/// <summary>Rejects a proposed sprint calendar. Nothing was created, so nothing is undone.</summary>
/// <param name="CalendarId">The calendar.</param>
/// <param name="Decision">Who rejected it and how that was proven.</param>
public sealed record RejectSprintCalendarCommand(Guid CalendarId, DecisionContext Decision) : ICommand;
