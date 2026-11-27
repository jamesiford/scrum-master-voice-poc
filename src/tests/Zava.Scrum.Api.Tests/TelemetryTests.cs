using Microsoft.AspNetCore.Builder;
using Zava.Scrum.Telemetry;

namespace Zava.Scrum.Api.Tests;

public sealed class TelemetryTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public void Configure_ContentRequested_RefusesBeforeExporterRegistration(string environment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration["Telemetry:CaptureContent"] = "true";

        var exception = Assert.Throws<InvalidOperationException>(() => TelemetryBootstrap.Configure(builder, "test"));

        Assert.Contains("prohibited", exception.Message, StringComparison.Ordinal);
    }
}
