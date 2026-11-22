using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Planning;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Stores planning packs in a JSON collection (<c>planning-packs.json</c>).</summary>
/// <remarks>PROD: one item per pack in Cosmos, partitioned by team, behind the same port.</remarks>
public sealed class JsonFilePlanningPackRepository : IPlanningPackRepository, IDisposable
{
    private const string Path = "planning-packs.json";
    private readonly ICollectionStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFilePlanningPackRepository"/> class.</summary>
    /// <param name="store">The collection store.</param>
    public JsonFilePlanningPackRepository(ICollectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public async Task<PlanningPack?> GetAsync(Guid id, CancellationToken ct) =>
        (await ReadAllAsync(ct)).FirstOrDefault(p => p.Id == id);

    /// <inheritdoc />
    public async Task<PlanningPack?> GetLatestAsync(string teamId, CancellationToken ct) =>
        (await ReadAllAsync(ct)).FirstOrDefault(p => p.Data.TeamId == teamId);

    /// <inheritdoc />
    public async Task SaveAsync(PlanningPack pack, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pack);
        await _gate.WaitAsync(ct);
        try
        {
            var records = await _store.ReadAsync<List<PlanningPackData>>(Path, () => [], ct);
            records.RemoveAll(r => r.Id == pack.Id);
            records.Add(pack.Data);
            await _store.WriteAsync(Path, records, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task<List<PlanningPack>> ReadAllAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return (await _store.ReadAsync<List<PlanningPackData>>(Path, () => [], ct))
                .Select(PlanningPack.Restore)
                .OrderByDescending(p => p.Data.CreatedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }
}
