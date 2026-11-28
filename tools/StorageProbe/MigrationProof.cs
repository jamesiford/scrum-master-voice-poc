using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Zava.Scrum.Infrastructure.Persistence;
using Zava.Scrum.Domain.Standups;

namespace StorageProbe;

/// <summary>Checks authoritative exported payloads against migrated items and retained legacy/audit evidence.</summary>
internal static class MigrationProof
{
    /// <summary>Verifies record copies and immutable source data without changing application state.</summary>
    internal static async Task RunAsync(Database database, string exportPath, JsonSerializerOptions options)
    {
        var before = JsonSerializer.Deserialize<List<JsonElement>>(await File.ReadAllTextAsync(Path.Combine(exportPath, "collections.json")))!;
        var publication = before.Single(row => row.GetProperty("id").GetString() == "demo-publication");
        var state = JsonSerializer.Deserialize<DemoState>(publication.GetProperty("json").GetString()!, JsonFile.SerializerOptions)!;
        var current = await new CosmosDemoStateStore(database.GetContainer("collections")).ReadAsync(default);
        if (current.RecordStorageVersion != 1 || RecordCollectionStore.Names.Any(current.Collections.ContainsKey)
            || current.Sessions.Values.SelectMany(s => s).Any())
            throw new InvalidOperationException("Growing publication arrays remain or the migration marker is missing.");
        var store = new CosmosTeamRecordStore(database.GetContainer("TeamRecords"));
        var copied = new Dictionary<string, int>();
        var cards = Rows("cards.json", state, before);
        var unattributed = 0;
        foreach (var name in RecordCollectionStore.Names)
        {
            var rows = Rows(name, state, before);
            foreach (var row in rows)
            {
                var owned = name == "bot-activities.json" ? TeamRecordMigration.AttributeActivity(row, cards) : row;
                if (owned is null)
                {
                    unattributed++;
                    continue;
                }
                var expected = RecordCollectionStore.Describe(name, owned.Value, state.RecordGenerations.GetValueOrDefault(
                    (name == "cards.json" ? owned.Value.GetProperty("Message") : owned.Value).GetProperty("TeamId").GetString()!));
                var actual = await store.GetAsync(expected.TeamId, expected.Id, default);
                if (actual is null || JsonSerializer.Serialize(actual.Json) != JsonSerializer.Serialize(expected.Json))
                    throw new InvalidOperationException($"Migrated {name} payload does not match its authoritative export.");
            }
            copied[name] = rows.Count;
        }
        foreach (var operation in state.Operations.Values)
        {
            var expected = RecordCollectionStore.Describe("followups.json",
                JsonSerializer.SerializeToElement(operation.Approval, JsonFile.SerializerOptions), 0);
            if (await store.GetAsync(expected.TeamId, expected.Id, default) is not { } actual
                || JsonSerializer.Serialize(actual.Json) != JsonSerializer.Serialize(expected.Json))
                throw new InvalidOperationException("A retained reset follow-up is missing or changed.");
        }
        var rawSessions = JsonSerializer.Deserialize<List<JsonElement>>(await File.ReadAllTextAsync(Path.Combine(exportPath, "Sessions.json")))!;
        var sessionSnapshots = rawSessions.Select(row => JsonSerializer.Deserialize<StandupSessionSnapshot>(
            row.GetProperty("json").GetString()!, JsonFile.SerializerOptions)!).ToDictionary(s => s.Id);
        foreach (var snapshot in state.Sessions.Values.SelectMany(s => s)) sessionSnapshots[snapshot.Id] = snapshot;
        foreach (var snapshot in sessionSnapshots.Values)
        {
            var actual = await database.GetContainer("Sessions").ReadItemAsync<JsonElement>(snapshot.Id.ToString(), new PartitionKey(snapshot.TeamId));
            var restored = JsonSerializer.Deserialize<StandupSessionSnapshot>(actual.Resource.GetProperty("json").GetString()!, JsonFile.SerializerOptions);
            if (JsonSerializer.Serialize(restored, JsonFile.SerializerOptions) != JsonSerializer.Serialize(snapshot, JsonFile.SerializerOptions))
                throw new InvalidOperationException("An authoritative or retained session changed during migration.");
        }
        var retainedLegacy = 0;
        foreach (var row in before.Where(row => row.GetProperty("id").GetString() != "demo-publication"))
        {
            var id = row.GetProperty("id").GetString()!;
            var actual = await database.GetContainer("collections").ReadItemAsync<JsonElement>(id, new PartitionKey(id));
            if (actual.Resource.GetProperty("json").GetString() != row.GetProperty("json").GetString())
                throw new InvalidOperationException("A raw legacy source was changed or deleted.");
            retainedLegacy++;
        }
        var retainedApprovals = 0;
        var approvals = JsonSerializer.Deserialize<List<JsonElement>>(await File.ReadAllTextAsync(Path.Combine(exportPath, "ApprovalRecords.json")))!;
        foreach (var row in approvals)
        {
            var actual = await database.GetContainer("ApprovalRecords").ReadItemAsync<JsonElement>(
                row.GetProperty("id").GetString()!, new PartitionKey(row.GetProperty("correlationId").GetString()!));
            if (Payload(actual.Resource) != Payload(row)) throw new InvalidOperationException("An append-only approval record changed.");
            retainedApprovals++;
        }
        var result = new { copied, resetFollowUps = state.Operations.Count, retainedSessions = sessionSnapshots.Count, retainedLegacy, retainedApprovals, unattributed,
            marker = current.RecordStorageVersion, growingPublicationArrays = 0, verifiedAt = DateTimeOffset.UtcNow };
        await File.WriteAllTextAsync(Path.Combine(exportPath, "migration-proof.json"), JsonSerializer.Serialize(result, options));
        Console.WriteLine(JsonSerializer.Serialize(result, options));
    }

    private static string Payload(JsonElement row) =>
        JsonSerializer.Serialize(row.EnumerateObject().Where(p => !p.Name.StartsWith('_')).OrderBy(p => p.Name, StringComparer.Ordinal).ToDictionary(p => p.Name, p => p.Value));

    private static List<JsonElement> Rows(string name, DemoState state, List<JsonElement> before) =>
        state.Collections.TryGetValue(name, out var authoritative)
            ? authoritative.Deserialize<List<JsonElement>>(JsonFile.SerializerOptions) ?? []
            : before.Where(row => row.GetProperty("id").GetString() == name)
                .SelectMany(row => JsonSerializer.Deserialize<List<JsonElement>>(row.GetProperty("json").GetString()!, JsonFile.SerializerOptions) ?? []).ToList();
}
