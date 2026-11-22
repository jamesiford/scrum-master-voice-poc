using Zava.Scrum.Api.Voice;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>The bridge websocket (SPEC 8). A thin adapter: it authenticates the token and hands the socket to the voice gateway.</summary>
public static class BridgeEndpoints
{
    /// <summary>Maps <c>/ws/bridge/{sessionId}</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapBridgeEndpoints(this IEndpointRouteBuilder app)
    {
        app.Map("/ws/bridge/{sessionId:guid}", async (Guid sessionId, string? token, HttpContext http, BridgeTokenService tokens, IServiceProvider services) =>
        {
            if (!http.WebSockets.IsWebSocketRequest)
            {
                return Results.BadRequest(new { error = "websocket_required", detail = "Connect with a websocket." });
            }

            if (!tokens.IsValid(token, sessionId))
            {
                return Results.Json(new { error = "invalid_token", detail = "The bridge token is missing, expired or for another session." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            using var socket = await http.WebSockets.AcceptWebSocketAsync();
            using var bridge = new BridgeConnection(socket);
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<VoiceGateway>().RunAsync(bridge, sessionId, http.RequestAborted);
            return Results.Empty;
        });
        return app;
    }
}
