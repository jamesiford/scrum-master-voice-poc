using System.Security.Claims;
using Zava.Scrum.Api.Endpoints;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Api.Auth;

/// <summary>Turns the validated portal principal into the identity the handlers decide on. It reads claims only, never a header or a body field.</summary>
public static class PortalActor
{
    private static readonly string[] Recorded = ["oid", "tid", "azp", "azpacr", "scp", "ver", "iat", "auth_time", "aud", "iss"];

    /// <summary>Runs a decision command as the caller: identity from the validated token, or the problem body when the token has none.</summary>
    /// <param name="http">The request.</param>
    /// <param name="send">Sends the command with the caller's decision context.</param>
    /// <returns>The HTTP result.</returns>
    public static async Task<IResult> DecideAsync(HttpContext http, Func<DecisionContext, Task<Result>> send)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(send);
        var decision = From(http.User);
        return decision.IsSuccess ? (await send(decision.Value)).ToHttpResult() : Result.Failure(decision.Error!).ToHttpResult();
    }

    /// <summary>Runs a decision command that returns a value as the caller.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="http">The request.</param>
    /// <param name="send">Sends the command with the caller's decision context.</param>
    /// <returns>The HTTP result.</returns>
    public static async Task<IResult> DecideAsync<T>(HttpContext http, Func<DecisionContext, Task<Result<T>>> send)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(send);
        var decision = From(http.User);
        return decision.IsSuccess ? (await send(decision.Value)).ToHttpResult() : Result.Failure(decision.Error!).ToHttpResult();
    }

    /// <summary>Builds a decision context from the caller's validated token.</summary>
    /// <param name="user">The authenticated principal.</param>
    /// <returns>The context, or a validation error when the token lacks an object id, tenant or name.</returns>
    public static Result<DecisionContext> From(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var name = user.FindFirstValue("name") ?? user.FindFirstValue("preferred_username");
        var upn = user.FindFirstValue("preferred_username") ?? user.FindFirstValue("upn");
        var actor = ActorIdentity.Create(user.FindFirstValue("oid"), user.FindFirstValue("tid"), name, upn, ActorSource.Portal, "entra-bearer");
        if (!actor.IsSuccess)
        {
            return Result<DecisionContext>.Failure(actor.Error!);
        }

        var roles = user.FindAll("roles").Select(c => c.Value).Distinct(StringComparer.Ordinal).ToList();
        var claims = Recorded
            .Select(type => (type, value: user.FindFirstValue(type)))
            .Where(c => !string.IsNullOrEmpty(c.value))
            .ToDictionary(c => c.type, c => c.value!);
        if (roles.Count > 0)
        {
            claims["roles"] = string.Join(' ', roles);
        }

        return Result<DecisionContext>.Success(new DecisionContext(actor.Value, ApprovalChannel.Portal, new AuthenticationContext("entra-bearer", claims), roles));
    }
}
