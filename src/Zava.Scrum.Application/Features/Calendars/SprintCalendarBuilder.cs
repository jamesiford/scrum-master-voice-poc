using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Calendars;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Schedules;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Features.Calendars;

/// <summary>The plan for one iteration.</summary>
/// <param name="Team">The team.</param>
/// <param name="Iteration">The iteration with its dates.</param>
/// <param name="Meetings">The planned meetings.</param>
internal sealed record CalendarPlan(Team Team, IterationInfo Iteration, IReadOnlyList<CalendarMeeting> Meetings);

/// <summary>
/// Gathers the inputs (team, schedule, iteration dates, settings) and runs the pure <see cref="SprintCalendarPlanner"/>. Shared by the query and
/// the command because handlers never call other handlers.
/// </summary>
internal sealed class SprintCalendarBuilder(ITeamRepository teams, IScheduleRepository schedules, IIterationReader iterations, ICalendarSettings settings)
{
    public async Task<Result<CalendarPlan>> BuildAsync(string teamId, string? iteration, CancellationToken ct)
    {
        var team = await teams.GetAsync(teamId, ct);
        if (team is null)
        {
            return Result<CalendarPlan>.Failure(Features.Teams.TeamErrors.NotFound(teamId));
        }

        var found = await iterations.FindIterationAsync(team, iteration, ct);
        if (found is null)
        {
            return Result<CalendarPlan>.Failure(SprintCalendarErrors.NothingToSchedule);
        }

        var schedule = await schedules.GetAsync(teamId, ct) ?? Schedule.Default(teamId);
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(team.TimeZone, out var z) ? z : TimeZoneInfo.Utc;
        var durations = new CalendarDurations(settings.PlanningMinutes, team.StandupMinutes, settings.ReviewMinutes, settings.RetroMinutes, settings.HasSprintReview);
        var meetings = SprintCalendarPlanner.Plan(found.Name, found.Start, found.End, zone, schedule.StartTime, schedule.Days, durations);
        return meetings.Count == 0
            ? Result<CalendarPlan>.Failure(SprintCalendarErrors.NothingToSchedule)
            : Result<CalendarPlan>.Success(new CalendarPlan(team, found, meetings));
    }
}
