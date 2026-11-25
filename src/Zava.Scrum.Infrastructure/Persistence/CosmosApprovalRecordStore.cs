using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>
/// The approval trail in its own Cosmos container (<c>ApprovalRecords</c>, partitioned by <c>/correlationId</c>). It only ever calls
/// <c>CreateItemAsync</c>, so an existing record can never be replaced, and the data-plane role may be limited to create and read.
/// </summary>
/// <remarks>PROD: a container with a Cosmos immutability policy (write-once) and change feed to a WORM archive.</remarks>
public sealed class CosmosApprovalRecordStore(Container container) : IApprovalRecordStore
{
    /// <inheritdoc />
    public async Task AppendAsync(ApprovalRecord record, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(record);
        await container.CreateItemAsync(new Item(record.Id.ToString(), record.CorrelationId, record.FollowUpId.ToString(), record.OccurredAt, record), new PartitionKey(record.CorrelationId), cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalRecord>> ListByFollowUpAsync(Guid followUpId, CancellationToken ct) =>
        await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE c.followUpId = @id ORDER BY c.occurredAt").WithParameter("@id", followUpId.ToString()), ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalRecord>> ListByCorrelationAsync(string correlationPrefix, CancellationToken ct) =>
        await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE STARTSWITH(c.correlationId, @p) ORDER BY c.occurredAt").WithParameter("@p", correlationPrefix), ct);

    private async Task<IReadOnlyList<ApprovalRecord>> QueryAsync(QueryDefinition query, CancellationToken ct)
    {
        var found = new List<ApprovalRecord>();
        using var feed = container.GetItemQueryIterator<Item>(query);
        while (feed.HasMoreResults)
        {
            found.AddRange((await feed.ReadNextAsync(ct)).Select(i => i.Record));
        }

        return found;
    }

    private sealed record Item(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("correlationId")] string CorrelationId,
        [property: JsonPropertyName("followUpId")] string FollowUpId,
        [property: JsonPropertyName("occurredAt")] DateTimeOffset OccurredAt,
        [property: JsonPropertyName("record")] ApprovalRecord Record);
}
