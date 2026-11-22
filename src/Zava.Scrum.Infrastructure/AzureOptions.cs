namespace Mcm.Scrum.Infrastructure;

/// <summary>Binds <c>Azure__*</c>: pins which tenant <c>DefaultAzureCredential</c> signs in to, so a machine with several signed-in accounts picks the demo tenant.</summary>
public sealed class AzureOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Azure";

    /// <summary>Gets or sets the Entra tenant id. Empty lets the credential chain choose.</summary>
    public string? TenantId { get; set; }
}
