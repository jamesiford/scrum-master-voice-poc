using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Infrastructure.WorkSignals;

/// <summary>Real, metadata-only MCP discovery over owner OBO. It cannot invoke tools or retrieve workplace content.</summary>
/// <remarks>Unregistered pending billing and real scope fixtures. https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/mcp/overview</remarks>
public sealed class WorkIqDiscoveryClient(HttpClient http, IWorkSignalTokenSource tokens)
{
    private static readonly Uri Endpoint = new("https://workiq.svc.cloud.microsoft/mcp");

    /// <summary>Discovers the current tool schemas without caching team data or performing any content read.</summary>
    /// <param name="context">The Application boundary's checked team and named owner.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The live tool schema result or an explicit protocol/access refusal.</returns>
    public async Task<Result<JsonElement>> DiscoverAsync(WorkSignalContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.Team.WorkSignalsEnabled || context.Scope.TeamId != context.Team.Id ||
            context.CorrelationId == Guid.Empty)
        {
            return Result<JsonElement>.Failure(Error.Conflict("signals.unavailable", "No signals available."));
        }

        var access = await tokens.ExchangeAsync(context.OwnerAssertion, ct);
        if (!access.IsSuccess)
        {
            return Result<JsonElement>.Failure(access.Error!);
        }

        using var initial = await SendAsync(access.Value, null, new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new { protocolVersion = "2025-03-26", capabilities = new { }, clientInfo = new { name = "zava-read-only-discovery", version = "1.0" } },
        }, ct);
        if (!initial.IsSuccessStatusCode)
        {
            return Refused(initial);
        }

        var parsed = WorkIqDiscoveryResponse.Parse(await initial.Content.ReadAsStringAsync(ct), 1);
        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        if (!parsed.Value.TryGetProperty("protocolVersion", out var version) ||
            version.ValueKind != JsonValueKind.String || version.GetString() != "2025-03-26")
        {
            return Result<JsonElement>.Failure(Error.Conflict("signals.protocol_unsupported", "Work IQ negotiated an unsupported MCP version."));
        }

        var session = initial.Headers.TryGetValues("Mcp-Session-Id", out var values) ? values.Single() : null;
        using var notification = await SendAsync(access.Value, session, new { jsonrpc = "2.0", method = "notifications/initialized" }, ct);
        if (!notification.IsSuccessStatusCode)
        {
            return Refused(notification);
        }

        using var listed = await SendAsync(access.Value, session, new { jsonrpc = "2.0", id = 2, method = "tools/list" }, ct);
        if (!listed.IsSuccessStatusCode)
        {
            return Refused(listed);
        }

        var discovery = WorkIqDiscoveryResponse.Parse(await listed.Content.ReadAsStringAsync(ct), 2);
        if (discovery.IsSuccess && (!discovery.Value.TryGetProperty("tools", out var tools) || tools.ValueKind != JsonValueKind.Array))
        {
            return Result<JsonElement>.Failure(Error.Conflict("signals.discovery_invalid", "Work IQ returned no tool schema array."));
        }

        return discovery;
    }

    private async Task<HttpResponseMessage> SendAsync(DelegatedWorkSignalAccess access, string? session, object body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access.Token);
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", "2025-03-26");
        if (session is not null)
        {
            request.Headers.Add("Mcp-Session-Id", session);
        }

        request.Content = JsonContent.Create(body);
        return await http.SendAsync(request, ct);
    }

    private static Result<JsonElement> Refused(HttpResponseMessage response) =>
        Result<JsonElement>.Failure(Error.Conflict("signals.discovery_refused",
            $"No signals available. Work IQ discovery returned HTTP {(int)response.StatusCode}; check owner sign-in, billing and policy."));
}
