using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Zava.Scrum.Api.Tests;

/// <summary>Without a portal client id, outside Development, the API stays up but refuses every portal call.</summary>
public sealed class NoClientIdTests : IClassFixture<NoClientIdTests.Factory>
{
    private readonly Factory _factory;

    public NoClientIdTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task PortalRoutes_Are401_AndHealthStaysUp()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/healthz")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/health")).StatusCode);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Portal:ClientId", string.Empty);
            builder.UseSetting("Store:DataPath", Path.Combine(Path.GetTempPath(), "zava-api-tests-noclient"));
        }
    }
}
