using System.Text.Json;
using Microsoft.Extensions.Options;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>One JSON file per session under <c>Store:DataPath/sessions/&lt;team id&gt;/&lt;session id&gt;.json</c>.</summary>
public sealed class FileSessionItemStore(IOptions<StoreOptions> options) : ISessionItemStore
{
    private string Root => Path.Combine(options.Value.DataPath, "sessions");

    /// <inheritdoc />
    public async Task<StandupSessionSnapshot?> GetAsync(Guid id, CancellationToken ct)
    {
        if (!Directory.Exists(Root))
        {
            return null;
        }

        var file = Directory.EnumerateFiles(Root, id + ".json", SearchOption.AllDirectories).FirstOrDefault();
        return file is null ? null : await JsonFile.ReadAsync<StandupSessionSnapshot?>(file, () => null, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StandupSessionSnapshot>> ListAsync(string? teamId, CancellationToken ct)
    {
        var folder = teamId is null ? Root : Path.Combine(Root, Safe(teamId));
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var found = new List<StandupSessionSnapshot>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            if (await JsonFile.ReadAsync<StandupSessionSnapshot?>(file, () => null, ct) is { } snapshot)
            {
                found.Add(snapshot);
            }
        }

        return found.OrderByDescending(s => s.CreatedAt).ToList();
    }

    /// <inheritdoc />
    public Task UpsertAsync(StandupSessionSnapshot snapshot, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonFile.WriteAsync(Path.Combine(Root, Safe(snapshot.TeamId), snapshot.Id + ".json"), snapshot, ct);
    }

    /// <inheritdoc />
    public Task DeleteAllAsync(CancellationToken ct)
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }

        return Task.CompletedTask;
    }

    private static string Safe(string teamId) => string.Concat(teamId.Select(c => Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c));
}
