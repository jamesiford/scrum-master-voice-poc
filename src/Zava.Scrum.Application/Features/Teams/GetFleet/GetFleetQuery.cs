using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.GetFleet;

/// <summary>The Admin fleet dashboard: one row per team.</summary>
public sealed record GetFleetQuery : IQuery<IReadOnlyList<FleetRow>>;

/// <summary>One team on the fleet dashboard.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Life cycle.</param>
/// <param name="Organisation">The Azure DevOps organisation.</param>
/// <param name="Project">The Azure DevOps project.</param>
/// <param name="NextStandup">Local start time and days of the schedule, or <see langword="null"/> when none is enabled.</param>
/// <param name="LastRun">When the team's latest session was created.</param>
/// <param name="OpenFollowUps">Follow-ups awaiting a decision.</param>
/// <param name="Connection">The live connection test: <c>ok</c>, or the step that failed.</param>
/// <param name="Bound">How many Teams conversations are bound.</param>
public sealed record FleetRow(string TeamId, string Name, TeamStatus Status, string Organisation, string Project, string? NextStandup, DateTimeOffset? LastRun, int OpenFollowUps, string Connection, int Bound);

internal sealed class GetFleetHandler(ITeamRepository teams, IScheduleRepository schedules, ISessionRepository sessions, IFollowUpRepository followUps, IAdoConnectionTester tester)
    : IRequestHandler<GetFleetQuery, Result<IReadOnlyList<FleetRow>>>
{
    public async Task<Result<IReadOnlyList<FleetRow>>> Handle(GetFleetQuery request, CancellationToken ct)
    {
        var all = await teams.ListAsync(ct);
        var allSessions = await sessions.ListAsync(ct);
        var rows = await Task.WhenAll(all.Select(t => RowAsync(t, allSessions, ct)));
        return Result<IReadOnlyList<FleetRow>>.Success(rows);
    }

    private async Task<FleetRow> RowAsync(Team team, IReadOnlyList<Zava.Scrum.Domain.Standups.StandupSession> allSessions, CancellationToken ct)
    {
        var schedule = await schedules.GetAsync(team.Id, ct);
        var open = (await followUps.ListAsync(team.Id, null, ct)).Count(f => f.Status is FollowUpStatus.Pending or FollowUpStatus.Sent);
        var last = allSessions.Where(s => s.TeamId == team.Id).Select(s => (DateTimeOffset?)s.CreatedAt).Max();
        return new FleetRow(team.Id, team.Name, team.Status, team.Ado.Org, team.Ado.Project,
            schedule is { Enabled: true } ? $"{schedule.StartTime} {team.TimeZone}" : null, last, open, await HealthAsync(team, ct), team.Bindings.Count);
    }

    // A broken connection is a state, not an error: the dashboard shows which step failed instead of failing itself.
    private async Task<string> HealthAsync(Team team, CancellationToken ct)
    {
        if (team.Status is TeamStatus.Draft or TeamStatus.Archived)
        {
            return "not checked";
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var report = await tester.TestAsync(team.Ado, timeout.Token);
            return report.Ok ? "ok" : $"failed at {report.Failed!.Name}: {report.Failed.Detail}";
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return "failed: timed out";
        }
    }
}
