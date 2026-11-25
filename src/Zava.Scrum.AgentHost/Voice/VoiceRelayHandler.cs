using System.Net.WebSockets;
using Azure.AI.AgentServer.Invocations;
using Azure.Core;
using Azure.Identity;

namespace Zava.Scrum.AgentHost.Voice;

/// <summary>
/// The voice route of the hosted agent: <see cref="VoiceRelay"/> wired to Azure Voice Live with the agent's own identity (no keys). The caller
/// names the team (<c>?team=</c>) and the model (<c>?model=</c>); the platform forwards both unchanged.
/// </summary>
/// <remarks>
/// VERIFY: https://learn.microsoft.com/azure/foundry/agents/how-to/build-voice-agent (the platform consumes <c>api-version</c> and the path
/// segments and forwards other query parameters unchanged).
/// </remarks>
public sealed class VoiceRelayHandler : InvocationWebSocketHandler
{
    private const string Scope = "https://ai.azure.com/.default";
    private static readonly TokenCredential Credential = new DefaultAzureCredential();

    /// <inheritdoc />
    public override async Task HandleAsync(HttpRequest request, HttpResponse response, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        await response.WriteAsync("{\"route\":\"invocations_ws\",\"role\":\"voice relay\"}", cancellationToken);
    }

    /// <inheritdoc />
    public override async Task HandleWebSocketAsync(WebSocket webSocket, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.QueryParameters.TryGetValue("team", out var team);
        context.QueryParameters.TryGetValue("model", out var requested);
        var model = string.IsNullOrWhiteSpace(requested) ? Environment.GetEnvironmentVariable("VOICELIVE_MODEL") ?? "gpt-realtime-2.1" : requested.ToString();
        await VoiceRelay.RunAsync(webSocket, team.ToString(), ct => ConnectAsync(model, ct), cancellationToken);
    }

    private static async Task<WebSocket> ConnectAsync(string model, CancellationToken ct)
    {
        var endpoint = Environment.GetEnvironmentVariable("VOICELIVE_ENDPOINT") ?? throw new InvalidOperationException("VOICELIVE_ENDPOINT is not set.");
        var version = Environment.GetEnvironmentVariable("VOICELIVE_API_VERSION") ?? "2026-04-10";
        var token = await Credential.GetTokenAsync(new TokenRequestContext([Scope]), ct);
        var upstream = new ClientWebSocket();
        upstream.Options.SetRequestHeader("Authorization", "Bearer " + token.Token);
        try
        {
            await upstream.ConnectAsync(new Uri($"{endpoint}?api-version={Uri.EscapeDataString(version)}&model={Uri.EscapeDataString(model)}"), ct);
            return upstream;
        }
        catch
        {
            upstream.Dispose();
            throw;
        }
    }
}
