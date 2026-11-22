using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Demo.GetDemoStatus;
using Zava.Scrum.Application.Features.Demo.ResetDemo;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Demo endpoints (SPEC 14): reset state so the storyboard can be re-run.</summary>
public static class DemoEndpoints
{
    /// <summary>Maps <c>/api/demo</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/demo");
        group.MapPost("/reset", async (ISender sender, CancellationToken ct) => (await sender.Send(new ResetDemoCommand(), ct)).ToHttpResult());
        group.MapGet("/status", async (string teamId, ISender sender, CancellationToken ct) => (await sender.Send(new GetDemoStatusQuery(teamId), ct)).ToHttpResult());
        return app;
    }
}
