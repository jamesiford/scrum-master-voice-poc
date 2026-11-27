using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Domain.WorkSignals;

/// <summary>Fail-closed rules for opt-in, current membership and the exact delegated owner (ADR 0027).</summary>
public static class WorkSignalAccess
{
    /// <summary>Gets the visible unavailable-state error, not a successful empty evidence result.</summary>
    public static Error Unavailable { get; } = Error.Conflict("signals.unavailable", "No signals available. The signal owner must connect or sign in again.");

    /// <summary>Checks access before any token exchange or Work IQ call.</summary>
    /// <param name="team">The selected team.</param>
    /// <param name="consent">Its current connection.</param>
    /// <param name="owner">The owner's current membership, rechecked on every read.</param>
    /// <param name="assertionObjectId">The validated request assertion's owner.</param>
    /// <param name="assertionTenantId">The validated request assertion's tenant.</param>
    /// <param name="tenantId">The deployment tenant.</param>
    /// <returns>Success only for this enabled team's unrevoked, signed-in lead.</returns>
    public static Result Authorise(Team team, WorkSignalConsent? consent, TeamMembership? owner,
        Guid? assertionObjectId, Guid? assertionTenantId, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(team);
        if (!team.WorkSignalsEnabled || !team.IsOpen || team.Status == TeamStatus.Draft ||
            consent is null || consent.OwnerObjectId == Guid.Empty || consent.RevokedAt is not null || consent.TeamId != team.Id ||
            consent.TenantId != tenantId || tenantId == Guid.Empty || owner is null ||
            owner.TeamId != team.Id || owner.ObjectId != consent.OwnerObjectId || owner.Role != TeamRole.TeamLead ||
            assertionObjectId != consent.OwnerObjectId || assertionTenantId != tenantId)
        {
            return Result.Failure(Unavailable);
        }

        return Result.Success();
    }
}
