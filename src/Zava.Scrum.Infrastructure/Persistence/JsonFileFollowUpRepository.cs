using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Stores follow-ups in a JSON file.</summary>
/// <remarks>PROD: <c>CosmosFollowUpRepository</c> behind the same port.</remarks>
public sealed class JsonFileFollowUpRepository : IFollowUpRepository, IDisposable
{
    private readonly string _path;
    private readonly ICollectionStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFileFollowUpRepository"/> class.</summary>
    /// <param name="store">The collection store.</param>
    public JsonFileFollowUpRepository(ICollectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _path = "followups.json";
    }

    /// <inheritdoc />
    public async Task<FollowUp?> GetAsync(Guid id, CancellationToken ct) =>
        _store is IRecordCollectionStore records
            ? (await records.QueryAsync<FollowUpSnapshot>(_path, null, null, null, id.ToString(), ct)).Select(FollowUp.Restore).SingleOrDefault()
            : (await ReadAllAsync(ct)).FirstOrDefault(f => f.Id == id);

    /// <inheritdoc />
    public async Task<IReadOnlyList<FollowUp>> ListAsync(string teamId, FollowUpStatus? status, CancellationToken ct) =>
        _store is IRecordCollectionStore records
            ? (await records.QueryAsync<FollowUpSnapshot>(_path, teamId, null, status?.ToString(), null, ct)).Select(FollowUp.Restore).OrderByDescending(f => f.CreatedAt).ToList()
            : (await ReadAllAsync(ct)).Where(f => f.TeamId == teamId && (status is null || f.Status == status)).ToList();

    /// <inheritdoc />
    public async Task<IReadOnlyList<FollowUp>> ListBySessionAsync(Guid sessionId, CancellationToken ct) =>
        _store is IRecordCollectionStore records
            ? (await records.QueryAsync<FollowUpSnapshot>(_path, null, sessionId, null, null, ct)).Select(FollowUp.Restore).OrderByDescending(f => f.CreatedAt).ToList()
            : (await ReadAllAsync(ct)).Where(f => f.SessionId == sessionId).ToList();

    /// <inheritdoc />
    public async Task SaveAsync(FollowUp followUp, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(followUp);
        if (_store is IRecordCollectionStore items)
        {
            await items.SaveRecordAsync(_path, followUp.ToSnapshot(), false, ct);
            return;
        }
        await _gate.WaitAsync(ct);
        try
        {
            var records = await _store.ReadAsync<List<FollowUpSnapshot>>(_path, () => [], ct);
            records.RemoveAll(r => r.Id == followUp.Id);
            records.Add(followUp.ToSnapshot());
            await _store.WriteAsync(_path, records, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task<List<FollowUp>> ReadAllAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return (await _store.ReadAsync<List<FollowUpSnapshot>>(_path, () => [], ct))
                .Select(FollowUp.Restore)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }
}
