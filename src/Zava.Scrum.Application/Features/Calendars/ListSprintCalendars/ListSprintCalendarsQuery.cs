using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Calendars;

namespace Zava.Scrum.Application.Features.Calendars.ListSprintCalendars;

/// <summary>Lists the team's proposed and decided sprint calendars, newest first.</summary>
/// <param name="TeamId">The team.</param>
public sealed record ListSprintCalendarsQuery(string TeamId) : IQuery<IReadOnlyList<SprintCalendarData>>;
