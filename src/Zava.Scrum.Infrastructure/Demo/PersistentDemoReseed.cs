using System.Text.Json;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Application.Features.Demo.OptInDemoScope;
using Zava.Scrum.Domain.Schedules;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Ado;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Demo;

/// <summary>Stages guarded reset inputs, then publishes all local state in one durable snapshot after approved ADO writes complete.</summary>
internal sealed class PersistentDemoReseed(
    ICollectionStore collections, IDemoStateStore publication, ITeamRepository teams, IRosterSource roster,
    ISessionItemStore sessions, DemoReseedPlan plan, IClock clock, IOptions<DemoOptions> options, DemoAdoResetGate gate) : IDemoReseed
{
    private static readonly string[] Lists = ["followups.json", "cards.json", "bot-activities.json", "planning-packs.json", "sprint-calendars.json", "schedules.json", "memberships.json"];
    private static readonly string[] FilmCast = ["daniel", "maya", "admin"];
    private static readonly string[] LegacyLists = ["followups.json", "cards.json", "bot-activities.json", "planning-packs.json", "sprint-calendars.json", "schedules.json"];

    /// <inheritdoc />
    public async Task<Result> OptInAsync(DemoResetScopeManifest manifest, ActorIdentity actor, CancellationToken ct)
    {
        var verified = await plan.ValidateManifestAsync(manifest, actor.TenantId, ct);
        if (!verified.IsSuccess)
        {
            return verified;
        }

        var existing = (await teams.ListAsync(ct)).ToDictionary(t => t.Id, StringComparer.Ordinal);
        return await publication.UpdateAsync(state =>
        {
            if (state.Operations.Values.Any(o => !o.Completed))
            {
                return Result.Failure(Error.Conflict("demo.reset_in_progress", "Resume or abort the staged operation before changing reset scope."));
            }

            var registry = Read(state, "teams.json", existing);
            foreach (var entry in manifest.Teams)
            {
                if (!registry.TryGetValue(entry.TeamId, out var team))
                {
                    var alreadyRemoved = state.Operations.Values.Any(o => o.Completed && !o.Aborted && o.TenantId == manifest.TenantId
                        && o.Scope.Any(t => t.Id == entry.TeamId && t.Ado == entry.ExpectedAdo));
                    if (alreadyRemoved)
                    {
                        continue;
                    }

                    return Result.Failure(Error.Conflict("demo.manifest_changed", "A reviewed registry entry is missing; no team is recreated or inferred."));
                }

                if (team.Ado != entry.ExpectedAdo || team.DemoTenantId is { } owner && owner != manifest.TenantId)
                {
                    return Result.Failure(Error.Forbidden("demo.manifest_changed", "A reviewed ADO fingerprint or tenant changed; no markers were applied."));
                }
            }

            var changed = false;
            foreach (var entry in manifest.Teams)
            {
                if (registry.TryGetValue(entry.TeamId, out var team) && (!team.IsDemo || team.DemoTenantId != manifest.TenantId))
                {
                    registry[entry.TeamId] = team with { IsDemo = true, DemoTenantId = manifest.TenantId };
                    changed = true;
                }
            }

            Put(state, "teams.json", registry);
            if (changed)
            {
                state.Epoch++;
            }

            return Result.Success();
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result> ClearAsync(Guid tenantId, CancellationToken ct)
    {
        var registrySnapshot = (await teams.ListAsync(ct)).ToDictionary(t => t.Id, StringComparer.Ordinal);
        var active = await sessions.ListAsync(null, ct);
        var snapshots = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var name in LegacyLists)
        {
            snapshots[name] = await collections.ReadAsync(name, () => JsonSerializer.SerializeToElement(Array.Empty<object>()), ct);
        }

        return await publication.UpdateAsync(state =>
        {
            if (state.Operations.Values.Any(o => !o.Completed))
            {
                return Result.Failure(Error.Conflict("demo.reset_in_progress", "The legacy reset cannot bypass an in-flight re-seed."));
            }

            var registry = Read(state, "teams.json", registrySnapshot);
            var scope = registry.Values.Where(t => options.Value.ResetTeamIds.Contains(t.Id, StringComparer.Ordinal)).ToList();
            var checkedScope = DemoResetScope.Check(options.Value.ResetTenantId, tenantId, options.Value.ResetTeamIds, scope);
            if (!checkedScope.IsSuccess)
            {
                return checkedScope;
            }

            var ids = scope.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
            if (active.Any(s => ids.Contains(s.TeamId) && Running(s))
                || state.Sessions.Values.SelectMany(s => s).Any(s => ids.Contains(s.TeamId) && Running(s)))
            {
                return Result.Failure(Error.Conflict("demo.session_running", "End the selected demo team's running take before reset."));
            }

            foreach (var pair in snapshots)
            {
                state.Collections.TryAdd(pair.Key, pair.Value);
            }

            foreach (var name in LegacyLists)
            {
                var list = Read<List<JsonElement>>(state, name, []);
                Put(state, name, name == "schedules.json"
                    ? list.Select(row => BelongsTo(row, ids, tenantId)
                        ? JsonSerializer.SerializeToElement(row.Deserialize<Schedule>(JsonFile.SerializerOptions)! with
                        { PreparedFor = null, PreparedSessionId = null, StartedFor = null }, JsonFile.SerializerOptions) : row).ToList()
                    : list.Where(row => !BelongsTo(row, ids, tenantId)).ToList());
            }

            HideSessions(state, ids, active.Where(s => ids.Contains(s.TeamId)).Select(s => s.Id), clock.UtcNow);

            state.Epoch++;
            return Result.Success();
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> StageAsync(string teamId, ActorIdentity actor, CancellationToken ct)
    {
        var scope = await ScopeAsync(teamId, actor, ct);
        if (!scope.IsSuccess)
        {
            return Result<Guid>.Failure(scope.Error!);
        }

        var state = await publication.ReadAsync(ct);
        var existing = state.Operations.Values.FirstOrDefault(o => !o.Completed);
        if (existing is not null)
        {
            return existing.Target.Id == teamId && existing.TenantId == actor.TenantId
                ? Result<Guid>.Success(existing.Id)
                : Result<Guid>.Failure(Error.Conflict("demo.reset_in_progress", "Another guarded reset must be completed first."));
        }

        var target = scope.Value.Single(t => t.Id == teamId);
        var id = Guid.NewGuid();
        var changes = await plan.BuildAsync(target, id, clock.UtcNow, actor.UserPrincipalName, ct);
        if (!changes.IsSuccess)
        {
            return Result<Guid>.Failure(changes.Error!);
        }

        var members = await roster.GetRosterAsync(target, ct);
        if (members.Count != 3 || !members.Select(m => m.Key).SequenceEqual(FilmCast))
        {
            return Result<Guid>.Failure(Error.Conflict("demo.cast_refused", "The film seed must have Daniel, Maya and Admin in speaking order."));
        }

        var active = await sessions.ListAsync(null, ct);
        if (active.Any(s => scope.Value.Any(t => t.Id == s.TeamId) && Running(s)))
        {
            return Result<Guid>.Failure(Error.Conflict("demo.session_running", "End the demo team's live session before re-seeding."));
        }

        // Capture every touched collection before acquiring the guard. A concurrent reset epoch is refused below.
        var snapshots = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var name in Lists)
        {
            snapshots[name] = await collections.ReadAsync(name, () => JsonSerializer.SerializeToElement(Array.Empty<object>()), ct);
        }

        foreach (var name in new[] { "teams.json", "members.json" })
        {
            snapshots[name] = await collections.ReadAsync(name, () => JsonSerializer.SerializeToElement(new Dictionary<string, object>()), ct);
        }

        var followup = FollowUp.Create(Guid.NewGuid(), teamId, id, null, FollowUpKind.Followup,
            "Approve restoring all existing tagged seed items, then commit the guarded film reset.",
            changes.Value, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime), clock.UtcNow, forScrumMaster: true);
        return await gate.RunAsync(() => publication.UpdateAsync(current =>
        {
            if (current.Epoch != state.Epoch || current.Operations.Values.Any(o => !o.Completed))
            {
                return Result<Guid>.Failure(Error.Conflict("demo.reset_changed", "Reset state changed while staging; retry."));
            }

            foreach (var pair in snapshots)
            {
                // Writes since the read are already canonical; never overwrite those with the staged copy.
                current.Collections.TryAdd(pair.Key, pair.Value);
            }

            var records = Read<List<FollowUpSnapshot>>(current, "followups.json", []);
            var registry = Read<Dictionary<string, Team>>(current, "teams.json", []);
            if (scope.Value.Any(team => registry.TryGetValue(team.Id, out var latest)
                    && (latest.IsDemo != team.IsDemo || latest.DemoTenantId != team.DemoTenantId || latest.Ado != team.Ado))
                || !DemoResetScope.CheckIsolation(target, scope.Value, registry.Values.ToList()).IsSuccess
                || current.Sessions.Values.SelectMany(s => s).Any(s => scope.Value.Any(t => t.Id == s.TeamId) && Running(s)))
            {
                return Result<Guid>.Failure(Error.Conflict("demo.reset_changed", "Demo scope or live sessions changed while staging; retry."));
            }

            records.Add(followup.ToSnapshot());
            Put(current, "followups.json", records);
            foreach (var team in scope.Value)
            {
                // The scheduler stops starting demo sessions while approval/ADO replay is pending.
                registry[team.Id] = team with { Status = TeamStatus.Paused };
            }

            Put(current, "teams.json", registry);
            var sessionIds = active.Where(s => scope.Value.Any(t => t.Id == s.TeamId)).Select(s => s.Id)
                .Concat(current.Sessions.Where(pair => scope.Value.Any(t => t.Id == pair.Key)).SelectMany(pair => pair.Value).Select(s => s.Id))
                .Distinct().ToList();
            current.Operations[id] = new DemoReseedOperation(id, actor.TenantId, target, scope.Value, members, followup.ToSnapshot(), SessionIds: sessionIds);
            current.Epoch++;
            return Result<Guid>.Success(id);
        }, ct), ct);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CommitAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
    {
        return await publication.UpdateAsync(state =>
        {
            var found = Find(state, teamId, operationId, actor);
            if (!found.IsSuccess)
            {
                return Result<Guid>.Failure(found.Error!);
            }

            var operation = found.Value;
            if (operation.Aborted)
            {
                return Result<Guid>.Failure(Error.Conflict("demo.reset_aborted", "This reset was aborted; stage a new operation."));
            }

            if (operation.Completed)
            {
                return Result<Guid>.Success(operationId);
            }

            var registry = Read<Dictionary<string, Team>>(state, "teams.json", []);
            var scope = operation.Scope.Select(t => registry.GetValueOrDefault(t.Id)).ToList();
            if (scope.Any(t => t is null))
            {
                return Result<Guid>.Failure(Error.Forbidden("demo.reset_scope_refused", "A scoped demo team disappeared; nothing was published."));
            }

            if (operation.Scope.Any(original => registry[original.Id].Ado != original.Ado))
            {
                return Result<Guid>.Failure(Error.Conflict("demo.reset_changed", "A verified demo connection changed after staging. No state was published."));
            }

            var check = DemoResetScope.Check(options.Value.ResetTenantId, actor.TenantId, options.Value.ResetTeamIds, scope.Select(t => t!).ToList());
            if (!check.IsSuccess)
            {
                return Result<Guid>.Failure(check.Error!);
            }

            check = DemoResetScope.CheckIsolation(operation.Target, operation.Scope, registry.Values.ToList());
            if (!check.IsSuccess)
            {
                return Result<Guid>.Failure(check.Error!);
            }

            if (state.Sessions.Values.SelectMany(s => s).Any(s => operation.Scope.Any(t => t.Id == s.TeamId) && Running(s)))
            {
                return Result<Guid>.Failure(Error.Conflict("demo.session_running", "A selected demo team has a running take; no state was published."));
            }

            var followups = Read<List<FollowUpSnapshot>>(state, "followups.json", []);
            var approval = followups.SingleOrDefault(f => f.Id == operation.Approval.Id);
            if (approval is null || approval.Status != FollowUpStatus.Applied
                || FollowUp.Restore(approval).ComputeProposalHash() != FollowUp.Restore(operation.Approval).ComputeProposalHash())
            {
                return Result<Guid>.Failure(Error.Conflict("demo.awaiting_apply", "Approve and successfully apply the exact staged reset follow-up before publication."));
            }

            var ids = operation.Scope.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var name in Lists)
            {
                var list = Read<List<JsonElement>>(state, name, []);
                Put(state, name, list.Where(row => !BelongsTo(row, ids, actor.TenantId)).ToList());
            }

            var rosters = Read<Dictionary<string, JsonElement>>(state, "members.json", []);
            foreach (var id in ids)
            {
                rosters.Remove(id);
                registry.Remove(id);
            }

            HideSessions(state, ids, operation.SessionIds ?? [], clock.UtcNow);

            registry[teamId] = operation.Target with { Status = TeamStatus.Active, RecapChannelKey = null, TeamsBindings = [] };
            rosters[teamId] = JsonSerializer.SerializeToElement(operation.Roster, JsonFile.SerializerOptions);
            Put(state, "teams.json", registry);
            Put(state, "members.json", rosters);
            state.Operations[operationId] = operation with { Approval = approval, Completed = true };
            state.Epoch++;
            return Result<Guid>.Success(operationId);
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<DemoReseedDto>> GetAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
    {
        var found = Find(await publication.ReadAsync(ct), teamId, operationId, actor);
        return found.IsSuccess ? Result<DemoReseedDto>.Success(found.Value.ToDto()) : Result<DemoReseedDto>.Failure(found.Error!);
    }

    /// <inheritdoc />
    public async Task<Result<string>> LocateTeamAsync(Guid operationId, CancellationToken ct)
    {
        var state = await publication.ReadAsync(ct);
        return state.Operations.TryGetValue(operationId, out var operation)
            && operation.TenantId == options.Value.ResetTenantId && operation.TenantId != Guid.Empty
            && options.Value.ResetTeamIds.Contains(operation.Target.Id, StringComparer.Ordinal)
                ? Result<string>.Success(operation.Target.Id)
                : Result<string>.Failure(Error.NotFound("demo.operation_not_found", "The reset operation was not found in the configured demo tenant."));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> AbortAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct)
    {
        return await publication.UpdateAsync(state =>
        {
            var found = Find(state, teamId, operationId, actor);
            if (!found.IsSuccess)
            {
                return Result<Guid>.Failure(found.Error!);
            }

            var operation = found.Value;
            if (operation.Aborted)
            {
                return Result<Guid>.Success(operationId);
            }

            var followups = Read<List<FollowUpSnapshot>>(state, "followups.json", []);
            var followup = followups.SingleOrDefault(f => f.Id == operation.Approval.Id);
            if (operation.Completed || followup is null || followup.Status is FollowUpStatus.Approved or FollowUpStatus.Applied)
            {
                return Result<Guid>.Failure(Error.Conflict("demo.reset_must_resume", "An approved reset may have written ADO changes. Resume apply/commit; it cannot be aborted."));
            }

            // Removing the undecided proposal and releasing paused statuses is one local publication.
            var registry = Read<Dictionary<string, Team>>(state, "teams.json", []);
            foreach (var team in operation.Scope)
            {
                registry[team.Id] = team;
            }

            Put(state, "teams.json", registry);
            Put(state, "followups.json", followups.Where(f => f.Id != operation.Approval.Id).ToList());
            state.Operations[operationId] = operation with { Approval = followup, Completed = true, Aborted = true };
            state.Epoch++;
            return Result<Guid>.Success(operationId);
        }, ct);
    }

    private async Task<Result<IReadOnlyList<Team>>> ScopeAsync(string teamId, ActorIdentity actor, CancellationToken ct)
    {
        var configured = options.Value.ResetTeamIds;
        if (!configured.Contains(teamId, StringComparer.Ordinal))
        {
            return Result<IReadOnlyList<Team>>.Failure(Error.Forbidden("demo.reset_scope_refused", "The requested team is not explicitly allowlisted."));
        }

        var all = await teams.ListAsync(ct);
        var scope = all.Where(t => configured.Contains(t.Id, StringComparer.Ordinal)).ToList();
        if (scope.All(t => t.Id != teamId))
        {
            return Result<IReadOnlyList<Team>>.Failure(Error.NotFound("team.not_found", "The requested demo team was not found."));
        }

        var check = DemoResetScope.Check(options.Value.ResetTenantId, actor.TenantId, configured, scope);
        if (check.IsSuccess)
        {
            check = DemoResetScope.CheckIsolation(scope.Single(t => t.Id == teamId), scope, all);
        }

        return check.IsSuccess ? Result<IReadOnlyList<Team>>.Success(scope) : Result<IReadOnlyList<Team>>.Failure(check.Error!);
    }

    private Result<DemoReseedOperation> Find(DemoState state, string teamId, Guid id, ActorIdentity actor) =>
        state.Operations.TryGetValue(id, out var operation) && operation.Target.Id == teamId
        && operation.TenantId == actor.TenantId && actor.TenantId == options.Value.ResetTenantId
        && options.Value.ResetTeamIds.Contains(teamId, StringComparer.Ordinal)
            ? Result<DemoReseedOperation>.Success(operation)
            : Result<DemoReseedOperation>.Failure(Error.NotFound("demo.operation_not_found", "The reset operation was not found in this demo tenant/team."));

    private static T Read<T>(DemoState state, string name, T empty) =>
        state.Collections.TryGetValue(name, out var json) ? json.Deserialize<T>(JsonFile.SerializerOptions) ?? empty : empty;
    private static void Put<T>(DemoState state, string name, T value) =>
        state.Collections[name] = JsonSerializer.SerializeToElement(value, JsonFile.SerializerOptions);

    private static bool Running(StandupSessionSnapshot session) =>
        session.Status == StandupStatus.Live || session.StartedAt is not null && session.EndedAt is null;

    private static void HideSessions(DemoState state, HashSet<string> ids, IEnumerable<Guid> oldIds, DateTimeOffset cutoff)
    {
        var allIds = oldIds.Concat(state.Sessions.Where(pair => ids.Contains(pair.Key)).SelectMany(pair => pair.Value).Select(s => s.Id)).ToHashSet();
        foreach (var id in ids)
        {
            var tombstones = state.SessionTombstones.GetValueOrDefault(id) ?? [];
            tombstones.UnionWith(allIds);
            state.SessionTombstones[id] = tombstones;
            state.SessionCutoffs[id] = state.SessionCutoffs.TryGetValue(id, out var old) && old > cutoff ? old : cutoff;
            state.Sessions[id] = [];
        }
    }

    private static bool BelongsTo(JsonElement row, HashSet<string> ids, Guid tenantId)
    {
        if (row.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (row.TryGetProperty("TenantId", out var tenant)
            && (tenant.ValueKind != JsonValueKind.String || !Guid.TryParse(tenant.GetString(), out var id) || id != tenantId))
        {
            return false;
        }

        if (row.TryGetProperty("TeamId", out var team) && team.ValueKind == JsonValueKind.String && ids.Contains(team.GetString()!))
        {
            return true;
        }

        return row.TryGetProperty("Message", out var message) && BelongsTo(message, ids, tenantId);
    }
}
