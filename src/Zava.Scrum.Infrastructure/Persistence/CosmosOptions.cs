namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Binds <c>Cosmos__*</c> configuration (SPEC 17). Used when <c>Store__Mode=Cosmos</c>.</summary>
public sealed class CosmosOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Cosmos";

    /// <summary>Gets or sets the account endpoint, for example <c>https://cosmos-zava.documents.azure.com:443/</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the database name. Created by Bicep, because data-plane RBAC cannot create it.</summary>
    public string Database { get; set; } = "zavascrum";

    /// <summary>Gets or sets the container name, partitioned by <c>/id</c>.</summary>
    public string Container { get; set; } = "collections";

    /// <summary>Gets or sets the container with one item per standup session, partitioned by <c>/teamId</c> (ADR 0021). Created by Bicep.</summary>
    public string SessionsContainer { get; set; } = "Sessions";

    /// <summary>Gets or sets the append-only approval trail container, partitioned by <c>/correlationId</c>. Created by Bicep.</summary>
    public string ApprovalRecordsContainer { get; set; } = "ApprovalRecords";
}
