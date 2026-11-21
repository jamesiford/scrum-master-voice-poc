using Mcm.Scrum.Api.Voice;
using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Features.Standups.GetSession;
using Mcm.Scrum.Application.Features.Standups.GetSessionEvents;
using Mcm.Scrum.Application.Features.Standups.JoinMeeting;
using Mcm.Scrum.Application.Features.Standups.ListSessions;
using Mcm.Scrum.Application.Features.Standups.PrepareStandup;
using Mcm.Scrum.Application.Features.Standups.StartStandup;
using Microsoft.Extensions.Configuration;

namespace Mcm.Scrum.Api.Endpoints;

/// <summary>Standup session endpoints (SPEC 14). Each route sends exactly one command or query.</summary>
public static class SessionsEndpoints
{
    /// <summary>Maps <c>/api/sessions</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapSessionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions");

        group.MapPost("/prepare", async (PrepareBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new PrepareStandupCommand(body.TeamId), ct)).ToHttpResult());

        group.MapGet("/", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListSessionsQuery(teamId), ct)).ToHttpResult());

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetSessionQuery(id), ct)).ToHttpResult());

        group.MapGet("/{id:guid}/events", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetSessionEventsQuery(id), ct)).ToHttpResult());

        group.MapPost("/{id:guid}/start", async (Guid id, StartBody body, HttpContext http, ISender sender, BridgeTokenService tokens, IConfiguration config, CancellationToken ct) =>
        {
            var started = await sender.Send(new StartStandupCommand(id, body.Mode, body.JoinUrl, body.StandupMinutes), ct);
            if (!started.IsSuccess)
            {
                return started.ToHttpResult();
            }

            var token = tokens.Issue(id);
            if (string.Equals(body.Mode, "acs", StringComparison.OrdinalIgnoreCase))
            {
                var publicApi = (config["Bridge:ApiPublicUrl"] ?? $"{http.Request.Scheme}://{http.Request.Host}").TrimEnd('/');
                var joined = await sender.Send(new JoinMeetingCommand(id, token, publicApi), ct);
                return joined.IsSuccess ? Results.Ok(new StartResponse(token, string.Empty)) : joined.ToHttpResult();
            }

            var api = $"{http.Request.Scheme}://{http.Request.Host}";
            var roomBase = (config["Bridge:RoomBaseUrl"] ?? "http://localhost:5090").TrimEnd('/');
            return Results.Ok(new StartResponse(token, $"{roomBase}/room/{id}?token={Uri.EscapeDataString(token)}&api={Uri.EscapeDataString(api)}"));
        });

        return app;
    }

    /// <summary>Request body for <c>POST /api/sessions/prepare</c>.</summary>
    /// <param name="TeamId">Team to prepare a standup for.</param>
    public sealed record PrepareBody(string TeamId);

    /// <summary>Request body for <c>POST /api/sessions/{id}/start</c>.</summary>
    /// <param name="Mode">simulated or acs (joins the Teams meeting in JoinUrl).</param>
    /// <param name="JoinUrl">Teams join URL (acs).</param>
    /// <param name="StandupMinutes">Timebox override.</param>
    public sealed record StartBody(string Mode, string? JoinUrl = null, int? StandupMinutes = null);

    /// <summary>Response of the start endpoint.</summary>
    /// <param name="BridgeToken">Short-lived websocket token.</param>
    /// <param name="RoomUrl">Where to open the Simulated Room.</param>
    public sealed record StartResponse(string BridgeToken, string RoomUrl);
}
