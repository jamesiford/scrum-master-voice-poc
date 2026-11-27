using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Zava.Scrum.Telemetry;

/// <summary>The OpenTelemetry source and meter of the use-case pipeline. The Api subscribes to <see cref="SourceName"/>; with no listener it costs nothing.</summary>
public static class SessionTelemetry
{
    /// <summary>The activity source and meter name.</summary>
    public const string SourceName = "Zava.Scrum.Application";

    /// <summary>The tag every span, metric and log scope carries for the team a request is about (ADR 0018). Reports and the gate harness filter on it.</summary>
    public const string TeamTag = "team.id";

    private static readonly Meter ScrumMeter = new(SourceName);

    /// <summary>Gets the activity source.</summary>
    public static ActivitySource Source { get; } = new(SourceName);

    /// <summary>Gets the duration of each use case in milliseconds, tagged with the use case and the team.</summary>
    public static Histogram<double> UseCaseDuration { get; } = ScrumMeter.CreateHistogram<double>("zava.scrum.usecase.duration", "ms", "Use case duration");

    /// <summary>Gets hop durations, without meeting content or participant identity.</summary>
    public static Histogram<double> HopDuration { get; } = ScrumMeter.CreateHistogram<double>("zava.scrum.hop.duration", "ms");

    /// <summary>Gets counts of joins, turns, interruptions, approval decisions and refusals.</summary>
    public static Counter<long> Events { get; } = ScrumMeter.CreateCounter<long>("zava.scrum.events", "{event}");

    /// <summary>Starts a hop in the persisted session trace, linking a different incoming request rather than losing its provenance.</summary>
    /// <param name="hop">Fixed operation name, never user input.</param>
    /// <param name="teamId">The verified owning team.</param>
    /// <param name="sessionId">The session, when known.</param>
    /// <param name="correlationId">The persisted W3C trace id.</param>
    /// <param name="startTime">An already measured interval start, or the current instant.</param>
    /// <returns>The activity, or null when no exporter listens.</returns>
    public static Activity? StartHop(string hop, string teamId, Guid? sessionId, string? correlationId, DateTimeOffset startTime = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            Count("trace", teamId, "missing_correlation");
            return null;
        }

        var traceId = ActivityTraceId.CreateFromString(correlationId.AsSpan());
        var incoming = Activity.Current;
        var parent = incoming?.TraceId == traceId ? incoming.Context : new ActivityContext(traceId, default, ActivityTraceFlags.Recorded);
        var links = incoming is not null && incoming.TraceId != traceId ? new[] { new ActivityLink(incoming.Context) } : null;
        return Source.StartActivity(hop, ActivityKind.Internal, parent,
            new ActivityTagsCollection { [TeamTag] = teamId, ["session.id"] = sessionId?.ToString(), ["hop"] = hop },
            links: links, startTime: startTime);
    }

    /// <summary>Records one content-free event for a verified team.</summary>
    /// <param name="name">Fixed event name.</param>
    /// <param name="teamId">Owning team.</param>
    /// <param name="outcome">Fixed result classification.</param>
    public static void Count(string name, string teamId, string outcome) =>
        Events.Add(1, new KeyValuePair<string, object?>("event", name),
            new KeyValuePair<string, object?>(TeamTag, teamId), new KeyValuePair<string, object?>("outcome", outcome));

    /// <summary>Records the measured duration of a fixed hop for a verified team.</summary>
    /// <param name="hop">Fixed hop name.</param>
    /// <param name="teamId">Owning team.</param>
    /// <param name="milliseconds">Measured duration.</param>
    public static void RecordHop(string hop, string teamId, double milliseconds) =>
        HopDuration.Record(milliseconds, new KeyValuePair<string, object?>("hop", hop),
            new KeyValuePair<string, object?>(TeamTag, teamId));
}
