using Zava.Scrum.Infrastructure.Identity;

namespace Zava.Scrum.Api.Auth;

/// <summary>Binds <c>Portal__*</c>: how the API validates portal callers (SPEC 13).</summary>
public sealed class PortalAuthOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Portal";

    /// <summary>Gets or sets the client id of the portal's Entra app registration. Its tokens' audience is this id or <c>api://{id}</c>.</summary>
    public string? ClientId { get; set; }

    /// <summary>Gets or sets the tenant. Falls back to <see cref="IdentityOptions.TenantId"/>, then <c>Azure:TenantId</c>.</summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the object id the <see cref="PortalAuthMode.Development"/> principal carries. DEMO: local runs only, where there is
    /// no Entra sign-in; the Development mode is refused outside the Development environment.
    /// </summary>
    public string DevelopmentObjectId { get; set; } = "00000000-0000-0000-0000-0000000000d1";
}

/// <summary>How portal callers are authenticated.</summary>
public enum PortalAuthMode
{
    /// <summary>Entra bearer tokens, validated in the API: signature, issuer, audience, lifetime, tenant and oid.</summary>
    Entra,

    /// <summary>A fixed local principal holding every role, for running without an Entra app registration. Development environment only.</summary>
    Development,
}
