using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Calendars.ProposeSprintCalendar;

/// <summary>Stores a proposed sprint calendar and sends the "Proposed sprint calendar" card to the Scrum Master. Nothing is created in any calendar.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="Iteration">The iteration's short name or path; the next iteration when omitted.</param>
public sealed record ProposeSprintCalendarCommand(string TeamId, string? Iteration = null) : ICommand<Guid>;
