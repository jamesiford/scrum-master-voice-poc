using System.ComponentModel.DataAnnotations;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>Binds <c>Ado__*</c> configuration (SPEC 17).</summary>
public sealed class AdoOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Ado";

    /// <summary>Gets or sets the organisation URL, for example <c>https://dev.azure.com/fordjames-demos</c>.</summary>
    [Required]
    [Url]
    public string Org { get; set; } = string.Empty;

    /// <summary>Gets or sets <c>Entra</c> (DefaultAzureCredential) or <c>Pat</c>.</summary>
    [Required]
    [RegularExpression("^(Entra|Pat)$", ErrorMessage = "Ado:AuthMode must be Entra or Pat.")]
    public string AuthMode { get; set; } = "Entra";

    /// <summary>Gets or sets the personal access token. User secrets only; never committed.</summary>
    public string? Pat { get; set; }
}
