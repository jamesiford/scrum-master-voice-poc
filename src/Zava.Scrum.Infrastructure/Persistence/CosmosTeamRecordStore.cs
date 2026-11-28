using System.Net;
using Microsoft.Azure.Cosmos;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Real Cosmos per-item adapter; payload text is excluded from indexing (ADR 0040).</summary>
public sealed class CosmosTeamRecordStore(Container container) : ITeamRecordStore
{
    /// <inheritdoc />
    public async Task<TeamRecord?> GetAsync(string teamId, string id, CancellationToken ct)
    {
        try
        {
            return (await container.ReadItemAsync<TeamRecord>(id, new PartitionKey(teamId), cancellationToken: ct)).Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamRecord>> QueryAsync(string collection, string? teamId, string? sessionId, string? status, string? key, CancellationToken ct)
    {
        var filters = new List<(string Field, string Value)> { ("collection", collection) };
        if (teamId is not null) filters.Add(("teamId", teamId));
        if (sessionId is not null) filters.Add(("sessionId", sessionId));
        if (status is not null) filters.Add(("status", status));
        if (key is not null) filters.Add(("key", key));
        var query = new QueryDefinition("SELECT * FROM c WHERE " + string.Join(" AND ", filters.Select(f => $"c.{f.Field} = @{f.Field}")));
        foreach (var (field, value) in filters) query.WithParameter("@" + field, value);
        using var feed = container.GetItemQueryIterator<TeamRecord>(query,
            requestOptions: new QueryRequestOptions { PartitionKey = teamId is null ? null : new PartitionKey(teamId) });
        var records = new List<TeamRecord>();
        while (feed.HasMoreResults) records.AddRange(await feed.ReadNextAsync(ct));
        return records;
    }

    /// <inheritdoc />
    public async Task<bool> CreateAsync(TeamRecord record, CancellationToken ct)
    {
        try
        {
            await container.CreateItemAsync(record, new PartitionKey(record.TeamId), cancellationToken: ct);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(TeamRecord record, CancellationToken ct) =>
        await container.UpsertItemAsync(record, new PartitionKey(record.TeamId), cancellationToken: ct);
}
