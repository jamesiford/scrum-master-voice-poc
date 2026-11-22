using System.ComponentModel.DataAnnotations;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Binds <c>Store__*</c> configuration (SPEC 17).</summary>
public sealed class StoreOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Store";

    /// <summary>Gets or sets the store implementation: <c>Json</c> or <c>Cosmos</c>.</summary>
    [Required]
    [RegularExpression("^(Json|Cosmos)$", ErrorMessage = "Store:Mode must be Json or Cosmos.")]
    public string Mode { get; set; } = "Json";

    /// <summary>Gets or sets the folder used by the JSON-file store.</summary>
    [Required]
    public string DataPath { get; set; } = "data";
}
