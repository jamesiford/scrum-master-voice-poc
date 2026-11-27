using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Domain.WorkSignals;

/// <summary>A team's named delegated signal owner, without credentials or Microsoft 365 content (ADR 0027).</summary>
/// <param name="TeamId">The application team, not a Microsoft 365 group id.</param>
/// <param name="OwnerObjectId">The consenting owner's Entra object id.</param>
/// <param name="TenantId">The deployment tenant.</param>
/// <param name="ConnectedAt">When the owner connected.</param>
/// <param name="RevokedAt">When consent was revoked, if applicable.</param>
public sealed record WorkSignalConsent(
    string TeamId, Guid OwnerObjectId, Guid TenantId, DateTimeOffset ConnectedAt, DateTimeOffset? RevokedAt = null)
{
    /// <summary>Records a connection only for the team's consenting lead, never an administrator's substitute identity.</summary>
    /// <param name="teamId">The team.</param>
    /// <param name="owner">The owner's current team membership.</param>
    /// <param name="tenantId">The validated tenant.</param>
    /// <param name="now">The connection timestamp.</param>
    /// <returns>Consent or a role refusal. Token consent must also succeed before saving this record.</returns>
    public static Result<WorkSignalConsent> Connect(string teamId, TeamMembership? owner, Guid tenantId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(teamId) || tenantId == Guid.Empty || owner is null ||
            owner.TeamId != teamId || owner.ObjectId == Guid.Empty || owner.Role != TeamRole.TeamLead)
        {
            return Result<WorkSignalConsent>.Failure(Error.Forbidden("signals.owner_required", "The signal owner must be a lead of this team."));
        }

        return Result<WorkSignalConsent>.Success(new(teamId, owner.ObjectId, tenantId, now));
    }

    /// <summary>Revokes this connection as its owner or an administrator; subsequent reads must fail closed.</summary>
    /// <param name="callerObjectId">The authenticated caller.</param>
    /// <param name="isAdmin">Whether the caller is a deployment administrator.</param>
    /// <param name="now">The revocation timestamp.</param>
    /// <returns>The revoked record, or a refusal.</returns>
    public Result<WorkSignalConsent> Revoke(Guid callerObjectId, bool isAdmin, DateTimeOffset now)
    {
        if (callerObjectId == Guid.Empty || !isAdmin && callerObjectId != OwnerObjectId)
        {
            return Result<WorkSignalConsent>.Failure(Error.Forbidden("signals.revoke_forbidden", "Only the signal owner or an administrator can revoke this connection."));
        }

        return Result<WorkSignalConsent>.Success(this with { RevokedAt = RevokedAt ?? now });
    }
}
