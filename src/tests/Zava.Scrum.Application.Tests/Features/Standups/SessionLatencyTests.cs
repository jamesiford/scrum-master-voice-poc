using Zava.Scrum.Application.Features.Standups.GetSessionLatency;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Tests.Features.Standups;

/// <summary>Measured-hop reads through the real mediator, with persistence faked only at its port.</summary>
public sealed class SessionLatencyTests
{
    [Fact]
    public async Task GetSessionLatency_MeasuredTurns_ExcludesTalkOversAndUncalledModel()
    {
        await using var harness = await StandupHarness.CreateAsync();
        var session = await harness.LoadAsync();
        session.RecordEvent(StandupEventType.Timing, null, [], """{"totalMs":900,"speechEndToTranscriptMs":100,"decisionModelMs":400,"source":"Model"}""", StandupHarness.Now);
        session.RecordEvent(StandupEventType.Timing, null, [], """{"totalMs":1100,"decisionModelMs":0,"source":"Rules"}""", StandupHarness.Now);
        session.RecordEvent(StandupEventType.Timing, null, [], """{"reason":"TalkOver","totalMs":0}""", StandupHarness.Now);
        await harness.Sessions.SaveAsync(session, default);

        var result = await harness.Sender.Send(new GetSessionLatencyQuery(session.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(session.TeamId, result.Value.TeamId);
        Assert.Equal(new HopLatencyDto("voice.total", 900, 1100, 2), Assert.Single(result.Value.Hops, h => h.Hop == "voice.total"));
        Assert.Equal(new HopLatencyDto("decision.model", 400, 400, 1), Assert.Single(result.Value.Hops, h => h.Hop == "decision.model"));
        Assert.Equal(1, Assert.Single(result.Value.Hops, h => h.Hop == "voice.transcription").N);
        Assert.DoesNotContain(result.Value.Hops, h => h.Hop == "voice.first_audio");
    }

    [Fact]
    public async Task GetSessionLatency_NoEvents_DoesNotInventZeroSamples()
    {
        await using var harness = await StandupHarness.CreateAsync();

        var result = await harness.Sender.Send(new GetSessionLatencyQuery(harness.SessionId));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Hops);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("""{"totalMs":-1}""")]
    [InlineData("""{"totalMs":"not measured"}""")]
    [InlineData("[]")]
    [InlineData("{\"reason\":false}")]
    [InlineData("{\"hop\":42}")]
    [InlineData("{\"source\":{}}")]
    public async Task GetSessionLatency_InvalidMeasurement_ReturnsExplicitFailure(string text)
    {
        await using var harness = await StandupHarness.CreateAsync();
        var session = await harness.LoadAsync();
        session.RecordEvent(StandupEventType.Timing, null, [], text, StandupHarness.Now);
        await harness.Sessions.SaveAsync(session, default);

        var result = await harness.Sender.Send(new GetSessionLatencyQuery(session.Id));

        Assert.Equal("standup.invalid_timing", result.Error?.Code);
    }
}
