using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Controls the Windows Graph media host without changing the meeting bridge port (ADR 0023).</summary>
public sealed class GraphMediaBridge(
    HttpClient http,
    ISessionRepository sessions,
    IOptions<BridgeOptions> options,
    ILogger<GraphMediaBridge> logger) : IMeetingBridge
{
    /// <inheritdoc />
    public async Task<Result> JoinAsync(MeetingJoinRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null || !string.Equals(session.JoinUrl, request.JoinUrl, StringComparison.Ordinal))
        {
            return Result.Failure(Error.Invalid("bridge.session_mismatch", "The join must use the session's stored meeting link."));
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "teams/join")
            {
                Content = JsonContent.Create(new
                {
                    request.SessionId,
                    session.TeamId,
                    request.JoinUrl,
                    request.BridgeToken,
                    request.ApiBase,
                }),
            };
            message.Headers.Add("x-bridge-key", options.Value.ApiKey);
            using var response = await http.SendAsync(message, ct);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            logger.LogWarning("Graph media join refused for {TeamId}/{SessionId}: {Status}", session.TeamId, session.Id, response.StatusCode);
            return Result.Failure(Error.ExternalRejected("bridge.graph_join_failed", $"Teams-native join failed ({(int)response.StatusCode}). Check the media host's call status."));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Graph media host unreachable for {TeamId}/{SessionId}", session.TeamId, session.Id);
            return Result.Failure(Error.ExternalRejected("bridge.graph_unreachable", "The Teams-native media host is not reachable."));
        }
    }

    /// <inheritdoc />
    public async Task LeaveAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await sessions.GetAsync(sessionId, ct)
            ?? throw new InvalidOperationException("Cannot leave a Graph media call without its owning session.");
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"teams/{sessionId}?teamId={Uri.EscapeDataString(session.TeamId)}");
        message.Headers.Add("x-bridge-key", options.Value.ApiKey);
        using var response = await http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
    }
}
