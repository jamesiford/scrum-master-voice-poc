using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Zava.Scrum.Application.Behaviors;

/// <summary>The OpenTelemetry source and meter of the use-case pipeline. The Api subscribes to <see cref="SourceName"/>; with no listener it costs nothing.</summary>
public static class ScrumTelemetry
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
}
