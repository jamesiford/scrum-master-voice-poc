using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Application.Features.Standups.JoinMeeting;
using Mcm.Scrum.Application.Features.Standups.PrepareStandup;
using Mcm.Scrum.Application.Features.Standups.StartStandup;
using Mcm.Scrum.Domain.Schedules;

namespace Mcm.Scrum.Application.Features.Schedules.RunScheduleTick;

/// <summary>
/// A process manager: asks the pure <see cref="ScheduleEvaluator"/> what is due and sends the existing commands for it.
/// It uses <see cref="ISender"/> rather than calling other handlers (ADR 0006). A failure leaves the day unmarked so the next tick retries.
/// </summary>
internal sealed class RunScheduleTickHandler(
    IScheduleRepository schedules,
    ITeamRepository teams,
    ISender sender,
    IBridgeAccess bridge,
    IClock clock) : IRequestHandler<RunScheduleTickCommand, Result<int>>
{
    public async Task<Result<int>> Handle(RunScheduleTickCommand request, CancellationToken ct)
    {
        var ran = 0;
        foreach (var schedule in await schedules.ListAsync(ct))
        {
            var team = await teams.GetAsync(schedule.TeamId, ct);
            if (team is null)
            {
                continue;
            }

            var local = TimeZoneInfo.ConvertTimeFromUtc(clock.UtcNow.UtcDateTime, ResolveZone(team.TimeZone));
            var next = ScheduleEvaluator.Evaluate(schedule, local) switch
            {
                ScheduleAction.Prepare => await PrepareAsync(schedule, local, ct),
                ScheduleAction.Start => await StartAsync(schedule, local, ct),
                _ => null,
            };
            if (next is not null)
            {
                await schedules.SaveAsync(next, ct);
                ran++;
            }
        }

        return Result<int>.Success(ran);
    }

    private static TimeZoneInfo ResolveZone(string id) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) ? zone : TimeZoneInfo.Utc;

    private async Task<Schedule?> PrepareAsync(Schedule s, DateTime local, CancellationToken ct)
    {
        var prepared = await sender.Send(new PrepareStandupCommand(s.TeamId), ct);
        return prepared.IsSuccess ? s with { PreparedFor = DateOnly.FromDateTime(local), PreparedSessionId = prepared.Value } : null;
    }

    private async Task<Schedule?> StartAsync(Schedule s, DateTime local, CancellationToken ct)
    {
        if (s.PreparedSessionId is not { } id)
        {
            return null;
        }

        var started = await sender.Send(new StartStandupCommand(id, s.Mode, s.JoinUrl), ct);
        if (!started.IsSuccess)
        {
            return null;
        }

        var joined = await sender.Send(new JoinMeetingCommand(id, bridge.Issue(id), bridge.ApiBase), ct);
        return joined.IsSuccess ? s with { StartedFor = DateOnly.FromDateTime(local) } : null;
    }
}
