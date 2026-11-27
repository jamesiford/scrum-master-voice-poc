using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Publishes one Cosmos item with ETag CAS. Separate collection/session partitions are deliberately not a transaction.</summary>
internal sealed class CosmosDemoStateStore(Container container) : IDemoStateStore
{
    private const string Id = "demo-publication";

    /// <inheritdoc />
    public async Task<DemoState> ReadAsync(CancellationToken ct) => (await ReadItemAsync(ct)).State;

    /// <inheritdoc />
    public async Task<T> UpdateAsync<T>(Func<DemoState, T> update, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var (state, etag) = await ReadItemAsync(ct);
            var result = update(state);
            var item = new Item(Id, JsonSerializer.Serialize(state, JsonFile.SerializerOptions));
            // Refuse before publication rather than losing staged state at Cosmos's 2 MB item limit.
            if (JsonSerializer.SerializeToUtf8Bytes(item).Length > 1_800_000)
            {
                throw new InvalidOperationException("Demo publication exceeds its safe Cosmos item size. Nothing was published.");
            }

            try
            {
                if (etag is null)
                {
                    await container.CreateItemAsync(item, new PartitionKey(Id), cancellationToken: ct);
                }
                else
                {
                    await container.ReplaceItemAsync(item, Id, new PartitionKey(Id),
                        new ItemRequestOptions { IfMatchEtag = etag }, ct);
                }

                return result;
            }
            catch (CosmosException ex) when (ex.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
            {
                // Re-run only a deterministic local mutation, never an Azure DevOps write.
            }
        }

        throw new InvalidOperationException("Demo publication changed concurrently; retry the recoverable operation.");
    }

    private async Task<(DemoState State, string? Etag)> ReadItemAsync(CancellationToken ct)
    {
        try
        {
            var item = await container.ReadItemAsync<Item>(Id, new PartitionKey(Id), cancellationToken: ct);
            return (JsonSerializer.Deserialize<DemoState>(item.Resource.Json, JsonFile.SerializerOptions) ?? new DemoState(), item.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return (new DemoState(), null);
        }
    }

    private sealed record Item(
        [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("json")] string Json);
}
