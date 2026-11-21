using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Persistence for <see cref="StandupSession"/>. Every implementation must pass the shared contract test (SOLID: L).</summary>
public interface ISessionRepository
{
    /// <summary>Gets a session by id.</summary>
    /// <param name="id">Session id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The session, or <see langword="null"/> when absent.</returns>
    Task<StandupSession?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Lists sessions, newest first.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All sessions.</returns>
    Task<IReadOnlyList<StandupSession>> ListAsync(CancellationToken ct);

    /// <summary>Inserts or replaces a session.</summary>
    /// <param name="session">The session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(StandupSession session, CancellationToken ct);
}
