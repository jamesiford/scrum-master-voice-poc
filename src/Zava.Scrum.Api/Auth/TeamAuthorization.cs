using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Zava.Scrum.Api.Endpoints;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Api.Auth;

/// <summary>The team permission a route needs. Added by the group convention, or by a route that needs more (approve) or less (a read-only POST).</summary>
/// <param name="Permission">The permission.</param>
internal sealed record TeamPermissionMetadata(TeamPermission Permission);

/// <summary>Marks a route that needs the Admin app role: the registry and anything across teams.</summary>
internal sealed record RegistryOnlyMetadata;

/// <summary>Marks a route that carries no team (health, the caller's own team list, the registry list); it filters its answer itself.</summary>
internal sealed record NoTeamScopeMetadata;

/// <summary>Builds the caller from the validated token: object id, and whether the Admin app role is present.</summary>
public static class CallerContextFactory
{
    /// <summary>Reads the caller.</summary>
    /// <param name="user">The authenticated principal.</param>
    /// <returns>The caller, or <see langword="null"/> when the token has no valid object id.</returns>
    public static CallerContext? From(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Guid.TryParse(user.FindFirstValue("oid"), out var oid) && oid != Guid.Empty
            ? new CallerContext(oid, user.FindAll("roles").Any(c => c.Value == AppRoles.Admin))
            : null;
    }
}

/// <summary>
/// Resource-based authorisation, server side, on every route (ADR 0018). It works out which team the request is about (a route value, a query
/// value, a body value, or the team of the session, follow-up, plan, calendar or audit chain the id names), asks the resolver whether this caller
/// may do what the route does to that team, and refuses otherwise. A route that names no team and is not marked as team-free is refused,
/// so a new route is closed until someone says which team it belongs to. A team the caller is not in is answered "not found", as if it did not exist.
/// </summary>
public sealed partial class TeamAuthorizationFilter(ITeamResolver resolver, IResourceTeamLocator locator, ILogger<TeamAuthorizationFilter> logger) : IEndpointFilter
{
    /// <summary>The key under which the resolved <see cref="Team"/> is kept for the request.</summary>
    public const string TeamItem = "Zava.Team";

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var http = context.HttpContext;
        var metadata = http.GetEndpoint()?.Metadata;
        if (metadata?.GetMetadata<NoTeamScopeMetadata>() is not null)
        {
            return await next(context);
        }

        var caller = CallerContextFactory.From(http.User);
        if (caller is null)
        {
            return Results.Json(new { error = "identity.object_id_required", detail = "The token carries no object id." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (metadata?.GetMetadata<RegistryOnlyMetadata>() is not null)
        {
            if (caller.IsAdmin)
            {
                return await next(context);
            }

            logger.LogWarning("Refused registry route {Path} for caller {Caller}", http.Request.Path, caller.ObjectId);
            return Result.Failure(TeamAccess.Forbidden).ToHttpResult();
        }

        var permission = metadata?.GetMetadata<TeamPermissionMetadata>()?.Permission ?? (HttpMethods.IsGet(http.Request.Method) ? TeamPermission.Read : TeamPermission.Operate);
        var teamId = await LocateAsync(context);
        if (teamId.Error is not null)
        {
            return Result.Failure(teamId.Error).ToHttpResult();
        }

        var resolved = await resolver.ForCallerAsync(caller, teamId.Value, permission, http.RequestAborted);
        if (!resolved.IsSuccess && permission == TeamPermission.Approve && http.User.FindAll("roles").Any(c => c.Value == AppRoles.Approver))
        {
            // The Approver app role from ADR 0017 still approves for a team the caller belongs to.
            resolved = await resolver.ForCallerAsync(caller, teamId.Value, TeamPermission.Read, http.RequestAborted);
        }

        if (!resolved.IsSuccess)
        {
            return resolved.ToHttpResult();
        }

        http.Items[TeamItem] = resolved.Value;
        return await next(context);
    }

    private async Task<Result<string>> LocateAsync(EndpointFilterInvocationContext context)
    {
        var http = context.HttpContext;
        var ct = http.RequestAborted;
        var path = http.Request.Path.Value ?? string.Empty;
        var route = http.Request.RouteValues;
        if (route.TryGetValue("teamId", out var rt) && rt is string routeTeam)
        {
            return Result<string>.Success(routeTeam);
        }

        if (path.StartsWith("/api/teams/", StringComparison.OrdinalIgnoreCase) && route.TryGetValue("id", out var id) && id is string teamsId)
        {
            return Result<string>.Success(teamsId);
        }

        if (http.Request.Query["teamId"].FirstOrDefault() is { Length: > 0 } queryTeam)
        {
            return Result<string>.Success(queryTeam);
        }

        if (MatchGuid(SessionPath(), path) is { } session)
        {
            return await locator.ForSessionAsync(session, ct);
        }

        if (MatchGuid(FollowUpPath(), path) is { } followUp)
        {
            return await locator.ForFollowUpAsync(followUp, ct);
        }

        if (MatchGuid(PackPath(), path) is { } pack)
        {
            return await locator.ForPlanningPackAsync(pack, ct);
        }

        if (MatchGuid(CalendarPath(), path) is { } calendar)
        {
            return await locator.ForCalendarAsync(calendar, ct);
        }

        if (AuditPath().Match(path) is { Success: true } audit)
        {
            return await locator.ForCorrelationAsync(audit.Groups[1].Value, ct);
        }

        if (Guid.TryParse(http.Request.Query["sessionId"].FirstOrDefault(), out var querySession))
        {
            return await locator.ForSessionAsync(querySession, ct);
        }

        return await FromArgumentsAsync(context);
    }

    // Parameter binding has already read the body by the time a filter runs, so the team comes from the bound arguments: a body (or an argument)
    // that carries a TeamId, or a SessionId whose session belongs to a team.
    private async Task<Result<string>> FromArgumentsAsync(EndpointFilterInvocationContext context)
    {
        var http = context.HttpContext;
        foreach (var argument in context.Arguments)
        {
            var type = argument?.GetType();
            if (argument is null || type is null || type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
            {
                continue;
            }

            if (type.GetProperty("TeamId")?.GetValue(argument) is string team && team.Length > 0)
            {
                return Result<string>.Success(team);
            }

            if (type.GetProperty("SessionId")?.GetValue(argument) is Guid session && session != Guid.Empty)
            {
                return await locator.ForSessionAsync(session, http.RequestAborted);
            }
        }

        logger.LogWarning("Refused {Method} {Path}: the request names no team", http.Request.Method, http.Request.Path);
        return Result<string>.Failure(TeamErrors.NoTeam);
    }
    private static Guid? MatchGuid(Regex pattern, string path) => pattern.Match(path) is { Success: true } m && Guid.TryParse(m.Groups[1].Value, out var id) ? id : null;

    [GeneratedRegex("^/api/sessions/([0-9a-fA-F-]{36})(/|$)", RegexOptions.CultureInvariant)]
    private static partial Regex SessionPath();

    [GeneratedRegex("^/api/followups/([0-9a-fA-F-]{36})(/|$)", RegexOptions.CultureInvariant)]
    private static partial Regex FollowUpPath();

    [GeneratedRegex("^/api/planning/packs/([0-9a-fA-F-]{36})(/|$)", RegexOptions.CultureInvariant)]
    private static partial Regex PackPath();

    [GeneratedRegex("^/api/calendar/proposals/([0-9a-fA-F-]{36})(/|$)", RegexOptions.CultureInvariant)]
    private static partial Regex CalendarPath();

    [GeneratedRegex("^/api/audit/([^/]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex AuditPath();
}
