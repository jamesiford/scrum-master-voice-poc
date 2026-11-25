using Zava.Scrum.Api.Auth;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Planning.ApprovePlan;
using Zava.Scrum.Application.Features.Planning.GetPlanningPack;
using Zava.Scrum.Application.Features.Planning.GetPlanningPrep;
using Zava.Scrum.Application.Features.Planning.PreparePlanning;
using Zava.Scrum.Application.Features.Planning.RejectPlan;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Sprint planning endpoints (SPEC M8, M9a). Each route sends one command or query.</summary>
public static class PlanningEndpoints
{
    /// <summary>Maps <c>/api/planning</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapPlanningEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/planning", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPlanningPrepQuery(teamId), ct)).ToHttpResult());

        app.MapPost("/api/planning/prepare", async (PrepareBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new PreparePlanningCommand(body.TeamId), ct)).ToHttpResult());

        app.MapGet("/api/planning/pack", async (string teamId, Guid? packId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPlanningPackQuery(teamId, packId), ct)).ToHttpResult());

        app.MapPost("/api/planning/packs/{id:guid}/approve", (Guid id, ApproveBody body, HttpContext http, ISender sender, CancellationToken ct) =>
            PortalActor.DecideAsync(http, d => sender.Send(new ApprovePlanCommand(id, d, body.SelectedItemIds ?? []), ct))).RequireApprover();

        app.MapPost("/api/planning/packs/{id:guid}/reject", (Guid id, HttpContext http, ISender sender, CancellationToken ct) =>
            PortalActor.DecideAsync(http, d => sender.Send(new RejectPlanCommand(id, d), ct))).RequireApprover();
        return app;
    }

    private sealed record PrepareBody(string TeamId);

    private sealed record ApproveBody(IReadOnlyList<int>? SelectedItemIds);
}
