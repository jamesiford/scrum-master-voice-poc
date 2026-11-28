using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Local per-item adapter, using hashed team directories and atomic JSON writes.</summary>
public sealed class FileTeamRecordStore(IOptions<StoreOptions> options) : ITeamRecordStore, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <inheritdoc />
    public Task<TeamRecord?> GetAsync(string teamId, string id, CancellationToken ct) =>
        WithGateAsync(() => JsonFile.ReadAsync<TeamRecord?>(PathOf(teamId, id), () => null, ct), ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamRecord>> QueryAsync(string collection, string? teamId, string? sessionId, string? status, string? key, CancellationToken ct)
        => await WithGateAsync(() => QueryFilesAsync(collection, teamId, sessionId, status, key, ct), ct);

    private async Task<IReadOnlyList<TeamRecord>> QueryFilesAsync(string collection, string? teamId, string? sessionId, string? status, string? key, CancellationToken ct)
    {
        var root = teamId is null ? Root : Path.GetDirectoryName(PathOf(teamId, "unused"))!;
        if (!Directory.Exists(root)) return [];
        var found = new List<TeamRecord>();
        foreach (var path in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
        {
            var record = await JsonFile.ReadAsync<TeamRecord?>(path, () => null, ct);
            if (record is not null && record.Collection == collection && (sessionId is null || record.SessionId == sessionId)
                && (status is null || record.Status == status) && (key is null || record.Key == key)) found.Add(record);
        }
        return found;
    }

    /// <inheritdoc />
    public async Task<bool> CreateAsync(TeamRecord record, CancellationToken ct)
        => await WithGateAsync(() => CreateFileAsync(record, ct), ct);

    private async Task<bool> CreateFileAsync(TeamRecord record, CancellationToken ct)
    {
        var path = PathOf(record.TeamId, record.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await JsonFile.WriteAsync(temporary, record, ct);
        try
        {
            File.Move(temporary, path, overwrite: false);
            return true;
        }
        catch (IOException) when (File.Exists(path))
        {
            return false;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(TeamRecord record, CancellationToken ct)
        => await WithGateAsync(async () =>
        {
            await UpsertFileAsync(record, ct);
            return true;
        }, ct);

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task UpsertFileAsync(TeamRecord record, CancellationToken ct)
    {
        var path = PathOf(record.TeamId, record.Id);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await JsonFile.WriteAsync(temporary, record, ct);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private string Root => Path.Combine(options.Value.DataPath, "team-records");
    private string PathOf(string teamId, string id) => Path.Combine(Root, TeamRecord.IdFor("team", teamId), id + ".json");

    private async Task<T> WithGateAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        // Windows opens and renames can briefly conflict even for create-only retries; file mode is single-process.
        await _gate.WaitAsync(ct);
        try
        {
            return await action();
        }
        finally
        {
            _gate.Release();
        }
    }
}
