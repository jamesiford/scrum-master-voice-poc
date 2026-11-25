using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Identity;

/// <summary>
/// Who did something. Every approval carries one, so a decision is tied to an Entra object id and tenant, never to a display name
/// (ADR 0017). The display name is for text only.
/// </summary>
/// <param name="ObjectId">The Entra object id (<c>oid</c> claim or <c>Activity.From.AadObjectId</c>).</param>
/// <param name="TenantId">The Entra tenant id (<c>tid</c> claim or the activity's tenant).</param>
/// <param name="DisplayName">The name to show. Never used to decide anything.</param>
/// <param name="UserPrincipalName">The user principal name, when the token or activity carries one.</param>
/// <param name="Source">Where the identity was established.</param>
/// <param name="AuthenticationMethod">How it was proven, for example <c>entra-bearer</c> or <c>bot-service-token</c>.</param>
public sealed record ActorIdentity(
    Guid ObjectId,
    Guid TenantId,
    string DisplayName,
    string? UserPrincipalName,
    ActorSource Source,
    string AuthenticationMethod)
{
    /// <summary>Creates an identity, refusing anything that does not name a real Entra principal.</summary>
    /// <param name="objectId">The Entra object id as text.</param>
    /// <param name="tenantId">The Entra tenant id as text.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="userPrincipalName">The user principal name, if any.</param>
    /// <param name="source">Where it was established.</param>
    /// <param name="authenticationMethod">How it was proven.</param>
    /// <returns>The identity, or a validation error.</returns>
    public static Result<ActorIdentity> Create(string? objectId, string? tenantId, string? displayName, string? userPrincipalName, ActorSource source, string? authenticationMethod)
    {
        if (!Guid.TryParse(objectId, out var oid) || oid == Guid.Empty)
        {
            return Result<ActorIdentity>.Failure(IdentityErrors.ObjectIdRequired);
        }

        if (!Guid.TryParse(tenantId, out var tid) || tid == Guid.Empty)
        {
            return Result<ActorIdentity>.Failure(IdentityErrors.TenantRequired);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result<ActorIdentity>.Failure(IdentityErrors.DisplayNameRequired);
        }

        if (string.IsNullOrWhiteSpace(authenticationMethod))
        {
            return Result<ActorIdentity>.Failure(IdentityErrors.AuthenticationMethodRequired);
        }

        var upn = string.IsNullOrWhiteSpace(userPrincipalName) ? null : userPrincipalName.Trim();
        return Result<ActorIdentity>.Success(new ActorIdentity(oid, tid, displayName.Trim(), upn, source, authenticationMethod.Trim()));
    }
}
