using System.Net;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Infrastructure.Tests.Ado;

public sealed class DelegatedWorkItemReaderTests
{
    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GetCurrentSprint_AdoDeniesAsker_ReturnsRefusalAndNeverRequestsAgentToken(HttpStatusCode status)
    {
        var seen = new List<string>();
        using var factory = Factory(new Denial(status, seen));
        var reader = new DelegatedWorkItemReader(factory, Options.Create(new DemoOptions()), NullLoggerFactory.Instance);
        var team = new Team("t1", "One", "UTC", 15, new("https://dev.azure.com/zava", "p", "t", "p"), new TeamBehaviours(), "Ship");

        var result = await reader.GetCurrentSprintAsync(team, new DelegatedAdoAccess("asker-token"), default);

        Assert.Equal("copilot.ado_denied", result.Error!.Code);
        Assert.Equal(["asker-token"], seen);
        Assert.Equal(0, factory.OrganisationCount);
    }

    [Fact]
    public async Task DelegatedClient_DifferentAskers_SharesConnectionsButNeverCredentials()
    {
        var seen = new List<string>();
        using var factory = Factory(new Denial(HttpStatusCode.Forbidden, seen));
        using var first = factory.DelegatedClientFor("https://dev.azure.com/zava", "first-token");
        using var second = factory.DelegatedClientFor("https://dev.azure.com/zava", "second-token");

        using var a = await first.GetAsync("_apis/projects");
        using var b = await second.GetAsync("_apis/projects");

        Assert.Equal(["first-token", "second-token"], seen);
        Assert.Equal(0, factory.OrganisationCount);
    }

    private static AdoClientFactory Factory(HttpMessageHandler handler) =>
        new(new ForbiddenCredential(), Options.Create(new AdoOptions()), Options.Create(new DemoOptions()), NullLoggerFactory.Instance, () => handler);

    private sealed class ForbiddenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The agent credential must never be requested.");

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The agent credential must never be requested.");
    }

    private sealed class Denial(HttpStatusCode status, List<string> seen) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            seen.Add(request.Headers.Authorization!.Parameter!);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("{}") });
        }
    }
}
