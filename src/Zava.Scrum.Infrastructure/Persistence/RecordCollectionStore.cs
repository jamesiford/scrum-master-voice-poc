using System.Text.Json;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Routes growing collections to items and applies the atomic reset's team generations.</summary>
internal sealed class RecordCollectionStore(ICollectionStore legacy, ITeamRecordStore items, IDemoStateStore publication) : IRecordCollectionStore
{
    /// <summary>Names of the migrated collections.</summary>
    internal static readonly string[] Names = ["followups.json", "cards.json", "bot-activities.json"];

    /// <inheritdoc />
    public async Task<T> ReadAsync<T>(string name, Func<T> empty, CancellationToken ct)
    {
        if (!Names.Contains(name, StringComparer.Ordinal)) return await legacy.ReadAsync(name, empty, ct);
        var rows = await QueryAsync<JsonElement>(name, null, null, null, null, ct);
        return JsonSerializer.SerializeToElement(rows).Deserialize<T>(JsonFile.SerializerOptions) ?? empty();
    }

    /// <inheritdoc />
    public Task WriteAsync<T>(string name, T value, CancellationToken ct) =>
        Names.Contains(name, StringComparer.Ordinal)
            ? throw new InvalidOperationException("Growing collections must be written one record at a time.")
            : legacy.WriteAsync(name, value, ct);

    /// <inheritdoc />
    public Task DeleteAsync(string name, CancellationToken ct) =>
        Names.Contains(name, StringComparer.Ordinal)
            ? throw new InvalidOperationException("Reset growing collections through their team generation.")
            : legacy.DeleteAsync(name, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> QueryAsync<T>(string name, string? teamId, Guid? sessionId, string? status, string? key, CancellationToken ct)
    {
        var state = await publication.ReadAsync(ct);
        IReadOnlyList<TeamRecord> found;
        if (teamId is not null && key is not null)
        {
            var record = await items.GetAsync(teamId, TeamRecord.IdFor(name, key), ct);
            found = record is not null && (sessionId is null || record.SessionId == sessionId.ToString())
                && (status is null || record.Status == status) ? [record] : [];
        }
        else
        {
            found = await items.QueryAsync(name, teamId, sessionId?.ToString(), status, key, ct);
        }
        var resetIds = state.Operations.Values.Select(o => o.Approval.Id.ToString()).ToHashSet(StringComparer.Ordinal);
        var rows = found.Where(r => r.Generation == state.RecordGenerations.GetValueOrDefault(r.TeamId)
            && (name != "followups.json" || !resetIds.Contains(r.Key))).Select(r => r.Json).ToList();
        if (name == "followups.json")
        {
            rows.AddRange(state.Operations.Values.Where(o => !o.Completed).Select(o => o.Approval)
                .Where(f => (teamId is null || f.TeamId == teamId) && (sessionId is null || f.SessionId == sessionId)
                    && (status is null || f.Status.ToString() == status) && (key is null || f.Id.ToString() == key))
                .Select(f => JsonSerializer.SerializeToElement(f, JsonFile.SerializerOptions)));
        }
        return rows.Select(row => row.Deserialize<T>(JsonFile.SerializerOptions)!).ToList();
    }

    /// <inheritdoc />
    public async Task SaveRecordAsync<T>(string name, T value, bool createOnly, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToElement(value, JsonFile.SerializerOptions);
        var state = await publication.ReadAsync(ct);
        var record = Describe(name, json, 0);
        var pending = state.Operations.Values.FirstOrDefault(o => !o.Completed && o.Scope.Any(t => t.Id == record.TeamId));
        if (pending is not null)
        {
            if (name != "followups.json" || record.Key != pending.Approval.Id.ToString())
                throw new DemoResetConflictException("This team has a staged reset; record writes are paused.");
            var snapshot = json.Deserialize<FollowUpSnapshot>(JsonFile.SerializerOptions)!;
            await publication.UpdateAsync(current =>
            {
                if (current.Epoch != state.Epoch || current.Operations[pending.Id].Completed
                    || FollowUp.Restore(snapshot).ComputeProposalHash() != FollowUp.Restore(pending.Approval).ComputeProposalHash())
                    throw new DemoResetConflictException("The staged reset changed. Re-read before writing.", "demo.reset_changed");
                current.Operations[pending.Id] = current.Operations[pending.Id] with { Approval = snapshot };
                return true;
            }, ct);
            await items.UpsertAsync(record with { Generation = state.RecordGenerations.GetValueOrDefault(record.TeamId) }, ct);
            return;
        }
        var generation = state.RecordGenerations.GetValueOrDefault(record.TeamId);
        var old = await items.GetAsync(record.TeamId, record.Id, ct);
        if (old is not null && old.Generation != generation
            || state.RecordCutoffs.TryGetValue(record.TeamId, out var cutoff) && record.CreatedAt <= cutoff)
            throw new DemoResetConflictException("This record belongs to a previous demo take.", "demo.stale_record");
        record = record with { Generation = generation };
        if (createOnly) await items.CreateAsync(record, ct);
        else await items.UpsertAsync(record, ct);
        var after = await publication.ReadAsync(ct);
        if (after.Epoch != state.Epoch)
            throw new DemoResetConflictException("Reset changed during the record write. Re-read before retrying.", "demo.reset_changed");
    }

    /// <summary>Extracts searchable ownership without altering the legacy payload.</summary>
    internal static TeamRecord Describe(string name, JsonElement row, long generation)
    {
        var source = name == "cards.json" ? row.GetProperty("Message") : row;
        var team = source.GetProperty("TeamId").GetString()!;
        if (string.IsNullOrWhiteSpace(team)) throw new InvalidDataException("A persisted record must have a team.");
        var session = source.GetProperty("SessionId").GetGuid().ToString();
        var key = name == "followups.json" ? row.GetProperty("Id").GetGuid().ToString() : source.GetProperty("DedupeKey").GetString()!;
        if (name == "bot-activities.json") key = JsonSerializer.Serialize(new[] { key, row.GetProperty("ConversationKey").GetString()! });
        var status = name == "followups.json"
            ? row.GetProperty("Status").Deserialize<FollowUpStatus>(JsonFile.SerializerOptions).ToString()
            : name == "cards.json" ? "Stored" : "Sent";
        var created = row.TryGetProperty(name == "cards.json" ? "SentAt" : "CreatedAt", out var timestamp)
            ? timestamp.GetDateTimeOffset() : DateTimeOffset.MinValue;
        return new TeamRecord(TeamRecord.IdFor(name, key), team, name, session, status, key, generation, created, row);
    }
}
