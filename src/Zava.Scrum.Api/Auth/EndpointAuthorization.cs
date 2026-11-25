using Microsoft.AspNetCore.Authorization;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Api.Auth;

/// <summary>Marks a mutating route that does not change anything and may be called by anyone who can read the team (for example a question to the agent).</summary>
internal sealed record ReadOnlyOperationMetadata;

/// <summary>
/// Route requirements (ADR 0018). Every <c>/api</c> route needs a signed-in caller, and the <see cref="TeamAuthorizationFilter"/> then checks
/// the caller against the team the route is about: read for a GET, operate for anything that changes state, approve for decisions. The
/// registry and anything across teams need the Admin app role.
/// </summary>
public static class EndpointAuthorization
{
    /// <summary>Requires the approve permission on the route's team (a team lead or an Admin).</summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The route.</param>
    /// <returns>The same builder.</returns>
    public static TBuilder RequireApprover<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder => builder.WithMetadata(new TeamPermissionMetadata(TeamPermission.Approve));

    /// <summary>Marks a POST route as read-only so a reader of the team may call it.</summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The route.</param>
    /// <returns>The same builder.</returns>
    public static TBuilder AsReadOnlyOperation<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder => builder.WithMetadata(new ReadOnlyOperationMetadata());

    /// <summary>Requires the Admin app role: the registry and anything across teams.</summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The route.</param>
    /// <returns>The same builder.</returns>
    public static TBuilder RequireRegistry<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder => builder.WithMetadata(new RegistryOnlyMetadata());

    /// <summary>Marks a route that names no team and filters its own answer (health, the caller's team list).</summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The route.</param>
    /// <returns>The same builder.</returns>
    public static TBuilder NoTeamScope<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder => builder.WithMetadata(new NoTeamScopeMetadata());

    /// <summary>
    /// Puts every route in the group behind authentication and the team filter. Runs last, so it sees each route's own metadata and adds the
    /// default permission (read for GET and read-only operations, operate for the rest) where a route did not state one.
    /// </summary>
    /// <param name="group">The group all API routes are mapped into.</param>
    /// <returns>The same group.</returns>
    public static RouteGroupBuilder RequireRolesByDefault(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.RequireAuthorization(Policies.Reader);
        group.AddEndpointFilter<TeamAuthorizationFilter>();
        ((IEndpointConventionBuilder)group).Finally(builder =>
        {
            if (builder is not RouteEndpointBuilder route || route.Metadata.OfType<TeamPermissionMetadata>().Any())
            {
                return;
            }

            var read = IsRead(route) || route.Metadata.OfType<ReadOnlyOperationMetadata>().Any();
            route.Metadata.Add(new TeamPermissionMetadata(read ? TeamPermission.Read : TeamPermission.Operate));
        });
        return group;
    }

    private static bool IsRead(RouteEndpointBuilder route) =>
        route.Metadata.OfType<IHttpMethodMetadata>().FirstOrDefault()?.HttpMethods.All(m => HttpMethods.IsGet(m) || HttpMethods.IsHead(m)) ?? true;
}
