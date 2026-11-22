using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Schedules;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Stores schedules in a JSON file.</summary>
/// <remarks>PROD: Cosmos behind the same port.</remarks>
public sealed class JsonFileScheduleRepository : IScheduleRepository, IDisposable
{
    private readonly string _path;
    private readonly ICollectionStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFileScheduleRepository"/> class.</summary>
    /// <param name="store">The collection store.</param>
    public JsonFileScheduleRepository(ICollectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _path = "schedules.json";
    }

    /// <inheritdoc />
    public async Task<Schedule?> GetAsync(string teamId, CancellationToken ct) =>
        (await ListAsync(ct)).FirstOrDefault(s => s.TeamId == teamId);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Schedule>> ListAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return await _store.ReadAsync<List<Schedule>>(_path, () => [], ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(Schedule schedule, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        await _gate.WaitAsync(ct);
        try
        {
            var all = await _store.ReadAsync<List<Schedule>>(_path, () => [], ct);
            all.RemoveAll(s => s.TeamId == schedule.TeamId);
            all.Add(schedule);
            await _store.WriteAsync(_path, all, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
