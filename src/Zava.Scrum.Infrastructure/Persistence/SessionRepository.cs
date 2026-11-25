using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>
/// Sessions over <see cref="ISessionItemStore"/>: one item per session, partitioned by team (ADR 0021). It replaces the single
/// <c>sessions.json</c> document that passed the Cosmos 2 MB item limit after a few dozen standups.
/// </summary>
public sealed class SessionRepository(ISessionItemStore store) : ISessionRepository
{
    /// <inheritdoc />
    public async Task<StandupSession?> GetAsync(Guid id, CancellationToken ct) =>
        await store.GetAsync(id, ct) is { } snapshot ? StandupSession.Restore(snapshot) : null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<StandupSession>> ListAsync(CancellationToken ct) =>
        (await store.ListAsync(null, ct)).Select(StandupSession.Restore).OrderByDescending(s => s.CreatedAt).ToList();

    /// <inheritdoc />
    public Task SaveAsync(StandupSession session, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);
        return store.UpsertAsync(session.ToSnapshot(), ct);
    }
}
