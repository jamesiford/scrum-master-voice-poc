using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Audit.GetAuditChain;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>The audit chain (SPEC 14). One route, one query; the reader role is enough.</summary>
public static class AuditEndpoints
{
    /// <summary>Maps <c>GET /api/audit/{correlationId}</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit/{correlationId}", async (string correlationId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAuditChainQuery(correlationId), ct)).ToHttpResult());
        return app;
    }
}
