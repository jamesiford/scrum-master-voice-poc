using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Teams;

/// <summary>
/// Decides whether a caller may do something to a team. Pure and server side. An Admin reaches every team; anyone else needs a
/// membership of that team with a role that carries the permission. A team the caller is not in is reported as not found, so its
/// existence is not revealed.
/// </summary>
public static class TeamAccess
{
    /// <summary>Gets the error for a team the caller cannot see (or that does not exist).</summary>
    /// <param name="teamId">The team.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string teamId) => Error.NotFound("team.not_found", $"Team '{teamId}' does not exist.");

    /// <summary>Gets the error for a caller who is in the team but may not do this.</summary>
    public static Error Forbidden { get; } = Error.Forbidden("team.forbidden", "Your role in this team does not allow that.");

    /// <summary>Authorises a caller for a permission on a team.</summary>
    /// <param name="teamId">The team.</param>
    /// <param name="isAdmin">Whether the caller holds the Admin app role.</param>
    /// <param name="membership">The caller's membership of the team, or <see langword="null"/>.</param>
    /// <param name="permission">What is needed.</param>
    /// <returns>Success, or not found (not in the team) or forbidden (in the team, wrong role).</returns>
    public static Result Authorise(string teamId, bool isAdmin, TeamMembership? membership, TeamPermission permission)
    {
        if (isAdmin)
        {
            return Result.Success();
        }

        if (membership is null || !string.Equals(membership.TeamId, teamId, StringComparison.Ordinal))
        {
            return Result.Failure(NotFound(teamId));
        }

        var allowed = permission switch
        {
            TeamPermission.Read => true,
            TeamPermission.Operate or TeamPermission.Approve => membership.Role == TeamRole.TeamLead,
            _ => false,
        };
        return allowed ? Result.Success() : Result.Failure(Forbidden);
    }
}
