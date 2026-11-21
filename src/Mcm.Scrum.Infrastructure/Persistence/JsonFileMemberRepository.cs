using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Members;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Stores every team's roster in one JSON file keyed by team id.</summary>
/// <remarks>PROD: <c>CosmosMemberRepository</c> behind the same port.</remarks>
public sealed class JsonFileMemberRepository : IMemberRepository, IDisposable
{
    private readonly string _path;
    private readonly ICollectionStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFileMemberRepository"/> class.</summary>
    /// <param name="store">The collection store.</param>
    public JsonFileMemberRepository(ICollectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _path = "members.json";
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Member>> ListAsync(string teamId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var all = await _store.ReadAsync<Dictionary<string, List<Member>>>(_path, () => [], ct);
            return all.TryGetValue(teamId, out var members) ? members.OrderBy(m => m.SpeakingOrder).ToList() : [];
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ReplaceAsync(string teamId, IReadOnlyList<Member> members, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(members);
        await _gate.WaitAsync(ct);
        try
        {
            var all = await _store.ReadAsync<Dictionary<string, List<Member>>>(_path, () => [], ct);
            all[teamId] = [.. members];
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
