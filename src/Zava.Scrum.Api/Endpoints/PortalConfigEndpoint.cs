using Microsoft.Extensions.Options;
using Zava.Scrum.Api.Auth;
using Zava.Scrum.Infrastructure.Identity;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>
/// Serves the public values the portal needs to sign in: the app registration's client id and the tenant. Neither is a secret, and the
/// portal cannot sign in before it has them, so the route is anonymous and sits outside <c>/api</c>.
/// </summary>
public static class PortalConfigEndpoint
{
    /// <summary>Maps <c>GET /portal-config.json</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapPortalConfigEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/portal-config.json", (IOptions<PortalAuthOptions> options, IConfiguration configuration) =>
        {
            var o = options.Value;
            var tenant = string.IsNullOrWhiteSpace(o.TenantId) ? IdentityOptions.ResolveTenant(configuration) : o.TenantId;
            return Results.Json(new { clientId = string.IsNullOrWhiteSpace(o.ClientId) ? null : o.ClientId, tenantId = tenant });
        }).AllowAnonymous();
        return app;
    }
}
