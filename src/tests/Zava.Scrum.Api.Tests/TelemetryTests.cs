using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Zava.Scrum.Telemetry;

namespace Zava.Scrum.Api.Tests;

public sealed class TelemetryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("InstrumentationKey=22222222-2222-2222-2222-222222222222")]
    public void Configure_ExplicitExporterDestination_DoesNotRequireOrOverrideReservedPlatformVariable(string? platformConnection)
    {
        var builder = WebApplication.CreateBuilder();
        const string destination = "InstrumentationKey=11111111-1111-1111-1111-111111111111";
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] = platformConnection;
        builder.Configuration["AzureMonitor:ConnectionString"] = destination;

        TelemetryBootstrap.Configure(builder, "test");
        using var provider = builder.Services.BuildServiceProvider();

        Assert.Equal(destination, provider.GetRequiredService<IOptions<AzureMonitorOptions>>().Value.ConnectionString);
        Assert.Equal(platformConnection, builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
    }

    private sealed class RecordingExporter : BaseExporter<Activity>
    {
        public List<Activity> Activities { get; } = [];

        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch) { Activities.Add(activity); }
            return ExportResult.Success;
        }
    }

    private static BaseProcessor<Activity> Processor()
    {
        var type = typeof(TelemetryBootstrap).GetNestedType("MetadataOnlyProcessor", BindingFlags.NonPublic);
        Assert.NotNull(type);
        return Assert.IsAssignableFrom<BaseProcessor<Activity>>(Activator.CreateInstance(type, nonPublic: true));
    }

    [Fact]
    public void OnStart_ChildSdkHop_InheritsVerifiedTeamAndSession()
    {
        using var parent = new Activity("decision.model").Start();
        parent.SetTag("team.id", "t1");
        parent.SetTag("session.id", "s1");
        using var child = new Activity("model.sdk").Start();

        Processor().OnStart(child);

        Assert.Equal("t1", child.GetTagItem("team.id"));
        Assert.Equal("s1", child.GetTagItem("session.id"));
    }

    [Fact]
    public void OnStart_ExplicitOwningTeam_IsNeverReplacedByIncomingRequest()
    {
        using var parent = new Activity("request").Start();
        parent.SetTag("team.id", "t1");
        using var child = new Activity("approved.session").Start();
        child.SetTag("team.id", "t2");

        Processor().OnStart(child);

        Assert.Equal("t2", child.GetTagItem("team.id"));
    }

    [Fact]
    public void OnEnd_SensitiveTagsAndStatus_ExportsOnlyMetadata()
    {
        using var activity = new Activity("decision.model").Start();
        activity.SetTag("team.id", "t1");
        activity.SetTag("gen_ai.input.messages", "synthetic private utterance");
        activity.SetTag("url.full", "https://example.test/?token=synthetic");
        activity.SetStatus(ActivityStatusCode.Error, "synthetic private exception");

        Processor().OnEnd(activity);

        Assert.Equal("t1", activity.GetTagItem("team.id"));
        Assert.Null(activity.GetTagItem("gen_ai.input.messages"));
        Assert.Null(activity.GetTagItem("url.full"));
        Assert.Null(activity.StatusDescription);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void OnEnd_ImmutableMessageEvent_SuppressesExportRatherThanLeakingContent()
    {
        using var activity = new Activity("model.sdk").Start();
        activity.ActivityTraceFlags = ActivityTraceFlags.Recorded;
        activity.SetTag("team.id", "t1");
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection { ["exception.message"] = "synthetic private utterance" }));

        var exporter = new RecordingExporter();
        using var exportProcessor = new SimpleActivityExportProcessor(exporter);
        Processor().OnEnd(activity);
        exportProcessor.OnEnd(activity);

        Assert.False(activity.Recorded);
        Assert.Empty(exporter.Activities);
    }

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
