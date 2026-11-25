using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Stores team memberships in one collection (<c>memberships.json</c>), keyed by team and Entra object id.</summary>
/// <remarks>PROD: one Cosmos item per membership, partitioned by team.</remarks>
public sealed class JsonFileTeamMembershipRepository(ICollectionStore store) : ITeamMembershipRepository, IDisposable
{
    private const string Path = "memberships.json";
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamMembership>> ListByTeamAsync(string teamId, CancellationToken ct) =>
        (await ReadAsync(ct)).Where(m => m.TeamId == teamId).ToList();

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamMembership>> ListByMemberAsync(Guid objectId, CancellationToken ct) =>
        (await ReadAsync(ct)).Where(m => m.ObjectId == objectId).ToList();

    /// <inheritdoc />
    public async Task UpsertAsync(TeamMembership membership, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(membership);
        await Mutate(list =>
        {
            list.RemoveAll(m => m.TeamId == membership.TeamId && m.ObjectId == membership.ObjectId);
            list.Add(membership);
        }, ct);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string teamId, Guid objectId, CancellationToken ct) =>
        Mutate(list => list.RemoveAll(m => m.TeamId == teamId && m.ObjectId == objectId), ct);

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task<List<TeamMembership>> ReadAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await store.ReadAsync<List<TeamMembership>>(Path, () => [], ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task Mutate(Action<List<TeamMembership>> change, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var list = await store.ReadAsync<List<TeamMembership>>(Path, () => [], ct);
            change(list);
            await store.WriteAsync(Path, list, ct);
        }
        finally
        {
            _gate.Release();
        }
    }
}
