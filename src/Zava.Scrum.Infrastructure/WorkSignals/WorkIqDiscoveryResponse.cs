using System.Text.Json;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Infrastructure.WorkSignals;

/// <summary>Parses JSON or SSE MCP metadata replies without accepting notifications, mismatched ids or protocol errors as success.</summary>
public static class WorkIqDiscoveryResponse
{
    /// <summary>Reads a matching JSON-RPC discovery result, including multiline SSE data.</summary>
    /// <param name="body">The metadata-only HTTP response.</param>
    /// <param name="expectedId">The request id.</param>
    /// <returns>A detached result or a typed protocol failure.</returns>
    public static Result<JsonElement> Parse(string body, int expectedId)
    {
        try
        {
            var payloads = body.TrimStart().StartsWith('{')
                ? new[] { body }
                : body.Replace("\r\n", "\n", StringComparison.Ordinal).Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                    .Select(frame => string.Join('\n', frame.Split('\n').Where(line => line.StartsWith("data:", StringComparison.Ordinal))
                        .Select(line => line[5..].TrimStart(' '))));
            foreach (var payload in payloads.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("id", out var id) ||
                    id.ValueKind != JsonValueKind.Number || !id.TryGetInt32(out var actualId) || actualId != expectedId)
                {
                    continue;
                }

                if (root.TryGetProperty("jsonrpc", out var version) && version.ValueKind == JsonValueKind.String && version.GetString() == "2.0" &&
                    !root.TryGetProperty("error", out _) && root.TryGetProperty("result", out var result) &&
                    result.ValueKind == JsonValueKind.Object)
                {
                    return Result<JsonElement>.Success(result.Clone());
                }

                return Invalid();
            }
        }
        catch (JsonException)
        {
            return Invalid();
        }

        return Invalid();
    }

    private static Result<JsonElement> Invalid() => Result<JsonElement>.Failure(
        Error.Conflict("signals.discovery_invalid", "No signals available. Work IQ returned an invalid discovery response."));
}
