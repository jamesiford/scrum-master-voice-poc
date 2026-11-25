using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Infrastructure.Delegation;

/// <summary>Error codes for delegated Azure DevOps access.</summary>
public static class DelegationErrors
{
    /// <summary>Gets the error for a deployment with no delegation settings.</summary>
    public static Error NotConfigured { get; } = Error.ExternalRejected("delegation.not_configured", "Delegated Azure DevOps writes are not configured (tenant, portal client id and managed identity). Nothing was written.");

    /// <summary>Gets the error for an exchange Entra refused.</summary>
    public static Error ExchangeFailed { get; } = Error.ExternalRejected("delegation.exchange_failed", "Azure DevOps did not accept your sign-in for this write. Sign out and in again (consent or multi-factor may be needed), then apply. Nothing was written.");
}
