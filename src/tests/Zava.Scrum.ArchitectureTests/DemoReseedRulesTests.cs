using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NetArchTest.Rules;
using Zava.Scrum.Api.Endpoints;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.ArchitectureTests;

/// <summary>Reset routes require Admin, and staging/publication cannot bypass the approved work-item writer.</summary>
public sealed class DemoReseedRulesTests
{
    [Fact]
    public async Task ReseedRoutes_AllRequireAuthenticatedAdminAndRealTeamPermission()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
        builder.Services.AddSingleton<ISender>(_ => null!);
        await using var app = builder.Build();
        app.MapDemoEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
            .Where(route => route.RoutePattern.RawText == "/api/demo/reseed"
                || route.RoutePattern.RawText!.StartsWith("/api/demo/reseed/", StringComparison.Ordinal)).ToList();

        Assert.Equal(4, routes.Count);
        Assert.All(routes, route => Assert.Contains(route.Metadata, m => m.GetType().Name == "TeamPermissionMetadata"));
        Assert.All(routes, route => Assert.DoesNotContain(route.Metadata, m => m.GetType().Name is "NoTeamScopeMetadata" or "RegistryOnlyMetadata"));
        Assert.All(routes, route =>
        {
            var policy = Assert.Single(route.Metadata.GetOrderedMetadata<AuthorizationPolicy>());
            Assert.Contains(policy.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
            Assert.Contains(policy.Requirements, requirement => requirement is RolesAuthorizationRequirement roles && roles.AllowedRoles.Contains(AppRoles.Admin));
        });
    }

    [Fact]
    public void PersistentReset_DoesNotReferenceWriterOrOldDestructiveAdoReset()
    {
        var assembly = typeof(Zava.Scrum.Infrastructure.SystemClock).Assembly;
        var result = Types.InAssembly(assembly).That().HaveName("PersistentDemoReseed")
            .ShouldNot().HaveDependencyOnAny(typeof(IWorkItemWriter).FullName!, typeof(IDemoAdoReset).FullName!).GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
