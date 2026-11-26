namespace Zava.Scrum.Infrastructure.Delegation;

/// <summary>Binds <c>Delegation__*</c>: what the on-behalf-of exchange for Azure DevOps needs (ADR 0020). There is no secret here.</summary>
public sealed class DelegationOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Delegation";

    /// <summary>Gets or sets the Entra tenant. Falls back to <c>Identity:TenantId</c> and <c>Azure:TenantId</c>.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the portal app registration (client) id the person's token was issued for. Falls back to <c>Portal:ClientId</c>.</summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client id of the user-assigned managed identity that proves the app to Entra. The app registration trusts it through a
    /// federated credential, so no client secret exists. Falls back to <c>AZURE_CLIENT_ID</c>.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }
}
