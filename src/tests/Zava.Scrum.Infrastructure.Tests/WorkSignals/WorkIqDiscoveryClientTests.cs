using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.WorkSignals;

namespace Zava.Scrum.Infrastructure.Tests.WorkSignals;

public sealed class WorkIqDiscoveryClientTests
{
    [Theory]
    [InlineData(false, "t1", true)]
    [InlineData(true, "other-team", true)]
    [InlineData(true, "t1", false)]
    public async Task Discover_DisabledOrInvalidTeamContext_MakesZeroTokenAndWorkIqCalls(
        bool enabled, string scopeTeam, bool hasCorrelation)
    {
        using var http = new HttpClient();
        var tokens = new RefusingTokens();
        var client = new WorkIqDiscoveryClient(http, tokens);
        var context = Context(enabled, scopeTeam, hasCorrelation);

        var result = await client.DiscoverAsync(context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("signals.unavailable", result.Error!.Code);
        Assert.Equal(0, tokens.Calls);
    }

    [Fact]
    public async Task Discover_TokenRefused_DoesNotFallbackToAnotherIdentity()
    {
        using var http = new HttpClient();
        var tokens = new RefusingTokens();
        var client = new WorkIqDiscoveryClient(http, tokens);

        Result<JsonElement> result = await client.DiscoverAsync(Context(true, "t1", true), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("signals.sign_in_required", result.Error!.Code);
        Assert.Equal(1, tokens.Calls);
    }

    private static WorkSignalContext Context(bool enabled, string scopeTeam, bool hasCorrelation) => new(
        new Team("t1", "Test", "UTC", 15, new AdoSettings("https://dev.azure.com/test", "Test", "Test", "Test"),
            new TeamBehaviours(), "Test", WorkSignalsEnabled: enabled),
        new UserAssertion("test-assertion", Guid.NewGuid(), Guid.NewGuid()),
        new WorkSignalScope(scopeTeam, "group", "channel", [], [], []),
        hasCorrelation ? Guid.NewGuid() : Guid.Empty);

    private sealed class RefusingTokens : IWorkSignalTokenSource
    {
        public int Calls { get; private set; }

        public Task<Result<DelegatedWorkSignalAccess>> ExchangeAsync(UserAssertion assertion, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Result<DelegatedWorkSignalAccess>.Failure(
                Error.Conflict("signals.sign_in_required", "No signals available. Owner sign-in required.")));
        }
    }
}
