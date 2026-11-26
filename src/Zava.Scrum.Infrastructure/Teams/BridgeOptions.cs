namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Binds <c>Bridge__*</c> configuration (SPEC 17).</summary>
public sealed class BridgeOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Bridge";

    /// <summary>Gets or sets the real meeting adapter: Acs (default), Simulated or GraphMedia.</summary>
    public string Mode { get; set; } = "Acs";

    /// <summary>Gets or sets the HTTPS control address of the Windows media host.</summary>
    public string GraphMediaBaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the address the browser opens the Simulated Room on.</summary>
    public string RoomBaseUrl { get; set; } = "http://localhost:5090";

    /// <summary>Gets or sets the bridge address used for control calls; defaults to <see cref="RoomBaseUrl"/>.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Gets or sets the shared secret for control calls. User secrets or Container Apps secrets only.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
