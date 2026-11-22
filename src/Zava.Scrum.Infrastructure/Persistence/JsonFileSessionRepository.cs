using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Stores sessions in a single JSON file. Good enough for the PoC and the Simulated Room.</summary>
/// <remarks>PROD: replaced by <c>CosmosSessionRepository</c> (Store__Mode=Cosmos) behind the same port.</remarks>
public sealed class JsonFileSessionRepository : ISessionRepository, IDisposable
{
    private readonly string _path;
    private readonly ICollectionStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFileSessionRepository"/> class.</summary>
    /// <param name="store">The collection store.</param>
    public JsonFileSessionRepository(ICollectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _path = "sessions.json";
    }

    /// <inheritdoc />
    public async Task<StandupSession?> GetAsync(Guid id, CancellationToken ct) =>
        (await ListAsync(ct)).FirstOrDefault(s => s.Id == id);

    /// <inheritdoc />
    public async Task<IReadOnlyList<StandupSession>> ListAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return (await _store.ReadAsync<List<StandupSessionSnapshot>>(_path, () => [], ct))
                .Select(StandupSession.Restore)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(StandupSession session, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);
        await _gate.WaitAsync(ct);
        try
        {
            var records = await _store.ReadAsync<List<StandupSessionSnapshot>>(_path, () => [], ct);
            records.RemoveAll(r => r.Id == session.Id);
            records.Add(session.ToSnapshot());
            await _store.WriteAsync(_path, records, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

}
