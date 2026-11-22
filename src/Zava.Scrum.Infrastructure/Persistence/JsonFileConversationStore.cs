using Mcm.Scrum.Application.Abstractions.Ports;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Stores Teams conversation references in a JSON file.</summary>
/// <remarks>PROD: Cosmos, keyed by user or channel, behind the same port.</remarks>
public sealed class JsonFileConversationStore : IConversationStore, IDisposable
{
    private readonly string _path;
    private readonly ICollectionStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFileConversationStore"/> class.</summary>
    /// <param name="store">The collection store.</param>
    public JsonFileConversationStore(ICollectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _path = "conversations.json";
    }

    /// <inheritdoc />
    public async Task SaveAsync(string key, string referenceJson, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var all = await _store.ReadAsync<Dictionary<string, string>>(_path, () => [], ct);
            all[key] = referenceJson;
            await _store.WriteAsync(_path, all, ct);
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
            return (await _store.ReadAsync<Dictionary<string, string>>(_path, () => [], ct)).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
