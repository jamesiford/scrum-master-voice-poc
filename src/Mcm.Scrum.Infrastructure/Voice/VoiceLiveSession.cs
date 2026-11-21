using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Infrastructure.Voice;

/// <summary>One Voice Live websocket conversation. Translates between the realtime event protocol and <see cref="VoiceEvent"/>.</summary>
/// <remarks>VERIFY: https://learn.microsoft.com/azure/ai-services/speech-service/voice-live-how-to (event names follow the Azure OpenAI Realtime API).</remarks>
internal sealed class VoiceLiveSession(ClientWebSocket socket, string baseInstructions) : IVoiceSession
{
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly byte[] _buffer = new byte[64 * 1024];

    public async IAsyncEnumerable<VoiceEvent> ReadEventsAsync([EnumeratorCancellation] CancellationToken ct)
    {
        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            string? text;
            string? fault = null;
            try
            {
                text = await ReceiveTextAsync(ct);
            }
            catch (WebSocketException ex)
            {
                text = null;
                fault = $"Voice Live disconnected: {ex.Message}";
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (text is null)
            {
                yield return new VoiceFault(fault ?? "Voice Live closed the connection.");
                yield break;
            }

            var mapped = Map(text);
            if (mapped is not null)
            {
                yield return mapped;
            }
        }
    }

    public ValueTask SendAudioAsync(ReadOnlyMemory<byte> pcm, CancellationToken ct) =>
        new(SendAsync(new JsonObject { ["type"] = "input_audio_buffer.append", ["audio"] = Convert.ToBase64String(pcm.Span) }, ct));

    public Task CreateResponseAsync(string? instructions, CancellationToken ct)
    {
        var response = new JsonObject();
        if (instructions is not null)
        {
            // Per-response instructions replace the session ones, so the persona is repeated with every turn.
            response["instructions"] = baseInstructions + "\n\nCURRENT TURN INSTRUCTION:\n" + instructions;
        }

        return SendAsync(new JsonObject { ["type"] = "response.create", ["response"] = response }, ct);
    }

    public Task CancelResponseAsync(CancellationToken ct) => SendAsync(new JsonObject { ["type"] = "response.cancel" }, ct);

    public Task AddUserTextAsync(string text, CancellationToken ct) =>
        SendAsync(
            new JsonObject
            {
                ["type"] = "conversation.item.create",
                ["item"] = new JsonObject
                {
                    ["type"] = "message",
                    ["role"] = "user",
                    ["content"] = new JsonArray(new JsonObject { ["type"] = "input_text", ["text"] = text }),
                },
            },
            ct);

    public Task SubmitToolOutputAsync(string callId, string output, CancellationToken ct) =>
        SendAsync(
            new JsonObject
            {
                ["type"] = "conversation.item.create",
                ["item"] = new JsonObject { ["type"] = "function_call_output", ["call_id"] = callId, ["output"] = output },
            },
            ct);

    public Task SendRawAsync(JsonObject message, CancellationToken ct) => SendAsync(message, ct);

    public async ValueTask DisposeAsync()
    {
        if (socket.State == WebSocketState.Open)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
            }
            catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
            {
                // The peer is already gone; nothing left to close.
            }
        }

        socket.Dispose();
        _sendGate.Dispose();
    }

    private async Task SendAsync(JsonObject message, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(message.ToJsonString());
        await _sendGate.WaitAsync(ct);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
            }
        }
        finally
        {
            _sendGate.Release();
        }
    }

    private async Task<string?> ReceiveTextAsync(CancellationToken ct)
    {
        using var message = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(_buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(_buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
    }

    private static VoiceEvent? Map(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return Str(root, "type") switch
        {
            "response.audio.delta" or "response.output_audio.delta" => new AgentAudio(Convert.FromBase64String(Str(root, "delta") ?? string.Empty)),
            "response.audio_transcript.done" or "response.output_audio_transcript.done" => new AgentTranscript(Str(root, "transcript") ?? string.Empty),
            "response.function_call_arguments.done" => new ToolCallRequested(Str(root, "call_id") ?? string.Empty, Str(root, "name") ?? string.Empty, Str(root, "arguments") ?? "{}"),
            "response.done" => new ResponseDone(HasFunctionCall(root)),
            "input_audio_buffer.speech_started" => new UserSpeechStarted(),
            "input_audio_buffer.speech_stopped" => new UserSpeechStopped(),
            "conversation.item.input_audio_transcription.completed" => new UserTranscript(Str(root, "transcript") ?? string.Empty),
            "error" => new VoiceFault(root.TryGetProperty("error", out var e) ? Str(e, "message") ?? "Voice Live error" : "Voice Live error"),
            _ => null,
        };
    }

    private static bool HasFunctionCall(JsonElement root) =>
        root.TryGetProperty("response", out var r) && r.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array
        && output.EnumerateArray().Any(i => Str(i, "type") == "function_call");

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
