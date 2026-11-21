using Mcm.Scrum.Application.Abstractions.Ports;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Stores Teams conversation references in a JSON file.</summary>
/// <remarks>PROD: Cosmos, keyed by user or channel, behind the same port.</remarks>
public sealed class JsonFileConversationStore : IConversationStore, IDisposable
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFileConversationStore"/> class.</summary>
    /// <param name="options">Store options.</param>
    public JsonFileConversationStore(IOptions<StoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _path = Path.Combine(options.Value.DataPath, "conversations.json");
    }

    /// <inheritdoc />
    public async Task SaveAsync(string key, string referenceJson, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var all = await JsonFile.ReadAsync<Dictionary<string, string>>(_path, () => [], ct);
            all[key] = referenceJson;
            await JsonFile.WriteAsync(_path, all, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KeyValuePair<string, string>>> ListAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            return (await JsonFile.ReadAsync<Dictionary<string, string>>(_path, () => [], ct)).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
