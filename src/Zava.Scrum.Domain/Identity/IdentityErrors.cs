using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Identity;

/// <summary>Error codes for actor identity and approval authority.</summary>
public static class IdentityErrors
{
    /// <summary>Gets the error for an identity with no valid Entra object id.</summary>
    public static Error ObjectIdRequired { get; } = Error.Invalid("identity.object_id_required", "An Entra object id is required. A display name alone does not identify anyone.");

    /// <summary>Gets the error for an identity with no valid tenant id.</summary>
    public static Error TenantRequired { get; } = Error.Invalid("identity.tenant_required", "An Entra tenant id is required.");

    /// <summary>Gets the error for an identity with no display name.</summary>
    public static Error DisplayNameRequired { get; } = Error.Invalid("identity.display_name_required", "A display name is required for the audit text.");

    /// <summary>Gets the error for an identity with no authentication method.</summary>
    public static Error AuthenticationMethodRequired { get; } = Error.Invalid("identity.authentication_method_required", "How the identity was proven is required.");

    /// <summary>Gets the error for an identity from another tenant.</summary>
    public static Error WrongTenant { get; } = Error.Forbidden("identity.wrong_tenant", "The sign-in is from a different tenant.");

    /// <summary>Gets the error for an identity that arrived through a channel it does not belong to.</summary>
    public static Error WrongSource { get; } = Error.Forbidden("identity.wrong_source", "The identity was not established through this channel.");

    /// <summary>Gets the error for a portal caller without the approver role.</summary>
    public static Error ApproverRoleRequired { get; } = Error.Forbidden("identity.approver_role_required", $"Approving needs the {AppRoles.Approver} role.");

    /// <summary>Gets the error for a Teams user who is neither on the roster for this follow-up nor an administrator.</summary>
    public static Error NotOnRoster { get; } = Error.Forbidden("identity.not_on_roster", "Only the person this follow-up is for, or an administrator, can decide it. Your Entra identity does not match.");

    /// <summary>Gets the error for a legacy follow-up with no verified identity.</summary>
    public static Error LegacyUnverified { get; } = Error.NotApproved("identity.legacy_unverified", "This follow-up was approved before identities were recorded (unverified, legacy). It is never written.");
}
