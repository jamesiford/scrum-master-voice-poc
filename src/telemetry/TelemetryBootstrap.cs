using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Zava.Scrum.Telemetry;

/// <summary>Shared host setup: fixed-percentage session sampling and metadata-only Azure Monitor export.</summary>
public static class TelemetryBootstrap
{
    /// <summary>Registers the existing exporter for an explicitly named host, without exporting application log bodies.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="service">Fixed service name.</param>
    public static void Configure(WebApplicationBuilder builder, string service)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder.Configuration.GetValue<bool>("Telemetry:CaptureContent"))
        {
            throw new InvalidOperationException("Telemetry content capture is prohibited. Export metadata only.");
        }

        AppContext.SetSwitch("Azure.Experimental.TraceGenAIMessageContent", false);
        if (string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"])) { return; }
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>((_, _) => false);
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(service))
            .WithTracing(tracing => tracing.AddSource(SessionTelemetry.SourceName, "Zava.Scrum.Agents")
                .AddProcessor(new MetadataOnlyProcessor()))
            .WithMetrics(metrics => metrics.AddMeter(SessionTelemetry.SourceName, "Zava.Scrum.Agents"))
            .UseAzureMonitor(options =>
            {
                // One decision for the session trace, not independently rate-limited hops.
                options.TracesPerSecond = null;
                options.SamplingRatio = 1;
                options.EnableLiveMetrics = false;
            });
    }

    /// <summary>Removes non-metadata tags and suppresses spans whose immutable events cannot be safely redacted.</summary>
    private sealed class MetadataOnlyProcessor : BaseProcessor<Activity>
    {
        public override void OnStart(Activity activity)
        {
            if (activity.GetTagItem(SessionTelemetry.TeamTag) is not null) { return; }
            for (var parent = activity.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent.GetTagItem(SessionTelemetry.TeamTag) is not string team) { continue; }
                activity.SetTag(SessionTelemetry.TeamTag, team);
                activity.SetTag("session.id", parent.GetTagItem("session.id"));
                if (activity.Source.Name == "Zava.Scrum.Agents") { activity.SetTag("hop", "model.sdk"); }
                break;
            }
        }

        public override void OnEnd(Activity activity)
        {
            // Activity events cannot be removed. Never export SDK exception/message events containing arbitrary text.
            if (activity.Events.Any())
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                if (activity.GetTagItem(SessionTelemetry.TeamTag) is string team)
                {
                    SessionTelemetry.Count("privacy", team, "span_with_events_suppressed");
                }
            }

            foreach (var tag in activity.TagObjects.ToArray())
            {
                if (!IsMetadata(tag.Key)) { activity.SetTag(tag.Key, null); }
            }

            activity.SetStatus(activity.Status);
        }

        private static bool IsMetadata(string key) => key is
            "team.id" or "session.id" or "hop" or "event" or "outcome" or
            "analysis.source" or "analysis.confidence" or "analysis.rejected_ids" or
            "approval.outcome" or "write.outcome" or "join.outcome" or "adapter" or
            "http.request.method" or "http.response.status_code" or "http.method" or "http.status_code" or
            "http.route" or "server.address" or "server.port" or "network.protocol.version" or
            "gen_ai.operation.name" or "gen_ai.system" or "gen_ai.provider.name" or
            "gen_ai.request.model" or "gen_ai.response.model" or
            "gen_ai.usage.input_tokens" or "gen_ai.usage.output_tokens";
    }
}
