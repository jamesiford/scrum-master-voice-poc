using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Calendars;

namespace Mcm.Scrum.Application.Features.Calendars.ListSprintCalendars;

/// <summary>Lists the team's proposed and decided sprint calendars, newest first.</summary>
/// <param name="TeamId">The team.</param>
public sealed record ListSprintCalendarsQuery(string TeamId) : IQuery<IReadOnlyList<SprintCalendarData>>;
