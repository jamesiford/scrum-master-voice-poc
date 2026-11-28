using Zava.Scrum.Api.Auth;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Demo.GetDemoStatus;
using Zava.Scrum.Application.Features.Demo.ResetDemo;
using Zava.Scrum.Application.Features.Demo.ResetDemoAdo;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Application.Features.Demo.GetDemoReseed;
using Zava.Scrum.Application.Features.Demo.AbortDemoReseed;
using Zava.Scrum.Application.Features.Demo.OptInDemoScope;
using Microsoft.AspNetCore.Authorization;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Demo endpoints (SPEC 14): reset state so the storyboard can be re-run.</summary>
public static class DemoEndpoints
{
    private static readonly AuthorizationPolicy AdminPolicy = new AuthorizationPolicyBuilder(Policies.Scheme)
        .RequireAuthenticatedUser().RequireRole(AppRoles.Admin).Build();

    /// <summary>Maps <c>/api/demo</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/demo");
        group.MapPost("/reset", (HttpContext http, ISender sender, CancellationToken ct) =>
            PortalActor.DecideAsync(http, decision => sender.Send(new ResetDemoCommand(decision), ct))).RequireAuthorization(AdminPolicy).RequireRegistry();
        group.MapPost("/reseed-scope", (DemoResetScopeManifest manifest, HttpContext http, ISender sender, CancellationToken ct) =>
            PortalActor.DecideAsync(http, decision => sender.Send(new OptInDemoScopeCommand(manifest, decision), ct))).RequireAuthorization(AdminPolicy).RequireRegistry();
        group.MapPost("/reset-ado", async (string teamId, ISender sender, CancellationToken ct) => (await sender.Send(new ResetDemoAdoCommand(teamId), ct)).ToHttpResult());
        group.MapGet("/status", async (string teamId, ISender sender, CancellationToken ct) => (await sender.Send(new GetDemoStatusQuery(teamId), ct)).ToHttpResult());
        group.MapPost("/reseed", (string teamId, HttpContext http, ISender sender, CancellationToken ct) =>
            PortalActor.DecideAsync(http, decision => sender.Send(new ReseedDemoCommand(teamId, decision), ct))).RequireAuthorization(AdminPolicy)
            .WithMetadata(new TeamPermissionMetadata(TeamPermission.Operate));
        group.MapPost("/reseed/{operationId:guid}/commit", (string teamId, Guid operationId, HttpContext http, ISender sender, CancellationToken ct) =>
            PortalActor.DecideAsync(http, decision => sender.Send(new ReseedDemoCommand(teamId, decision, operationId), ct))).RequireAuthorization(AdminPolicy)
            .WithMetadata(new TeamPermissionMetadata(TeamPermission.Operate));
        group.MapGet("/reseed/{operationId:guid}", (string teamId, Guid operationId, HttpContext http, ISender sender, CancellationToken ct) =>
            PortalActor.DecideAsync(http, decision => sender.Send(new GetDemoReseedQuery(teamId, operationId, decision), ct))).RequireAuthorization(AdminPolicy)
            .WithMetadata(new TeamPermissionMetadata(TeamPermission.Read));
        group.MapPost("/reseed/{operationId:guid}/abort", (string teamId, Guid operationId, HttpContext http, ISender sender, CancellationToken ct) =>
            PortalActor.DecideAsync(http, decision => sender.Send(new AbortDemoReseedCommand(teamId, operationId, decision), ct))).RequireAuthorization(AdminPolicy)
            .WithMetadata(new TeamPermissionMetadata(TeamPermission.Operate));
        return app;
    }
}
