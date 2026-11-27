using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Standups.GetSessionLatency;

/// <summary>Summarises recorded intervals, excluding talk-over markers and unmeasured model calls.</summary>
internal sealed class GetSessionLatencyHandler(ISessionRepository sessions, ILogger<GetSessionLatencyHandler> logger)
    : IRequestHandler<GetSessionLatencyQuery, Result<SessionLatencyDto>>
{
    /// <inheritdoc />
    public async Task<Result<SessionLatencyDto>> Handle(GetSessionLatencyQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<SessionLatencyDto>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var samples = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var timing in session.Events.Where(e => e.Type == StandupEventType.Timing))
        {
            try
            {
                using var document = JsonDocument.Parse(timing.Text);
                Read(document.RootElement, samples);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Invalid timing event {EventId} in team {TeamId} session {SessionId}", timing.Id, session.TeamId, session.Id);
                return Result<SessionLatencyDto>.Failure(new Error("standup.invalid_timing", "A recorded timing event is invalid.", ErrorType.Unexpected));
            }
        }

        var hops = samples.OrderBy(s => s.Key, StringComparer.Ordinal)
            .Select(s => new HopLatencyDto(s.Key, LatencyStats.Percentile(s.Value, 50), LatencyStats.Percentile(s.Value, 95), s.Value.Count)).ToList();
        return Result<SessionLatencyDto>.Success(new SessionLatencyDto(session.TeamId, session.Id, hops));
    }

    private static void Read(JsonElement timing, Dictionary<string, List<int>> samples)
    {
        if (timing.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A timing event must be an object.");
        }

        if (ReadString(timing, "reason") == "TalkOver")
        {
            return;
        }

        if (ReadString(timing, "hop") is { } hop)
        {
            if (hop is "bridge.audio" or "bridge.playback")
            {
                Add(samples, hop, timing, "durationMs");
            }

            return;
        }

        Add(samples, "voice.total", timing, "totalMs");
        Add(samples, "voice.transcription", timing, "speechEndToTranscriptMs");
        Add(samples, "decision.grounding", timing, "analyserMs");
        Add(samples, "voice.orchestration", timing, "orchestrationMs");
        Add(samples, "voice.first_audio", timing, "voiceFirstAudioMs");
        if (string.Equals(ReadString(timing, "source"), "model", StringComparison.OrdinalIgnoreCase))
        {
            Add(samples, "decision.model", timing, "decisionModelMs");
        }
    }

    private static string? ReadString(JsonElement timing, string field)
    {
        if (!timing.TryGetProperty(field, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Invalid timing label {field}.");
        }

        return value.GetString();
    }

    private static void Add(Dictionary<string, List<int>> samples, string hop, JsonElement timing, string field)
    {
        if (!timing.TryGetProperty(field, out var value))
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var milliseconds) || milliseconds < 0)
        {
            throw new JsonException($"Invalid measured interval {field}.");
        }

        if (!samples.TryGetValue(hop, out var values))
        {
            samples[hop] = values = [];
        }

        values.Add(milliseconds);
    }
}
