using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Features.Planning.GetPlanningPrep;

namespace Mcm.Scrum.Api.Endpoints;

/// <summary>Planning prep endpoint (SPEC M8). One route, one query.</summary>
public static class PlanningEndpoints
{
    /// <summary>Maps <c>/api/planning</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapPlanningEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/planning", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPlanningPrepQuery(teamId), ct)).ToHttpResult());
        return app;
    }
}
