using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Api.Tests;

/// <summary>Route architecture: film reads inherit authentication and team-read authorisation, never registry-wide access.</summary>
public sealed class FilmRouteArchitectureTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("/api/audit/{correlationId}")]
    [InlineData("/api/sessions/{id:guid}/latency")]
    public void FilmRead_MappedEndpoint_IsAuthenticatedAndTeamScoped(string pattern)
    {
        var endpoint = Assert.Single(factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>(), e => e.RoutePattern.RawText == pattern);

        Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(), a => a.Policy is not null);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
        Assert.Contains(endpoint.Metadata, m => m.GetType().Name == "TeamPermissionMetadata" && m.ToString()!.Contains("Read", StringComparison.Ordinal));
        Assert.DoesNotContain(endpoint.Metadata, m => m.GetType().Name is "NoTeamScopeMetadata" or "RegistryOnlyMetadata");
    }
}
