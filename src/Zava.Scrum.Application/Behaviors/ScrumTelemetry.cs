using System.Diagnostics;
using System.Diagnostics.Metrics;
using Zava.Scrum.Telemetry;

namespace Zava.Scrum.Application.Behaviors;

/// <summary>The use-case pipeline's shared content-free session telemetry.</summary>
public static class ScrumTelemetry
{
    /// <summary>The activity source and meter name.</summary>
    public const string SourceName = SessionTelemetry.SourceName;

    /// <summary>The verified owning team tag.</summary>
    public const string TeamTag = SessionTelemetry.TeamTag;

    /// <summary>Gets the shared activity source.</summary>
    public static ActivitySource Source => SessionTelemetry.Source;

    /// <summary>Gets measured use-case durations.</summary>
    public static Histogram<double> UseCaseDuration => SessionTelemetry.UseCaseDuration;

    /// <summary>Starts a hop in the persisted session trace.</summary>
    /// <param name="hop">Fixed operation name.</param>
    /// <param name="teamId">Verified team.</param>
    /// <param name="sessionId">Session, when known.</param>
    /// <param name="correlationId">Persisted trace id; absent legacy ids are counted, not invented.</param>
    /// <param name="startTime">An already measured interval start, or the current instant.</param>
    /// <returns>The activity, or null when no listener or correlation exists.</returns>
    public static Activity? StartHop(string hop, string teamId, Guid? sessionId, string? correlationId, DateTimeOffset startTime = default) =>
        SessionTelemetry.StartHop(hop, teamId, sessionId, correlationId, startTime);

    /// <summary>Counts an event without meeting content.</summary>
    /// <param name="name">Fixed event name.</param>
    /// <param name="teamId">Verified team.</param>
    /// <param name="outcome">Fixed result classification.</param>
    public static void Count(string name, string teamId, string outcome) => SessionTelemetry.Count(name, teamId, outcome);

    /// <summary>Records a measured hop duration.</summary>
    /// <param name="hop">Fixed operation name.</param>
    /// <param name="teamId">Verified team.</param>
    /// <param name="milliseconds">Measured duration.</param>
    public static void RecordHop(string hop, string teamId, double milliseconds) => SessionTelemetry.RecordHop(hop, teamId, milliseconds);
}
