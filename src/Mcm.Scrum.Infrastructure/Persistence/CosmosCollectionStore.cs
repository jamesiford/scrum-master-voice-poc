using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Collections as items in one Cosmos container (<c>Store__Mode=Cosmos</c>). The JSON text is stored in a single property so the same converters apply as for files.</summary>
public sealed class CosmosCollectionStore(Container container) : ICollectionStore
{
    private static readonly JsonSerializerOptions Options = JsonFile.SerializerOptions;

    /// <inheritdoc />
    public async Task<T> ReadAsync<T>(string name, Func<T> empty, CancellationToken ct)
    {
        try
        {
            var response = await container.ReadItemAsync<Item>(name, new PartitionKey(name), cancellationToken: ct);
            return JsonSerializer.Deserialize<T>(response.Resource.Json, Options) ?? empty();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return empty();
        }
    }

    /// <inheritdoc />
    public async Task WriteAsync<T>(string name, T value, CancellationToken ct) =>
        await container.UpsertItemAsync(new Item(name, JsonSerializer.Serialize(value, Options)), new PartitionKey(name), cancellationToken: ct);

    /// <inheritdoc />
    public async Task DeleteAsync(string name, CancellationToken ct)
    {
        try
        {
            await container.DeleteItemAsync<Item>(name, new PartitionKey(name), cancellationToken: ct);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Already gone.
        }
    }

    private sealed record Item(
        [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("json")] string Json);
}