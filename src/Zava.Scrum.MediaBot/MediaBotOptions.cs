using System.ComponentModel.DataAnnotations;

namespace Zava.Scrum.MediaBot;

/// <summary>Windows host settings, provisioned by the media-host deployment hook (ADR 0023).</summary>
public sealed class MediaBotOptions
{
    /// <summary>Gets or sets the single deployment tenant.</summary>
    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the calling bot's application id.</summary>
    [Required]
    public string AppId { get; set; } = string.Empty;

    /// <summary>Gets or sets the LocalMachine/My TLS and Graph client certificate thumbprint.</summary>
    [Required]
    public string CertificateThumbprint { get; set; } = string.Empty;

    /// <summary>Gets or sets the public DNS name covered by the certificate.</summary>
    [Required]
    public string ServiceFqdn { get; set; } = string.Empty;

    /// <summary>Gets or sets the instance-level public IP address.</summary>
    [Required]
    public string PublicIp { get; set; } = string.Empty;

    /// <summary>Gets or sets the fixed public and local media TCP port.</summary>
    [Range(1024, 65535)]
    public int MediaPort { get; set; } = 8445;

    /// <summary>Gets or sets the authorised API origin; join requests cannot redirect tokens elsewhere.</summary>
    [Required, Url]
    public string ApiBase { get; set; } = string.Empty;

    /// <summary>Gets or sets the shared control secret, delivered securely by the deployment hook.</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the hard concurrent meeting cap, initially one until video is measured.</summary>
    [Range(1, 4)]
    public int MaxConcurrentMeetings { get; set; } = 1;
}
