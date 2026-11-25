using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>
/// One Cosmos item per session in the <c>Sessions</c> container, partitioned by <c>/teamId</c> (ADR 0021). The snapshot is stored as JSON text in one
/// property, so the same converters apply as for files, and a session is read back by id with a single-partition query when the team is known.
/// </summary>
public sealed class CosmosSessionItemStore(Container container) : ISessionItemStore
{
    private static readonly JsonSerializerOptions Options = JsonFile.SerializerOptions;

    /// <inheritdoc />
    public async Task<StandupSessionSnapshot?> GetAsync(Guid id, CancellationToken ct)
    {
        var found = await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id.ToString()), null, ct);
        return found.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StandupSessionSnapshot>> ListAsync(string? teamId, CancellationToken ct) =>
        (await QueryAsync(new QueryDefinition("SELECT * FROM c"), teamId, ct)).OrderByDescending(s => s.CreatedAt).ToList();

    /// <inheritdoc />
    public async Task UpsertAsync(StandupSessionSnapshot snapshot, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await container.UpsertItemAsync(new Item(snapshot.Id.ToString(), snapshot.TeamId, JsonSerializer.Serialize(snapshot, Options)), new PartitionKey(snapshot.TeamId), cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task DeleteAllAsync(CancellationToken ct)
    {
        var keys = new List<(string Id, string TeamId)>();
        using (var feed = container.GetItemQueryIterator<Key>(new QueryDefinition("SELECT c.id, c.teamId FROM c")))
        {
            while (feed.HasMoreResults)
            {
                keys.AddRange((await feed.ReadNextAsync(ct)).Select(k => (k.Id, k.TeamId)));
            }
        }

        foreach (var (id, teamId) in keys)
        {
            await container.DeleteItemAsync<Item>(id, new PartitionKey(teamId), cancellationToken: ct);
        }
    }

    private async Task<List<StandupSessionSnapshot>> QueryAsync(QueryDefinition query, string? teamId, CancellationToken ct)
    {
        var found = new List<StandupSessionSnapshot>();
        var requestOptions = teamId is null ? null : new QueryRequestOptions { PartitionKey = new PartitionKey(teamId) };
        using var feed = container.GetItemQueryIterator<Item>(query, requestOptions: requestOptions);
        while (feed.HasMoreResults)
        {
            foreach (var item in await feed.ReadNextAsync(ct))
            {
                if (JsonSerializer.Deserialize<StandupSessionSnapshot>(item.Json, Options) is { } snapshot)
                {
                    found.Add(snapshot);
                }
            }
        }

        return found;
    }

    private sealed record Item(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("teamId")] string TeamId,
        [property: JsonPropertyName("json")] string Json);

    private sealed record Key(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("teamId")] string TeamId);
}
