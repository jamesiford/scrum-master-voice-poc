using Zava.Scrum.Application.Abstractions.Messaging;
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

        group.MapGet("/{id}", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTeamQuery(id), ct)).ToHttpResult());

        group.MapPut("/{id}", async (string id, UpdateTeamSettingsBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(body.ToCommand(id), ct)).ToHttpResult());

        group.MapGet("/{id}/members", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetRosterQuery(id), ct)).ToHttpResult());

        group.MapPost("/{id}/members/sync", async (string id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SyncRosterCommand(id), ct)).ToHttpResult());

        group.MapPut("/{id}/members/{memberId}/availability", async (string id, string memberId, AvailabilityBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateMemberAvailabilityCommand(id, memberId, body.Percent), ct)).ToHttpResult());

        group.MapPut("/{id}/recap-channel", async (string id, RecapChannelBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetRecapChannelCommand(id, body.ConversationId), ct)).ToHttpResult());

        return app;
    }

    /// <summary>Request body for <c>PUT /api/teams/{id}/members/{memberId}/availability</c>.</summary>
    /// <param name="Percent">Availability, 0 to 100.</param>
    public sealed record AvailabilityBody(int Percent);

    /// <summary>Request body for <c>PUT /api/teams/{id}/recap-channel</c>.</summary>
    /// <param name="ConversationId">Teams conversation id of the channel.</param>
    public sealed record RecapChannelBody(string ConversationId);

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
