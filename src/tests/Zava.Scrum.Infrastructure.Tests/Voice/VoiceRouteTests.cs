using Zava.Scrum.Infrastructure.Voice;

namespace Zava.Scrum.Infrastructure.Tests.Voice;

/// <summary>Which hosted agent a standup uses: stable, or the canary for a share of sessions in the allowed teams (ADR 0021).</summary>
public sealed class VoiceRouteTests
{
    private const string Stable = "wss://x/agents/zava-voice-relay/endpoint/protocols/invocations_ws";
    private const string Canary = "wss://x/agents/zava-voice-relay-canary/endpoint/protocols/invocations_ws";

    [Fact]
    public void EndpointFor_NoRelayEndpoint_IsTheDirectRoute() =>
        Assert.Equal(string.Empty, VoiceRoute.EndpointFor(new VoiceLiveOptions(), "t1", Guid.NewGuid()));

    [Fact]
    public void EndpointFor_CanaryAtZeroPercent_AlwaysUsesTheStableAgent()
    {
        var options = new VoiceLiveOptions { RelayEndpoint = Stable, CanaryEndpoint = Canary, CanaryPercent = 0 };

        Assert.All(Enumerable.Range(0, 200).Select(_ => Guid.NewGuid()), id => Assert.Equal(Stable, VoiceRoute.EndpointFor(options, "t1", id)));
    }

    [Fact]
    public void EndpointFor_CanaryAtOneHundredPercent_AlwaysUsesTheCanaryAgent()
    {
        var options = new VoiceLiveOptions { RelayEndpoint = Stable, CanaryEndpoint = Canary, CanaryPercent = 100 };

        Assert.All(Enumerable.Range(0, 200).Select(_ => Guid.NewGuid()), id => Assert.Equal(Canary, VoiceRoute.EndpointFor(options, "t1", id)));
    }

    [Fact]
    public void EndpointFor_TenPercent_SendsAboutTenPercentOfSessionsToTheCanaryAndTheSameSessionAlwaysTheSameWay()
    {
        var options = new VoiceLiveOptions { RelayEndpoint = Stable, CanaryEndpoint = Canary, CanaryPercent = 10 };
        var ids = Enumerable.Range(0, 2000).Select(_ => Guid.NewGuid()).ToList();

        var canary = ids.Count(id => VoiceRoute.EndpointFor(options, "t1", id) == Canary);

        Assert.InRange(canary, 120, 280);
        Assert.All(ids.Take(50), id => Assert.Equal(VoiceRoute.EndpointFor(options, "t1", id), VoiceRoute.EndpointFor(options, "t1", id)));
    }

    [Fact]
    public void EndpointFor_CanaryRestrictedToTeams_NeverSendsAnotherTeamToTheCanary()
    {
        var options = new VoiceLiveOptions { RelayEndpoint = Stable, CanaryEndpoint = Canary, CanaryPercent = 100, CanaryTeams = ["team-b"] };

        Assert.Equal(Canary, VoiceRoute.EndpointFor(options, "TEAM-B", Guid.NewGuid()));
        Assert.Equal(Stable, VoiceRoute.EndpointFor(options, "team-a", Guid.NewGuid()));
    }
}
