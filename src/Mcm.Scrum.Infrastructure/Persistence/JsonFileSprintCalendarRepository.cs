using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Calendars;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Stores sprint calendars in a JSON collection (<c>sprint-calendars.json</c>).</summary>
/// <remarks>PROD: one item per calendar in Cosmos, partitioned by team, behind the same port.</remarks>
public sealed class JsonFileSprintCalendarRepository : ISprintCalendarRepository, IDisposable
{
    private const string Path = "sprint-calendars.json";
    private readonly ICollectionStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFileSprintCalendarRepository"/> class.</summary>
    /// <param name="store">The collection store.</param>
    public JsonFileSprintCalendarRepository(ICollectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public async Task<SprintCalendar?> GetAsync(Guid id, CancellationToken ct) =>
        (await ReadAllAsync(ct)).FirstOrDefault(c => c.Id == id);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SprintCalendar>> ListAsync(string teamId, CancellationToken ct) =>
        [.. (await ReadAllAsync(ct)).Where(c => c.Data.TeamId == teamId)];

    /// <inheritdoc />
    public async Task SaveAsync(SprintCalendar calendar, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        await _gate.WaitAsync(ct);
        try
        {
            var records = await _store.ReadAsync<List<SprintCalendarData>>(Path, () => [], ct);
            records.RemoveAll(r => r.Id == calendar.Id);
            records.Add(calendar.Data);
            await _store.WriteAsync(Path, records, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task<List<SprintCalendar>> ReadAllAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return (await _store.ReadAsync<List<SprintCalendarData>>(Path, () => [], ct))
                .Select(SprintCalendar.Restore)
                .OrderByDescending(c => c.Data.CreatedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }
}
