using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Zava.Scrum.Api.Voice;

/// <summary>The websocket to one bridge. Serialises sends because audio and JSON frames come from different tasks.</summary>
public sealed class BridgeConnection(WebSocket socket) : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Gets a value indicating whether the socket is open.</summary>
    public bool IsOpen => socket.State == WebSocketState.Open;

    /// <summary>Sends agent audio as a binary frame.</summary>
    /// <param name="pcm">PCM16, 24 kHz, mono.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    public Task SendAudioAsync(ReadOnlyMemory<byte> pcm, CancellationToken ct) => SendAsync(pcm, WebSocketMessageType.Binary, ct);

    /// <summary>Sends a protocol message as a JSON text frame.</summary>
    /// <param name="message">An object with a <c>type</c> property (SPEC 8).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    public Task SendAsync(object message, CancellationToken ct) =>
        SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json)), WebSocketMessageType.Text, ct);

    /// <summary>Receives one whole message.</summary>
    /// <param name="buffer">Scratch buffer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The type and bytes, or <see langword="null"/> when the peer closed.</returns>
    public async Task<(WebSocketMessageType Type, byte[] Data)?> ReceiveAsync(byte[] buffer, CancellationToken ct)
    {
        using var message = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return (result.MessageType, message.ToArray());
    }

    /// <summary>Closes the socket politely.</summary>
    /// <returns>A task.</returns>
    public async Task CloseAsync()
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
            }
            catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
            {
                // The bridge is already gone.
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task SendAsync(ReadOnlyMemory<byte> data, WebSocketMessageType type, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(data, type, endOfMessage: true, ct);
            }
        }
        catch (WebSocketException)
        {
            // A bridge that vanished mid-send is handled by the receive loop ending.
        }
        finally
        {
            _gate.Release();
        }
    }
}
