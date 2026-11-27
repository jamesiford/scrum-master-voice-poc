using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Publishes a flushed snapshot by same-volume atomic rename, under a cross-process exclusive file lock.</summary>
internal sealed class FileDemoStateStore(IOptions<StoreOptions> options) : IDemoStateStore
{
    private string Root => options.Value.DataPath;
    private string PathOfState => Path.Combine(Root, "demo-publication.json");

    /// <inheritdoc />
    public async Task<DemoState> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(PathOfState))
        {
            return new DemoState();
        }

        await using var stream = new FileStream(PathOfState, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        return await JsonSerializer.DeserializeAsync<DemoState>(stream, JsonFile.SerializerOptions, ct) ?? new DemoState();
    }

    /// <inheritdoc />
    public async Task<T> UpdateAsync<T>(Func<DemoState, T> update, CancellationToken ct)
    {
        Directory.CreateDirectory(Root);
        await using var lease = await LockAsync(ct);
        var state = await ReadAsync(ct);
        var result = update(state);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state, JsonFile.SerializerOptions);
        var stage = PathOfState + ".stage";
        try
        {
            await using (var stream = new FileStream(stage, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, ct);
                stream.Flush(flushToDisk: true);
            }

            ct.ThrowIfCancellationRequested();
            File.Move(stage, PathOfState, overwrite: true);
            return result;
        }
        finally
        {
            if (File.Exists(stage))
            {
                File.Delete(stage);
            }
        }
    }

    private async Task<FileStream> LockAsync(CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(Path.Combine(Root, "demo-publication.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(20, ct);
            }
        }
    }
}
