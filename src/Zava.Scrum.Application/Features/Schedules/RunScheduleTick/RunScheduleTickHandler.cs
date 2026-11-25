using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups.JoinMeeting;
using Zava.Scrum.Application.Features.Standups.PrepareStandup;
using Zava.Scrum.Application.Features.Standups.StartStandup;
using Zava.Scrum.Domain.Capacity;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Schedules;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Schedules.RunScheduleTick;

/// <summary>
/// A process manager: asks the pure <see cref="ScheduleEvaluator"/> what is due per team (each in its own time zone, only for active teams
/// whose daily scrum is on) and sends the existing commands for it. Starts go through the pure <see cref="CapacityGovernor"/>: a limit on
/// concurrent standups, a stagger between starts, a visible waiting reason, and a notification when a standup cannot start. A ceremony is
/// never dropped silently. It uses <see cref="ISender"/> rather than calling other handlers (ADR 0006); a failure leaves the day unmarked so
/// the next tick retries.
/// </summary>
internal sealed class RunScheduleTickHandler(
    IScheduleRepository schedules,
    ITeamRepository teams,
    ISessionRepository sessions,
    ITeamsMessenger messenger,
    ISender sender,
    IBridgeAccess bridge,
    ICapacitySettings capacity,
    IClock clock,
    ILogger<RunScheduleTickHandler> logger) : IRequestHandler<RunScheduleTickCommand, Result<int>>
{
    public async Task<Result<int>> Handle(RunScheduleTickCommand request, CancellationToken ct)
    {
        var ran = 0;
        var startsDue = new List<(Schedule Schedule, Team Team, DateTime Local, DueStandup Due)>();
        foreach (var schedule in await schedules.ListAsync(ct))
        {
            var team = await teams.GetAsync(schedule.TeamId, ct);
            if (team is null || !team.IsSchedulable || !team.CeremonyFlags.DailyScrum)
            {
                continue;
            }

            var zone = ResolveZone(team.TimeZone);
            var local = TimeZoneInfo.ConvertTimeFromUtc(clock.UtcNow.UtcDateTime, zone);
            switch (ScheduleEvaluator.Evaluate(schedule, local))
            {
                case ScheduleAction.Prepare:
                    ran += await SaveAsync(await PrepareAsync(schedule, local, ct), ct);
                    break;
                case ScheduleAction.Start:
                    var dueAt = new DateTimeOffset(DateOnly.FromDateTime(local).ToDateTime(schedule.StartTime), zone.GetUtcOffset(local));
                    startsDue.Add((schedule, team, local, new DueStandup(team.Id, dueAt)));
                    break;
                case ScheduleAction.None:
                    break;
            }
        }

        return Result<int>.Success(ran + await StartWithinCapacityAsync(startsDue, ct));
    }

    private static TimeZoneInfo ResolveZone(string id) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) ? zone : TimeZoneInfo.Utc;

    private async Task<int> StartWithinCapacityAsync(List<(Schedule Schedule, Team Team, DateTime Local, DueStandup Due)> due, CancellationToken ct)
    {
        if (due.Count == 0)
        {
            return 0;
        }

        var all = await sessions.ListAsync(ct);
        var running = all.Count(s => s.Status == StandupStatus.Live);
        var lastStart = all.Where(s => s.StartedAt is not null).Select(s => s.StartedAt).Max();
        var plan = CapacityGovernor.Plan(due.Select(d => d.Due).ToList(), running, lastStart, clock.UtcNow, capacity.Limits);
        var started = 0;
        foreach (var decision in plan)
        {
            var item = due.First(d => d.Team.Id == decision.TeamId);
            if (decision.Start)
            {
                started += await SaveAsync(await StartAsync(item.Schedule, item.Local, ct), ct);
                continue;
            }

            logger.LogInformation("Standup for team {TeamId} is waiting: {Reason}", item.Team.Id, decision.Reason);
            if (item.Schedule.WaitingReason != decision.Reason)
            {
                await schedules.SaveAsync(item.Schedule with { WaitingReason = decision.Reason }, ct);
            }

            if (decision.Blocked)
            {
                await NotifyBlockedAsync(item.Team, item.Schedule, decision.Reason!, ct);
            }
        }

        return started;
    }

    private async Task NotifyBlockedAsync(Team team, Schedule schedule, string reason, CancellationToken ct)
    {
        logger.LogWarning("Standup for team {TeamId} cannot start: {Reason}", team.Id, reason);
        await messenger.SendAsync(
            new TeamsMessage(CardKind.Recap, team.Id, schedule.PreparedSessionId ?? Guid.Empty, $"capacity:{team.Id}:{DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)}", "Standup could not start",
                $"The standup is waiting. {reason} It will start as soon as there is room, and you can start it from the portal.", [], null, null, CardRoute.Recap, team.RecapChannelKey),
            ct);
    }

    private async Task<int> SaveAsync(Schedule? next, CancellationToken ct)
    {
        if (next is null)
        {
            return 0;
        }

        await schedules.SaveAsync(next, ct);
        return 1;
    }

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

        var started = await sender.Send(new StartStandupCommand(id, s.Mode, s.JoinUrlFor(DateOnly.FromDateTime(local))), ct);
        if (!started.IsSuccess)
        {
            return null;
        }

        var joined = await sender.Send(new JoinMeetingCommand(id, bridge.Issue(id), bridge.ApiBase), ct);
        return joined.IsSuccess ? s with { StartedFor = DateOnly.FromDateTime(local), WaitingReason = null } : null;
    }
}
