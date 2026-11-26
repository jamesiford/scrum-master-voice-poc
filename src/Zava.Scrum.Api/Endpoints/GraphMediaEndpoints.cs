using Microsoft.AspNetCore.Authorization;
using System.Net.WebSockets;
using Zava.Scrum.Api.Auth;
using Zava.Scrum.Api.Voice;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Standups.EndStandup;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Team/session capability routes for the trusted Windows media host, separate from the ACS gateway.</summary>
public static class GraphMediaEndpoints
{
    /// <summary>Maps the non-recording media context and identity-tagged WebSocket endpoints.</summary>
    /// <param name="app">Route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapGraphMediaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/media/context", async (Guid sessionId, string teamId,
            MediaSessionLoader loader, CancellationToken ct) =>
        {
            var context = await loader.LoadAsync(sessionId, teamId, ct);
            return context.IsSuccess
                ? Results.Ok(new { sessionId, teamId, context.Value.JoinUrl, context.Value.BoardMode })
                : context.ToHttpResult();
        }).RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = MediaHostAuthenticationHandler.SchemeName }).NoTeamScope();
        app.Map("/invocations_ws", async (Guid sessionId, string teamId,
            HttpContext http, MediaSessionLoader loader,
            GraphMediaVoiceGateway gateway, ISender sender) =>
        {
            if (!http.WebSockets.IsWebSocketRequest) { return Results.BadRequest(); }
            var context = await loader.LoadAsync(sessionId, teamId, http.RequestAborted);
            if (!context.IsSuccess) { return context.ToHttpResult(); }
            using var socket = await http.WebSockets.AcceptWebSocketAsync();
            using var bridge = new BridgeConnection(socket);
            if (await gateway.RunAsync(bridge, context.Value, http.RequestAborted))
            {
                var ended = await sender.Send(new EndStandupCommand(sessionId), CancellationToken.None);
                if (!ended.IsSuccess)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError,
                        $"Session end failed ({ended.Error!.Code}).", http.RequestAborted);
                }
            }

            return Results.Empty;
        }).RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = MediaHostAuthenticationHandler.SchemeName }).NoTeamScope();
        return app;
    }
}
