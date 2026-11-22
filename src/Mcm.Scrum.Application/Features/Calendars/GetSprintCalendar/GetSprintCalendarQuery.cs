using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Calendars;

namespace Mcm.Scrum.Application.Features.Calendars.GetSprintCalendar;

/// <summary>Computes the sprint's calendar from its iteration dates and the team's settings, and formats it as an <c>.ics</c> file. A query: nothing is stored or sent.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="Iteration">The iteration's short name or path; the next iteration when omitted.</param>
public sealed record GetSprintCalendarQuery(string TeamId, string? Iteration = null) : IQuery<SprintCalendarDto>;

/// <summary>A planned sprint calendar.</summary>
/// <param name="IterationPath">The iteration path.</param>
/// <param name="TimeZone">The team's time zone.</param>
/// <param name="Meetings">The planned meetings.</param>
/// <param name="Ics">The calendar as an iCalendar file, for the fallback download.</param>
public sealed record SprintCalendarDto(string IterationPath, string TimeZone, IReadOnlyList<CalendarMeeting> Meetings, string Ics);
