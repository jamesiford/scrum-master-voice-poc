using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Health.GetHealth;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Health endpoints.</summary>
public static class HealthEndpoints
{
    /// <summary>Maps <c>/api/health</c>, served through <see cref="GetHealthQuery"/> to prove the pipeline end to end.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health");
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetHealthQuery(), ct)).ToHttpResult());
        return app;
    }
}
