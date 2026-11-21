using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Features.Teams.GetRoster;
using Mcm.Scrum.Application.Features.Teams.GetTeam;
using Mcm.Scrum.Application.Features.Teams.SyncRoster;
using Mcm.Scrum.Application.Features.Teams.UpdateTeamSettings;

namespace Mcm.Scrum.Api.Endpoints;

/// <summary>Team settings and roster endpoints (SPEC 14). Each route sends exactly one command or query.</summary>
public static class TeamsEndpoints
{
    /// <summary>Maps <c>/api/teams</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams");

        group.MapGet("/{id}", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTeamQuery(id), ct)).ToHttpResult());

        group.MapPut("/{id}", async (string id, UpdateTeamSettingsBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(body.ToCommand(id), ct)).ToHttpResult());

        group.MapGet("/{id}/members", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetRosterQuery(id), ct)).ToHttpResult());

        group.MapPost("/{id}/members/sync", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SyncRosterCommand(id), ct)).ToHttpResult());

        return app;
    }

    /// <summary>Request body for <c>PUT /api/teams/{id}</c>.</summary>
    /// <param name="Name">Display name.</param>
    /// <param name="TimeZone">IANA time zone.</param>
    /// <param name="StandupMinutes">Standup length.</param>
    /// <param name="Project">Azure DevOps project.</param>
    /// <param name="AdoTeam">Azure DevOps team.</param>
    /// <param name="AreaPath">Area path.</param>
    /// <param name="StaleDays">Days in state before an item is stale.</param>
    /// <param name="EscalateBlockerAfterDays">Days before a blocker is escalated.</param>
    /// <param name="SprintGoal">Sprint goal.</param>
    public sealed record UpdateTeamSettingsBody(
        string Name,
        string TimeZone,
        int StandupMinutes,
        string Project,
        string AdoTeam,
        string AreaPath,
        int StaleDays,
        int EscalateBlockerAfterDays,
        string SprintGoal)
    {
        /// <summary>Maps the body to the command.</summary>
        /// <param name="teamId">Team id from the route.</param>
        /// <returns>The command.</returns>
        public UpdateTeamSettingsCommand ToCommand(string teamId) =>
            new(teamId, Name, TimeZone, StandupMinutes, Project, AdoTeam, AreaPath, StaleDays, EscalateBlockerAfterDays, SprintGoal);
    }
}
