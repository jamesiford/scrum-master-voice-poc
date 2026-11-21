using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Application.Features.Teams;
using Mcm.Scrum.Domain.Schedules;

namespace Mcm.Scrum.Application.Features.Schedules.UpdateSchedule;

/// <summary>Saves the schedule and keeps the per-day run state, so editing does not re-trigger today's standup.</summary>
internal sealed class UpdateScheduleHandler(IScheduleRepository schedules, ITeamRepository teams) : IRequestHandler<UpdateScheduleCommand, Result>
{
    public async Task<Result> Handle(UpdateScheduleCommand request, CancellationToken ct)
    {
        if (await teams.GetAsync(request.TeamId, ct) is null)
        {
            return Result.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var existing = await schedules.GetAsync(request.TeamId, ct);
        var days = request.Days.Select(Enum.Parse<DayOfWeek>).Distinct().ToList();
        var updated = (existing ?? Schedule.Default(request.TeamId)) with
        {
            Enabled = request.Enabled,
            Days = days,
            StartTime = TimeOnly.ParseExact(request.StartTime, "HH:mm"),
            Mode = request.Mode,
            JoinUrl = string.IsNullOrWhiteSpace(request.JoinUrl) ? null : request.JoinUrl.Trim(),
        };
        await schedules.SaveAsync(updated, ct);
        return Result.Success();
    }
}