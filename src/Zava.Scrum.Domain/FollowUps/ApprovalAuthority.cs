using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Domain.FollowUps;

/// <summary>
/// Decides whether an identified actor may approve or reject a follow-up. Pure: the adapters supply the facts (the verified identity,
/// the roles in the token, the roster member the object id matches, whether the object id is a configured administrator).
/// Display names are never consulted (ADR 0017).
/// </summary>
public static class ApprovalAuthority
{
    /// <summary>Authorises a decision.</summary>
    /// <param name="actor">The verified identity, or <see langword="null"/> when none was established.</param>
    /// <param name="expectedTenant">The tenant the deployment belongs to.</param>
    /// <param name="channel">Where the decision came from.</param>
    /// <param name="roles">App roles from the validated portal token (empty for Teams).</param>
    /// <param name="isConfiguredAdmin">Whether the actor's object id is a configured administrator (used for Teams).</param>
    /// <param name="isTeamLead">Whether the actor is a team lead of the follow-up's team (a membership, by object id).</param>
    /// <param name="followUpMemberId">The roster id of the member the follow-up is for, if any.</param>
    /// <param name="forScrumMaster">Whether the follow-up belongs to the Scrum Master, not a member.</param>
    /// <param name="actorAsMember">The roster member whose Entra object id equals the actor's, if any.</param>
    /// <returns>Success, or a validation or forbidden error.</returns>
    public static Result Authorise(
        ActorIdentity? actor,
        Guid expectedTenant,
        ApprovalChannel channel,
        IReadOnlyCollection<string> roles,
        bool isConfiguredAdmin,
        bool isTeamLead,
        string? followUpMemberId,
        bool forScrumMaster,
        Member? actorAsMember)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (actor is null || actor.ObjectId == Guid.Empty)
        {
            return Result.Failure(IdentityErrors.ObjectIdRequired);
        }

        if (actor.TenantId != expectedTenant)
        {
            return Result.Failure(IdentityErrors.WrongTenant);
        }

        return channel switch
        {
            ApprovalChannel.Portal => AuthorisePortal(actor, roles, isTeamLead),
            ApprovalChannel.Teams => AuthoriseTeams(actor, isConfiguredAdmin || isTeamLead, followUpMemberId, forScrumMaster, actorAsMember),
            _ => Result.Failure(IdentityErrors.WrongSource),
        };
    }

    private static Result AuthorisePortal(ActorIdentity actor, IReadOnlyCollection<string> roles, bool isTeamLead)
    {
        if (actor.Source != ActorSource.Portal)
        {
            return Result.Failure(IdentityErrors.WrongSource);
        }

        return isTeamLead || roles.Contains(AppRoles.Approver, StringComparer.Ordinal) ? Result.Success() : Result.Failure(IdentityErrors.ApproverRoleRequired);
    }

    private static Result AuthoriseTeams(ActorIdentity actor, bool isConfiguredAdmin, string? followUpMemberId, bool forScrumMaster, Member? actorAsMember)
    {
        if (actor.Source != ActorSource.Teams)
        {
            return Result.Failure(IdentityErrors.WrongSource);
        }

        if (isConfiguredAdmin)
        {
            return Result.Success();
        }

        var isTheirOwn = actorAsMember is not null && !forScrumMaster && followUpMemberId is not null
            && string.Equals(actorAsMember.Id, followUpMemberId, StringComparison.OrdinalIgnoreCase);
        return isTheirOwn ? Result.Success() : Result.Failure(IdentityErrors.NotOnRoster);
    }
}
