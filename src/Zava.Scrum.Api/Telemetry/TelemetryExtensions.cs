using Zava.Scrum.Telemetry;

namespace Zava.Scrum.Api.Telemetry;

/// <summary>Content-free OpenTelemetry for the API, session hops and Agent Framework model calls.</summary>
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
    /// </remarks>
    public static WebApplicationBuilder AddScrumTelemetry(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        TelemetryBootstrap.Configure(builder, "zava-api");
        return builder;
    }
}
