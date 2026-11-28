using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Migrates authoritative whole-document overrides additively before requests, without deleting legacy evidence.</summary>
internal sealed class TeamRecordMigration(
    ICollectionStore legacy, ITeamRecordStore items, IDemoStateStore publication, ISessionItemStore sessions,
    ILogger<TeamRecordMigration> logger) : ITeamRecordMigration
{
    /// <summary>Runs a restart-safe create-only migration and removes growing arrays from the publication document.</summary>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var state = await publication.ReadAsync(ct);
        if (state.RecordStorageVersion >= 1) return 0;
        var count = 0;
        var approvals = new Dictionary<Guid, FollowUpSnapshot>();
        var cards = await legacy.ReadAsync<List<JsonElement>>("cards.json", () => [], ct);
        foreach (var name in RecordCollectionStore.Names)
        {
            var rows = await legacy.ReadAsync<List<JsonElement>>(name, () => [], ct);
            foreach (var row in rows)
            {
                var owned = name == "bot-activities.json" ? AttributeActivity(row, cards) : row;
                if (owned is null)
                {
                    // Unowned historic deliveries stay in the read-only legacy document; never guess a team.
                    logger.LogWarning("Legacy bot activity cannot be attributed to a card; retained in legacy storage without replay");
                    continue;
                }
                var record = RecordCollectionStore.Describe(name, owned.Value, 0);
                var pending = state.Operations.Values.FirstOrDefault(o => !o.Completed && o.Approval.Id.ToString() == record.Key);
                if (name == "followups.json" && pending is not null)
                {
                    approvals[pending.Id] = owned.Value.Deserialize<FollowUpSnapshot>(JsonFile.SerializerOptions)!;
                    continue;
                }
                if (await items.CreateAsync(record with { Generation = state.RecordGenerations.GetValueOrDefault(record.TeamId) }, ct)) count++;
            }
        }
        foreach (var snapshot in state.Sessions.Values.SelectMany(s => s))
            await sessions.UpsertAsync(snapshot, ct);
        foreach (var operation in state.Operations.Values)
        {
            var approval = approvals.GetValueOrDefault(operation.Id) ?? operation.Approval;
            var record = RecordCollectionStore.Describe("followups.json", JsonSerializer.SerializeToElement(approval, JsonFile.SerializerOptions), 0);
            if (await items.CreateAsync(record with { Generation = state.RecordGenerations.GetValueOrDefault(record.TeamId) }, ct)) count++;
        }
        await publication.UpdateAsync(current =>
        {
            if (current.Epoch != state.Epoch
                || JsonSerializer.Serialize(current.Collections, JsonFile.SerializerOptions) != JsonSerializer.Serialize(state.Collections, JsonFile.SerializerOptions)
                || JsonSerializer.Serialize(current.Sessions, JsonFile.SerializerOptions) != JsonSerializer.Serialize(state.Sessions, JsonFile.SerializerOptions)
                || JsonSerializer.Serialize(current.Operations, JsonFile.SerializerOptions) != JsonSerializer.Serialize(state.Operations, JsonFile.SerializerOptions))
                throw new InvalidOperationException("Legacy writers changed during storage migration. Quiesce the old revision before retrying; legacy data was not cleared.");
            foreach (var name in RecordCollectionStore.Names) current.Collections.Remove(name);
            foreach (var (id, approval) in approvals) current.Operations[id] = current.Operations[id] with { Approval = approval };
            current.Sessions.Clear();
            current.SessionTombstones.Clear();
            current.RecordStorageVersion = 1;
            return true;
        }, ct);
        logger.LogInformation("Migrated {Count} team-owned records; legacy sources retained and growing publication arrays removed", count);
        return count;
    }

    /// <summary>Attributes an old delivery only when one stored card proves its ownership.</summary>
    internal static JsonElement? AttributeActivity(JsonElement row, IReadOnlyList<JsonElement> cards)
    {
        if (row.TryGetProperty("TeamId", out var team) && team.ValueKind == JsonValueKind.String
            && row.TryGetProperty("SessionId", out var session) && session.ValueKind == JsonValueKind.String) return row;
        var key = row.GetProperty("DedupeKey").GetString();
        var matches = cards.Where(c => c.GetProperty("Message").GetProperty("DedupeKey").GetString() == key).ToList();
        if (matches.Count != 1) return null;
        var card = matches[0];
        var fields = row.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        fields["TeamId"] = card.GetProperty("Message").GetProperty("TeamId");
        fields["SessionId"] = card.GetProperty("Message").GetProperty("SessionId");
        fields["CreatedAt"] = card.GetProperty("SentAt");
        return JsonSerializer.SerializeToElement(fields);
    }
}
