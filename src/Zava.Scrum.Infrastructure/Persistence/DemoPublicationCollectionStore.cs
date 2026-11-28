using System.Runtime.CompilerServices;
using System.Text.Json;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Uses atomic publication overrides, preserving untouched legacy collections and refusing stale reset writes.</summary>
internal sealed class DemoPublicationCollectionStore(ICollectionStore legacy, IDemoStateStore publication) : ICollectionStore
{
    private readonly ConditionalWeakTable<object, Epoch> _reads = new();

    /// <inheritdoc />
    public async Task<T> ReadAsync<T>(string name, Func<T> empty, CancellationToken ct)
    {
        var state = await publication.ReadAsync(ct);
        var value = state.Collections.TryGetValue(name, out var json)
            ? json.Deserialize<T>(JsonFile.SerializerOptions) ?? empty()
            : await legacy.ReadAsync(name, empty, ct);
        if (value is object reference)
        {
            _reads.AddOrUpdate(reference, new Epoch(state.Epoch));
        }

        return value;
    }

    /// <inheritdoc />
    public async Task WriteAsync<T>(string name, T value, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToElement(value, JsonFile.SerializerOptions);
        await publication.UpdateAsync(state =>
        {
            if (value is object reference && _reads.TryGetValue(reference, out var read) && read.Value != state.Epoch)
            {
                throw new DemoResetConflictException("A reset changed the persistence epoch. Re-read before writing.", "demo.reset_changed");
            }

            CheckGuard(state, name, json);
            state.Collections[name] = json;
            return true;
        }, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string name, CancellationToken ct)
    {
        await publication.UpdateAsync(state =>
        {
            CheckGuard(state, name, JsonSerializer.SerializeToElement(Array.Empty<object>()));
            // A tombstone overrides legacy files, which remain intact for recovery.
            state.Collections[name] = JsonSerializer.SerializeToElement<object?>(null);
            return true;
        }, ct);
    }

    private static void CheckGuard(DemoState state, string name, JsonElement replacement)
    {
        var pending = state.Operations.Values.Where(o => !o.Completed).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        if (name == "followups.json" && state.Collections.TryGetValue(name, out var before))
        {
            var allowed = pending.Select(o => o.Approval.Id).ToHashSet();
            var old = before.Deserialize<List<FollowUpSnapshot>>(JsonFile.SerializerOptions) ?? [];
            var next = replacement.Deserialize<List<FollowUpSnapshot>>(JsonFile.SerializerOptions) ?? [];
            var scope = pending.SelectMany(o => o.Scope).Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
            var unchanged = JsonSerializer.Serialize(old.Where(f => scope.Contains(f.TeamId) && !allowed.Contains(f.Id)).OrderBy(f => f.Id), JsonFile.SerializerOptions)
                == JsonSerializer.Serialize(next.Where(f => scope.Contains(f.TeamId) && !allowed.Contains(f.Id)).OrderBy(f => f.Id), JsonFile.SerializerOptions);
            if (unchanged && pending.All(operation => next.Count(f => f.Id == operation.Approval.Id) == 1
                && FollowUp.Restore(next.Single(f => f.Id == operation.Approval.Id)).ComputeProposalHash()
                    == FollowUp.Restore(operation.Approval).ComputeProposalHash()))
            {
                return;
            }
        }

        else
        {
            if (name == "teams.json" && replacement.ValueKind == JsonValueKind.Object)
            {
                var registry = replacement.Deserialize<Dictionary<string, Team>>(JsonFile.SerializerOptions) ?? [];
                if (pending.Any(operation => !DemoResetScope.CheckIsolation(operation.Target, operation.Scope, registry.Values.ToList()).IsSuccess))
                {
                    throw new DemoResetConflictException("A team cannot be introduced into the guarded demo's Azure DevOps area while a reset is staged.");
                }
            }

            var ids = pending.SelectMany(o => o.Scope).Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
            var old = state.Collections.GetValueOrDefault(name);
            if (GuardedRows(name, old, ids) == GuardedRows(name, replacement, ids))
            {
                return;
            }
        }

        throw new DemoResetConflictException("A staged reset guards only the selected demo team's data. Approve/apply its follow-up, then commit.");
    }

    private static string GuardedRows(string name, JsonElement json, HashSet<string> ids)
    {
        if (json.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        if (name is "teams.json" or "members.json")
        {
            return string.Join('|', json.EnumerateObject().Where(p => ids.Contains(p.Name)).OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => p.Name + ":" + p.Value.GetRawText()));
        }

        if (json.ValueKind == JsonValueKind.Array)
        {
            return string.Join('|', json.EnumerateArray().Where(row => Scoped(row, ids)).Select(row => row.GetRawText()).Order(StringComparer.Ordinal));
        }

        // Unowned collections cannot safely be attributed to a demo team, so they remain unchanged during staging.
        return json.GetRawText();
    }

    private static bool Scoped(JsonElement row, HashSet<string> ids) =>
        row.ValueKind == JsonValueKind.Object
        && (row.TryGetProperty("TeamId", out var team) && team.ValueKind == JsonValueKind.String && ids.Contains(team.GetString()!)
            || row.TryGetProperty("Message", out var message) && Scoped(message, ids));

    private sealed record Epoch(long Value);
}
