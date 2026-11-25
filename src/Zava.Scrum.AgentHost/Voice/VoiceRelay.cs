using System.Diagnostics;
using System.Net.WebSockets;

namespace Zava.Scrum.AgentHost.Voice;

/// <summary>
/// The relay itself: forwards every frame between a caller's connection and one upstream Voice Live connection, text and binary, unchanged and
/// in order, until either side closes. It holds no session state, so a stopped or killed host loses nothing; the caller reconnects with the
/// same agent session id and re-sends its configuration from Cosmos-backed state (ADR 0021).
/// </summary>
public static class VoiceRelay
{
    private const int BufferSize = 64 * 1024;

    /// <summary>The close status sent when the caller names no team: the host never infers one (prompt 3A, rule 10).</summary>
    public const WebSocketCloseStatus TeamRequired = WebSocketCloseStatus.PolicyViolation;

    /// <summary>Relays one connection.</summary>
    /// <param name="caller">The caller's connection (the platform's <c>invocations_ws</c> side).</param>
    /// <param name="teamId">The team the caller resolved before calling; refused when empty.</param>
    /// <param name="connectUpstream">Opens the upstream Voice Live connection with the host's own identity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>How long the relay ran, for the structured log line.</returns>
    public static async Task<TimeSpan> RunAsync(WebSocket caller, string? teamId, Func<CancellationToken, Task<WebSocket>> connectUpstream, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(connectUpstream);
        var clock = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(teamId))
        {
            await CloseQuietlyAsync(caller, TeamRequired, "A team id is required");
            return clock.Elapsed;
        }

        using var upstream = await connectUpstream(ct);
        using var done = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var up = PumpAsync(caller, upstream, done.Token);
        var down = PumpAsync(upstream, caller, done.Token);
        await Task.WhenAny(up, down);
        await done.CancelAsync();
        await CloseQuietlyAsync(upstream, WebSocketCloseStatus.NormalClosure, "relay done");
        await CloseQuietlyAsync(caller, WebSocketCloseStatus.NormalClosure, "relay done");
        return clock.Elapsed;
    }

    private static async Task PumpAsync(WebSocket from, WebSocket to, CancellationToken ct)
    {
        var buffer = new byte[BufferSize];
        try
        {
            while (from.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var received = await from.ReceiveAsync(buffer, ct);
                if (received.MessageType == WebSocketMessageType.Close || to.State != WebSocketState.Open)
                {
                    return;
                }

                await to.SendAsync(new ArraySegment<byte>(buffer, 0, received.Count), received.MessageType, received.EndOfMessage, ct);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            // Either side went away; the caller of the pump closes the other side.
        }
    }

    private static async Task CloseQuietlyAsync(WebSocket socket, WebSocketCloseStatus status, string reason)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await socket.CloseAsync(status, reason, cts.Token);
            }
            catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
            {
                // Already gone.
            }
        }
    }
}
