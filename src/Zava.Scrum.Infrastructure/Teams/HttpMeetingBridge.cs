using System.Net.Http.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Controls the Node meeting bridge over HTTP (SPEC 7.3). The shared <c>x-bridge-key</c> keeps the control routes private.</summary>
public sealed class HttpMeetingBridge(HttpClient http, IOptions<BridgeOptions> options, ILogger<HttpMeetingBridge> logger) : IMeetingBridge
{
    /// <inheritdoc />
    public async Task<Result> JoinAsync(MeetingJoinRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "teams/join") { Content = JsonContent.Create(request) };
            message.Headers.Add("x-bridge-key", options.Value.ApiKey);
            using var response = await http.SendAsync(message, ct);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var detail = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Bridge refused the join: {Status} {Detail}", (int)response.StatusCode, detail);
            return Result.Failure(Error.ExternalRejected("bridge.join_failed", $"The meeting bridge could not start the call ({(int)response.StatusCode}). {detail}"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Meeting bridge unreachable");
            return Result.Failure(Error.ExternalRejected("bridge.unreachable", "The meeting bridge is not reachable. Is it running?"));
        }
    }

    /// <inheritdoc />
    public async Task LeaveAsync(Guid sessionId, CancellationToken ct)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Delete, $"teams/{sessionId}");
            message.Headers.Add("x-bridge-key", options.Value.ApiKey);
            using var response = await http.SendAsync(message, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Nothing to hang up if the bridge is gone.
            logger.LogDebug(ex, "Leave skipped for {SessionId}", sessionId);
        }
    }
}
