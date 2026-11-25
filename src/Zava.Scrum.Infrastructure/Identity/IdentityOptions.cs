namespace Zava.Scrum.Infrastructure.Identity;

/// <summary>Binds <c>Identity__*</c>: who may act in this deployment.</summary>
public sealed class IdentityOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Identity";

    /// <summary>Gets or sets the Entra tenant the deployment belongs to. Falls back to <c>Azure:TenantId</c>, then <c>Bot:TenantId</c>.</summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the Entra object ids that count as administrators in Teams, where there is no token with app roles: a list, or a
    /// comma-separated string in the environment (<c>Identity__AdminObjectIds</c>). The deployment sets it to the administrator.
    /// </summary>
    public string[] AdminObjectIds { get; set; } = [];

    /// <summary>Resolves the deployment's tenant: <c>Identity:TenantId</c>, then <c>Azure:TenantId</c>, then <c>Bot:TenantId</c>. Blank values are skipped.</summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The tenant id text, or <see langword="null"/> when none is set.</returns>
    public static string? ResolveTenant(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new[] { configuration["Identity:TenantId"], configuration["Azure:TenantId"], configuration["Bot:TenantId"] }.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
}
