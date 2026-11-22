using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Azure.Core;
using Zava.Scrum.Application.Abstractions.Ports;
using Microsoft.Extensions.Options;
using Zava.Scrum.Infrastructure.Agents;

namespace Zava.Scrum.Infrastructure.Voice;

/// <summary>Opens Azure Voice Live sessions with an Entra token (no keys) and sends the session configuration.</summary>
/// <remarks>VERIFY: https://learn.microsoft.com/azure/ai-services/speech-service/voice-live-how-to (scope https://ai.azure.com/.default).</remarks>
public sealed class VoiceLiveSessionFactory(TokenCredential credential, IOptions<VoiceLiveOptions> voice, IOptions<FoundryOptions> foundry) : IVoiceSessionFactory
{
    private const string Scope = "https://ai.azure.com/.default";

    /// <inheritdoc />
    public async Task<IVoiceSession> CreateAsync(VoiceSessionConfig config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(config);
        var options = voice.Value;
        var token = await credential.GetTokenAsync(new TokenRequestContext([Scope]), ct);
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", "Bearer " + token.Token);
        try
        {
            await socket.ConnectAsync(BuildUri(options), ct);
            var session = new VoiceLiveSession(socket, config.Instructions);
            await session.SendRawAsync(SessionUpdate(config, options), ct);
            return session;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private Uri BuildUri(VoiceLiveOptions options)
    {
        var host = string.IsNullOrWhiteSpace(options.Endpoint)
            ? "wss://" + new Uri(foundry.Value.ProjectEndpoint).Host + "/voice-live/realtime"
            : options.Endpoint;
        // FoundryAgent mode: bind the session to a Foundry voice agent instead of a model deployment.
        // VERIFY: https://learn.microsoft.com/azure/ai-services/speech-service/voice-live-agents-quickstart (agent-name and agent-project-name query parameters)
        if (!string.IsNullOrWhiteSpace(options.AgentName))
        {
            var project = Uri.EscapeDataString(options.AgentProjectName);
            return new Uri($"{host}?api-version={Uri.EscapeDataString(options.ApiVersion)}&agent-name={Uri.EscapeDataString(options.AgentName)}&agent-project-name={project}");
        }

        var model = Uri.EscapeDataString(options.Model);
        return new Uri($"{host}?api-version={Uri.EscapeDataString(options.ApiVersion)}&model={model}");
    }

    private static JsonObject SessionUpdate(VoiceSessionConfig config, VoiceLiveOptions options)
    {
        var tools = new JsonArray();
        foreach (var tool in config.Tools)
        {
            tools.Add(new JsonObject
            {
                ["type"] = "function",
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = JsonNode.Parse(tool.ParametersJsonSchema),
            });
        }

        return new JsonObject
        {
            ["type"] = "session.update",
            ["session"] = new JsonObject
            {
                ["modalities"] = new JsonArray("text", "audio"),
                ["instructions"] = config.Instructions,
                ["voice"] = new JsonObject { ["name"] = options.Voice, ["type"] = "azure-standard" },
                ["input_audio_format"] = "pcm16",
                ["output_audio_format"] = "pcm16",
                ["input_audio_sampling_rate"] = 24000,
                ["input_audio_noise_reduction"] = new JsonObject { ["type"] = "azure_deep_noise_suppression" },
                ["input_audio_echo_cancellation"] = new JsonObject { ["type"] = "server_echo_cancellation" },
                ["input_audio_transcription"] = new JsonObject { ["model"] = "azure-speech", ["language"] = options.Language },
                // create_response=false: the gateway's floor control, not the VAD, decides when the agent speaks (SPEC 11.1).
                ["turn_detection"] = new JsonObject
                {
                    ["type"] = "azure_semantic_vad",
                    ["silence_duration_ms"] = config.SilenceMs,
                    ["remove_filler_words"] = true,
                    ["create_response"] = false,
                    ["interrupt_response"] = false,
                },
                ["tools"] = tools,
                ["tool_choice"] = "auto",
            },
        };
    }
}
