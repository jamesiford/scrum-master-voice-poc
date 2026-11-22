using Azure.Monitor.OpenTelemetry.AspNetCore;
using Mcm.Scrum.Application.Behaviors;
using Mcm.Scrum.Infrastructure.Agents;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Mcm.Scrum.Api.Telemetry;

/// <summary>Basic OpenTelemetry to Application Insights: requests, outgoing HTTP, use-case spans and Agent Framework model calls.</summary>
public static class TelemetryExtensions
{
    /// <summary>The environment variable Bicep sets for the Application Insights resource.</summary>
    public const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Adds the Azure Monitor exporter when a connection string is configured; otherwise does nothing, so local runs need no setup.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same builder.</returns>
    /// <remarks>
    /// VERIFY: https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore.
    /// PROD: sampling, custom metrics per ceremony and distributed traces from the meeting bridge.
    /// </remarks>
    public static WebApplicationBuilder AddScrumTelemetry(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (string.IsNullOrWhiteSpace(builder.Configuration[ConnectionStringKey]))
        {
            return builder;
        }

        builder.Services.AddOpenTelemetry()
            .UseAzureMonitor()
            .WithTracing(t => t.AddSource(ScrumTelemetry.SourceName, FoundryAgentFactory.TelemetrySource))
            .WithMetrics(m => m.AddMeter(FoundryAgentFactory.TelemetrySource));
        return builder;
    }
}
