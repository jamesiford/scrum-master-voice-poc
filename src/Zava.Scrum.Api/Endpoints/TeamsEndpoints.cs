using Zava.Scrum.Api.Auth;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Application.Features.Teams.AdoDiscovery;
using Zava.Scrum.Application.Features.Teams.ArchiveTeam;
using Zava.Scrum.Application.Features.Teams.CreateTeam;
using Zava.Scrum.Application.Features.Teams.GetFleet;
using Zava.Scrum.Application.Features.Teams.ListTeams;
using Zava.Scrum.Application.Features.Teams.Memberships;
using Zava.Scrum.Application.Features.Teams.TestConnection;
using Zava.Scrum.Application.Features.Teams.UpdateTeam;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Application.Features.Teams.GetRoster;
using Zava.Scrum.Application.Features.Teams.GetTeam;
using Zava.Scrum.Application.Features.Teams.SetRecapChannel;
using Zava.Scrum.Application.Features.Teams.SyncRoster;
using Zava.Scrum.Application.Features.Teams.UpdateMemberAvailability;
using Zava.Scrum.Application.Features.Teams.UpdateTeamSettings;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Team settings and roster endpoints (SPEC 14). Each route sends exactly one command or query.</summary>
public static class TeamsEndpoints
{
    /// <summary>Maps <c>/api/teams</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams");

        // The registry (ADR 0018). The list names no team: it answers with only the caller's teams (all of them for an Admin).
        group.MapGet("/", async (bool? includeArchived, HttpContext http, ISender sender, CancellationToken ct) =>
            CallerContextFactory.From(http.User) is { } caller
                ? (await sender.Send(new ListTeamsQuery(caller, includeArchived == true), ct)).ToHttpResult()
                : Results.Unauthorized()).NoTeamScope();

        group.MapPost("/", async (CreateTeamBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateTeamCommand(body.TeamId, body.Input), ct)).ToHttpResult()).RequireRegistry();

        group.MapGet("/{id}", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTeamQuery(id), ct)).ToHttpResult());

        group.MapPut("/{id}", async (string id, TeamInput body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateTeamCommand(id, body), ct)).ToHttpResult()).RequireRegistry();

        // Archive, never a hard delete.
        group.MapDelete("/{id}", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ArchiveTeamCommand(id), ct)).ToHttpResult()).RequireRegistry();

        group.MapPost("/{id}/test-connection", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new TestTeamConnectionQuery(id), ct)).ToHttpResult());

        // Behaviour thresholds, name, time zone and goal: a team lead's. The Azure DevOps connection is not here.
        group.MapPut("/{id}/settings", async (string id, UpdateTeamSettingsBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(body.ToCommand(id), ct)).ToHttpResult());

        group.MapGet("/{id}/memberships", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListMembershipsQuery(id), ct)).ToHttpResult());

        group.MapPut("/{id}/memberships/{objectId:guid}", async (string id, Guid objectId, MembershipBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetMembershipCommand(id, objectId, body.Role), ct)).ToHttpResult());

        group.MapDelete("/{id}/memberships/{objectId:guid}", async (string id, Guid objectId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RemoveMembershipCommand(id, objectId), ct)).ToHttpResult());

        group.MapPost("/{id}/memberships/sync", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SyncMembershipsFromGroupCommand(id), ct)).ToHttpResult());

        group.MapGet("/{id}/members", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetRosterQuery(id), ct)).ToHttpResult());

        group.MapPost("/{id}/members/sync", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SyncRosterCommand(id), ct)).ToHttpResult());

        group.MapPut("/{id}/members/{memberId}/availability", async (string id, string memberId, AvailabilityBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateMemberAvailabilityCommand(id, memberId, body.Percent), ct)).ToHttpResult());

        group.MapPut("/{id}/recap-channel", async (string id, RecapChannelBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetRecapChannelCommand(id, body.ConversationId), ct)).ToHttpResult());

        // Across teams: the fleet dashboard and the onboarding pickers read organisations, so they are Admin only.
        app.MapGet("/api/fleet", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetFleetQuery(), ct)).ToHttpResult()).RequireRegistry();

        app.MapGet("/api/ado/projects", async (string organisation, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListAdoProjectsQuery(organisation), ct)).ToHttpResult()).RequireRegistry();

        app.MapGet("/api/ado/teams", async (string organisation, string project, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListAdoTeamsQuery(organisation, project), ct)).ToHttpResult()).RequireRegistry();

        app.MapPost("/api/ado/test-connection", async (TestConnectionBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new TestTeamConnectionQuery(null, body.Ado), ct)).ToHttpResult()).RequireRegistry();

        return app;
    }

    /// <summary>Request body for <c>POST /api/teams</c>.</summary>
    /// <param name="TeamId">The new team's id.</param>
    /// <param name="Input">The team's details.</param>
    public sealed record CreateTeamBody(string TeamId, TeamInput Input);

    /// <summary>Request body for <c>PUT /api/teams/{id}/memberships/{objectId}</c>.</summary>
    /// <param name="Role">The role in the team.</param>
    public sealed record MembershipBody(TeamRole Role);

    /// <summary>Request body for <c>POST /api/ado/test-connection</c>.</summary>
    /// <param name="Ado">The connection to test.</param>
    public sealed record TestConnectionBody(AdoSettings Ado);
    /// <summary>Request body for <c>PUT /api/teams/{id}/members/{memberId}/availability</c>.</summary>
    /// <param name="Percent">Availability, 0 to 100.</param>
    public sealed record AvailabilityBody(int Percent);

    /// <summary>Request body for <c>PUT /api/teams/{id}/recap-channel</c>.</summary>
    /// <param name="ConversationId">Teams conversation id of the channel.</param>
    public sealed record RecapChannelBody(string ConversationId);

    /// <summary>Request body for <c>PUT /api/teams/{id}/settings</c>.</summary>
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
