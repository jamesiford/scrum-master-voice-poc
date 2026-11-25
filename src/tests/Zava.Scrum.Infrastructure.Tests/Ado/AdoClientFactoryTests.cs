using System.Net;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Infrastructure.Tests.Ado;

/// <summary>One client and credential per Azure DevOps organisation, created lazily and cached (ADR 0018).</summary>
public sealed class AdoClientFactoryTests
{
    private const string OrgA = "https://dev.azure.com/org-a";
    private const string OrgB = "https://dev.azure.com/org-b";

    private readonly List<Uri> _seen = [];

    [Fact]
    public async Task For_TwoOrganisations_SendEachRequestToItsOwnOrganisationWithABearerToken()
    {
        using var factory = NewFactory();

        await factory.For(OrgA).GetAsync("_apis/projects");
        await factory.For(OrgB).GetAsync("_apis/projects");

        Assert.Equal(["https://dev.azure.com/org-a/_apis/projects", "https://dev.azure.com/org-b/_apis/projects"], _seen.Select(u => u.ToString()));
        Assert.Equal(2, factory.OrganisationCount);
    }

    [Fact]
    public void For_TheSameOrganisationWrittenDifferently_ReturnsOneCachedClient()
    {
        using var factory = NewFactory();

        var first = factory.For(OrgA);

        Assert.Same(first, factory.For("https://dev.azure.com/ORG-A/"));
        Assert.Equal(1, factory.OrganisationCount);
    }

    [Fact]
    public void For_NothingIsCreatedUntilAnOrganisationIsUsed() => Assert.Equal(0, NewFactory().OrganisationCount);

    [Fact]
    public void ReaderAndWriter_AreCachedPerOrganisation()
    {
        using var factory = NewFactory();

        Assert.Same(factory.ReaderFor(OrgA), factory.ReaderFor(OrgA));
        Assert.NotSame(factory.ReaderFor(OrgA), factory.ReaderFor(OrgB));
        Assert.Same(factory.WriterFor(OrgB), factory.WriterFor(OrgB));
        Assert.NotSame(factory.WriterFor(OrgA), factory.WriterFor(OrgB));
    }

    [Theory]
    [InlineData("https://evil.example.com/org-a")]
    [InlineData("http://dev.azure.com/org-a")]
    [InlineData("not a url")]
    public void For_AnAddressThatIsNotAnAzureDevOpsOrganisation_IsRefusedBeforeAnyClientExists(string url)
    {
        using var factory = NewFactory();

        Assert.Throws<ArgumentException>(() => factory.For(url));
        Assert.Equal(0, factory.OrganisationCount);
    }

    private AdoClientFactory NewFactory() =>
        new(new FakeCredential(), Options.Create(new AdoOptions()), Options.Create(new DemoOptions()), NullLoggerFactory.Instance, () => new Recorder(_seen));

    private sealed class FakeCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => new("token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => new(GetToken(requestContext, cancellationToken));
    }

    private sealed class Recorder(List<Uri> seen) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            seen.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        }
    }
}
