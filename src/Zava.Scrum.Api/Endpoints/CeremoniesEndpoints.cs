using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Ceremonies.CreateCeremonyPack;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Ceremony pack endpoint (M9). One route, one command.</summary>
public static class CeremoniesEndpoints
{
    /// <summary>Maps <c>/api/ceremonies/{kind}/pack</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapCeremoniesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ceremonies/{kind}/pack", async (CeremonyKind kind, PackBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateCeremonyPackCommand(body.TeamId, kind), ct)).ToHttpResult());
        return app;
    }

    private sealed record PackBody(string TeamId);
}
