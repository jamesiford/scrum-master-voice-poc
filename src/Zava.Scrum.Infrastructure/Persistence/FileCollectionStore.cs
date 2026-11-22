using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Collections as JSON files under <c>Store:DataPath</c>.</summary>
public sealed class FileCollectionStore(IOptions<StoreOptions> options) : ICollectionStore
{
    /// <inheritdoc />
    public Task<T> ReadAsync<T>(string name, Func<T> empty, CancellationToken ct) =>
        JsonFile.ReadAsync(PathOf(name), empty, ct);

    /// <inheritdoc />
    public Task WriteAsync<T>(string name, T value, CancellationToken ct) =>
        JsonFile.WriteAsync(PathOf(name), value, ct);

    /// <inheritdoc />
    public Task DeleteAsync(string name, CancellationToken ct)
    {
        var path = PathOf(name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string PathOf(string name) => Path.Combine(options.Value.DataPath, name);
}
