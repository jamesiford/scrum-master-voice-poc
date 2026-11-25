using System.Diagnostics;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Voice;

namespace Zava.Scrum.Infrastructure.Hosting;

/// <summary>
/// Starts the hosted agent sandbox for a standup by calling its HTTP <c>invocations</c> route once with the agent session id the standup will use
/// (ADR 0015 S3 measured the cold start at 3.4 to 4.1 s; the next call on the same session is warm). It carries the team id and the host's
/// own Entra token. A failure is reported, never thrown: the standup then pays the cold start instead.
/// </summary>
public sealed class HostedAgentWarmer(HttpClient http, TokenCredential credential, IOptions<VoiceLiveOptions> options, ILogger<HostedAgentWarmer> logger) : IAgentHostWarmer
{
    private const string Scope = "https://ai.azure.com/.default";

    /// <inheritdoc />
    public async Task<HostWarmResult> WarmAsync(string teamId, Guid sessionId, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew();
        var endpoint = VoiceRoute.EndpointFor(options.Value, teamId, sessionId);
        var route = endpoint == options.Value.CanaryEndpoint ? "hosted-canary" : "hosted";
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            var token = await credential.GetTokenAsync(new TokenRequestContext([Scope]), timeout.Token);
            using var request = new HttpRequestMessage(HttpMethod.Post, InvocationsUrl(endpoint, teamId, sessionId))
            {
                Content = new StringContent("{\"warm\":true}", System.Text.Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
            using var response = await http.SendAsync(request, timeout.Token);
            var ms = (int)clock.ElapsedMilliseconds;
            logger.LogInformation("Warmed the agent host for team {TeamId} session {SessionId}: {Status} in {Ms} ms", teamId, sessionId, (int)response.StatusCode, ms);
            return response.IsSuccessStatusCode
                ? new HostWarmResult(route, true, ms)
                : new HostWarmResult(route, false, ms, $"The host answered {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or Azure.Identity.AuthenticationFailedException or Azure.Identity.CredentialUnavailableException)
        {
            logger.LogWarning(ex, "Warming the agent host failed for team {TeamId}; the standup will pay the cold start", teamId);
            return new HostWarmResult(route, false, (int)clock.ElapsedMilliseconds, "The host did not answer in time.");
        }
    }

    // The voice endpoint is .../protocols/invocations_ws; the same agent serves the HTTP route next to it.
    private static Uri InvocationsUrl(string relayEndpoint, string teamId, Guid sessionId)
    {
        var url = relayEndpoint.Replace("wss://", "https://", StringComparison.Ordinal).Replace("/invocations_ws", "/invocations", StringComparison.Ordinal);
        return new Uri($"{url}?api-version=v1&agent_session_id={sessionId}&team={Uri.EscapeDataString(teamId)}");
    }
}

/// <summary>The direct voice route has no host to start, so warming says so and does nothing (a real adapter of the same port).</summary>
public sealed class NoHostWarmer : IAgentHostWarmer
{
    /// <inheritdoc />
    public Task<HostWarmResult> WarmAsync(string teamId, Guid sessionId, CancellationToken ct) =>
        Task.FromResult(new HostWarmResult("direct", false, 0, "The voice route is direct; there is no host to warm."));
}
