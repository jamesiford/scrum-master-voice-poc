using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Calendars.RejectSprintCalendar;

/// <summary>Rejects a proposed sprint calendar. Nothing was created, so nothing is undone.</summary>
/// <param name="CalendarId">The calendar.</param>
/// <param name="Rejecter">Who rejected it (display name).</param>
public sealed record RejectSprintCalendarCommand(Guid CalendarId, string Rejecter) : ICommand;
