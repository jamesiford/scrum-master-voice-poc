using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>
/// Stores one item per standup session, partitioned by team id, so a session never shares a document with another session or another team
/// and no collection grows towards the 2 MB item limit (ADR 0021). The JSON-file and Cosmos implementations behave the same.
/// </summary>
public interface ISessionItemStore
{
    /// <summary>Reads one session.</summary>
    /// <param name="id">The session id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The snapshot, or <see langword="null"/> when absent.</returns>
    Task<StandupSessionSnapshot?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Lists sessions, optionally for one team.</summary>
    /// <param name="teamId">The team, or <see langword="null"/> for every team.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The snapshots, newest first.</returns>
    Task<IReadOnlyList<StandupSessionSnapshot>> ListAsync(string? teamId, CancellationToken ct);

    /// <summary>Inserts or replaces one session.</summary>
    /// <param name="snapshot">The session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task UpsertAsync(StandupSessionSnapshot snapshot, CancellationToken ct);

    /// <summary>Deletes every session (the demo reset).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task DeleteAllAsync(CancellationToken ct);
}
