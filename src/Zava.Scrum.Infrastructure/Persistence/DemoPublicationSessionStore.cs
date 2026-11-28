using Zava.Scrum.Domain.Standups;
using Microsoft.Extensions.Options;
using Zava.Scrum.Infrastructure.Ado;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Switches demo sessions with the same publication document as roster and follow-ups; other teams stay in the legacy item store.</summary>
internal sealed class DemoPublicationSessionStore(ISessionItemStore legacy, IDemoStateStore publication, IOptions<DemoOptions> options) : ISessionItemStore
{
    /// <inheritdoc />
    public async Task<StandupSessionSnapshot?> GetAsync(Guid id, CancellationToken ct)
    {
        var state = await publication.ReadAsync(ct);
        if (state.RecordStorageVersion >= 1)
        {
            var snapshot = await legacy.GetAsync(id, ct);
            return snapshot is not null && Visible(state, snapshot) ? snapshot : null;
        }
        var own = state.Sessions.Values.SelectMany(s => s).FirstOrDefault(s => s.Id == id);
        if (own is not null)
        {
            return own;
        }

        var old = await legacy.GetAsync(id, ct);
        return old is not null && !state.Sessions.ContainsKey(old.TeamId) ? old : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StandupSessionSnapshot>> ListAsync(string? teamId, CancellationToken ct)
    {
        var state = await publication.ReadAsync(ct);
        var old = await legacy.ListAsync(teamId, ct);
        if (state.RecordStorageVersion >= 1)
            return old.Where(s => Visible(state, s)).OrderByDescending(s => s.CreatedAt).ToList();
        return old.Where(s => !state.Sessions.ContainsKey(s.TeamId))
            .Concat(state.Sessions.Where(p => teamId is null || p.Key == teamId).SelectMany(p => p.Value))
            .OrderByDescending(s => s.CreatedAt).ToList();
    }

    /// <inheritdoc />
    public async Task UpsertAsync(StandupSessionSnapshot snapshot, CancellationToken ct)
    {
        // Capture the epoch before reading legacy state, so a concurrent publication cannot resurrect deleted sessions.
        var before = await publication.ReadAsync(ct);
        if (before.Operations.Values.Any(o => !o.Completed && o.Scope.Any(t => t.Id == snapshot.TeamId)))
        {
            throw new DemoResetConflictException("This demo team has a staged reset; prepare/start/session writes are paused.");
        }

        CheckTombstone(before, snapshot);

        if (before.RecordStorageVersion >= 1)
        {
            await legacy.UpsertAsync(snapshot, ct);
            var after = await publication.ReadAsync(ct);
            if (after.Epoch != before.Epoch)
                throw new DemoResetConflictException("Reset changed during the session write. Re-read before retrying.", "demo.reset_changed");
            return;
        }

        if (!before.Sessions.ContainsKey(snapshot.TeamId) && !options.Value.ResetTeamIds.Contains(snapshot.TeamId, StringComparer.Ordinal))
        {
            await legacy.UpsertAsync(snapshot, ct);
            return;
        }

        var existing = before.Sessions.TryGetValue(snapshot.TeamId, out var own)
            ? own : (await legacy.ListAsync(snapshot.TeamId, ct)).ToList();
        await publication.UpdateAsync(state =>
        {
            if (state.Epoch != before.Epoch)
            {
                throw new DemoResetConflictException("A reset changed the session epoch. Re-read before writing.", "demo.reset_changed");
            }

            if (state.Operations.Values.Any(o => !o.Completed && o.Scope.Any(t => t.Id == snapshot.TeamId)))
            {
                throw new DemoResetConflictException("This demo team has a staged reset; prepare/start/session writes are paused.");
            }

            CheckTombstone(state, snapshot);

            var sessions = state.Sessions.TryGetValue(snapshot.TeamId, out var current) ? current : existing;
            state.Sessions[snapshot.TeamId] = [.. sessions.Where(s => s.Id != snapshot.Id), snapshot];
            return true;
        }, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAllAsync(CancellationToken ct)
    {
        var all = await ListAsync(null, ct);
        await publication.UpdateAsync(state =>
        {
            if (state.Operations.Values.Any(o => !o.Completed))
            {
                throw new DemoResetConflictException("Commit or abort the staged reset before another reset.");
            }

            foreach (var team in all.Select(s => s.TeamId).Distinct())
            {
                if (state.RecordStorageVersion >= 1)
                    state.SessionCutoffs[team] = all.Where(s => s.TeamId == team).Max(s => s.CreatedAt);
                else state.Sessions[team] = [];
            }

            state.Epoch++;
            return true;
        }, ct);
    }

    private static void CheckTombstone(DemoState state, StandupSessionSnapshot snapshot)
    {
        if (state.SessionTombstones.TryGetValue(snapshot.TeamId, out var deleted) && deleted.Contains(snapshot.Id)
            || state.SessionCutoffs.TryGetValue(snapshot.TeamId, out var cutoff) && snapshot.CreatedAt <= cutoff)
        {
            throw new DemoResetConflictException("This preparation/session belongs to a previous demo take. Prepare a new session after reset.", "demo.stale_session");
        }

    }

    private static bool Visible(DemoState state, StandupSessionSnapshot snapshot) =>
        !state.SessionCutoffs.TryGetValue(snapshot.TeamId, out var cutoff) || snapshot.CreatedAt > cutoff;
}
