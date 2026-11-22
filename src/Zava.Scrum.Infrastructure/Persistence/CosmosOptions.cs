namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Binds <c>Cosmos__*</c> configuration (SPEC 17). Used when <c>Store__Mode=Cosmos</c>.</summary>
public sealed class CosmosOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Cosmos";

    /// <summary>Gets or sets the account endpoint, for example <c>https://cosmos-mcm.documents.azure.com:443/</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the database name. Created by Bicep, because data-plane RBAC cannot create it.</summary>
    public string Database { get; set; } = "mcmscrum";

    /// <summary>Gets or sets the container name, partitioned by <c>/id</c>.</summary>
    public string Container { get; set; } = "collections";
}
