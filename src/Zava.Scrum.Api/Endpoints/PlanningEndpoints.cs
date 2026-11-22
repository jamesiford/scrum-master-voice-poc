using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Features.Planning.ApprovePlan;
using Mcm.Scrum.Application.Features.Planning.GetPlanningPack;
using Mcm.Scrum.Application.Features.Planning.GetPlanningPrep;
using Mcm.Scrum.Application.Features.Planning.PreparePlanning;
using Mcm.Scrum.Application.Features.Planning.RejectPlan;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Api.Endpoints;

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

        app.MapPost("/api/planning/packs/{id:guid}/approve", async (Guid id, ApproveBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ApprovePlanCommand(id, body.Approver, body.SelectedItemIds ?? [], ApprovalChannel.Portal), ct)).ToHttpResult());

        app.MapPost("/api/planning/packs/{id:guid}/reject", async (Guid id, RejectBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RejectPlanCommand(id, body.Approver), ct)).ToHttpResult());
        return app;
    }

    private sealed record PrepareBody(string TeamId);

    private sealed record ApproveBody(string Approver, IReadOnlyList<int>? SelectedItemIds);

    private sealed record RejectBody(string Approver);
}
