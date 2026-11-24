using Zava.Scrum.Api.Voice;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Standups.ControlStandup;
using Zava.Scrum.Application.Features.Standups.GetLiveState;
using Zava.Scrum.Application.Features.Standups.GetSession;
using Zava.Scrum.Application.Features.Standups.GetSessionEvents;
using Zava.Scrum.Application.Features.Standups.JoinMeeting;
using Zava.Scrum.Application.Features.Standups.ListSessions;
using Zava.Scrum.Application.Features.Standups.PrepareStandup;
using Zava.Scrum.Application.Features.Standups.StartStandup;
using Zava.Scrum.Application.Features.Voice.AnalyseUtterance;
using Microsoft.Extensions.Configuration;

namespace Zava.Scrum.Api.Endpoints;

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

        // Shows how the decision model reads one sentence (intent, items, comment kinds) without a live call, so the prompt can be tuned.
        group.MapPost("/{id:guid}/analyse", async (Guid id, AnalyseBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AnalyseUtteranceQuery(id, body.SpeakerId, body.HolderId, body.Text, null, body.Asked, body.FocusItemId), ct)).ToHttpResult());

        group.MapPost("/{id:guid}/control", async (Guid id, ControlBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ControlStandupCommand(id, body.Action), ct)).ToHttpResult());

        // Server-sent events for the Live console: one state frame per second until the client leaves or the standup ends.
        group.MapGet("/{id:guid}/live", async (Guid id, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-store";
            for (var attempts = 0; !ct.IsCancellationRequested; attempts++)
            {
                var state = await sender.Send(new GetLiveStateQuery(id), ct);
                var frame = state.IsSuccess ? state.Value : "{\"ended\":true}";
                await http.Response.WriteAsync($"data: {frame}\n\n", ct);
                await http.Response.Body.FlushAsync(ct);
                if (!state.IsSuccess && attempts > 2)
                {
                    break;
                }

                await Task.Delay(1000, ct);
            }
        });

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

    /// <summary>Request body for <c>POST /api/sessions/{id}/control</c>.</summary>
    /// <param name="Action">begin, mute, unmute, skip, takeover, release or end.</param>
    public sealed record ControlBody(string Action);

    /// <summary>Request body for <c>POST /api/sessions/{id}/analyse</c>.</summary>
    /// <param name="SpeakerId">Who said it.</param>
    /// <param name="Text">What was said.</param>
    /// <param name="HolderId">Whose turn it is, if anyone's.</param>
    /// <param name="Asked">The question the agent just asked, if any.</param>
    /// <param name="FocusItemId">The item that question was about.</param>
    public sealed record AnalyseBody(string SpeakerId, string Text, string? HolderId = null, string? Asked = null, int? FocusItemId = null);

    /// <summary>Response of the start endpoint.</summary>
    /// <param name="BridgeToken">Short-lived websocket token.</param>
    /// <param name="RoomUrl">Where to open the Simulated Room; empty for a Teams meeting.</param>
    public sealed record StartResponse(string BridgeToken, string RoomUrl);
}
