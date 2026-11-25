using System.Security.Claims;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Api.Auth;

/// <summary>
/// Hands the portal caller's validated bearer token to the Application layer for the length of the request, so an approval can be
/// written to Azure DevOps as that person (ADR 0020). It runs after authentication, so the <c>oid</c> and <c>tid</c> it copies come from
/// a token this app has already validated; it never reads a header on its own authority or logs the token.
/// </summary>
public sealed class UserAssertionMiddleware(RequestDelegate next)
{
    /// <summary>Sets the assertion when the request is an authenticated portal call.</summary>
    /// <param name="context">The request.</param>
    /// <param name="accessor">The scoped assertion holder.</param>
    /// <returns>A task that completes when the rest of the pipeline has run.</returns>
    public Task InvokeAsync(HttpContext context, IUserAssertionAccessor accessor)
    {
        var header = context.Request.Headers.Authorization.ToString();
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true
            && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(user.FindFirstValue("oid"), out var oid)
            && Guid.TryParse(user.FindFirstValue("tid"), out var tid))
        {
            accessor.Current = new UserAssertion(header["Bearer ".Length..].Trim(), oid, tid);
        }

        return next(context);
    }
}
