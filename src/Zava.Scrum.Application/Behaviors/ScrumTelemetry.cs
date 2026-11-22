using System.Diagnostics;

namespace Zava.Scrum.Application.Behaviors;

/// <summary>The OpenTelemetry source of the use-case pipeline. The Api subscribes to <see cref="SourceName"/>; with no listener it costs nothing.</summary>
public static class ScrumTelemetry
{
    /// <summary>The activity source name.</summary>
    public const string SourceName = "Zava.Scrum.Application";

    /// <summary>Gets the activity source.</summary>
    public static ActivitySource Source { get; } = new(SourceName);
}
