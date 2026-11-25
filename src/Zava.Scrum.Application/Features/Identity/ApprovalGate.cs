using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Identity;

/// <summary>
/// Gathers the facts <see cref="ApprovalAuthority"/> needs (roster match, configured administrator, tenant) and applies it, so every
/// decision handler asks the same question the same way (DRY). It never looks at a display name.
/// </summary>
internal sealed class ApprovalGate(IMemberRepository members, ITeamMembershipRepository memberships, IIdentityPolicy policy)
{
    /// <summary>Authorises a decision on something that belongs to a team.</summary>
    /// <param name="decision">Who is deciding.</param>
    /// <param name="teamId">The team.</param>
    /// <param name="memberId">The roster member the item is for, if any.</param>
    /// <param name="forScrumMaster">Whether it belongs to the Scrum Master.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success, or the reason the decision is refused.</returns>
    public async Task<Result> AuthoriseAsync(DecisionContext decision, string teamId, string? memberId, bool forScrumMaster, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(decision);
        var roster = await members.ListAsync(teamId, ct);
        var actorAsMember = roster.FirstOrDefault(m => m.EntraObjectId is { } id && id == decision.Actor.ObjectId);
        var isLead = (await memberships.ListByMemberAsync(decision.Actor.ObjectId, ct)).Any(m => m.TeamId == teamId && m.Role == TeamRole.TeamLead);
        return ApprovalAuthority.Authorise(
            decision.Actor,
            policy.TenantId,
            decision.Channel,
            decision.Roles,
            policy.IsAdmin(decision.Actor.ObjectId),
            isLead,
            memberId,
            forScrumMaster,
            actorAsMember);
    }
}
