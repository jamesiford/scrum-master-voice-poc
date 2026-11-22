using Mcm.Scrum.Domain.Planning;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Persistence for <see cref="PlanningPack"/>. Every implementation must pass the shared contract test (SOLID: L).</summary>
public interface IPlanningPackRepository
{
    /// <summary>Gets a pack by id.</summary>
    /// <param name="id">Pack id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The pack, or <see langword="null"/>.</returns>
    Task<PlanningPack?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Gets the team's most recently prepared pack.</summary>
    /// <param name="teamId">Team id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The pack, or <see langword="null"/> when none was prepared.</returns>
    Task<PlanningPack?> GetLatestAsync(string teamId, CancellationToken ct);

    /// <summary>Inserts or replaces a pack.</summary>
    /// <param name="pack">The pack.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(PlanningPack pack, CancellationToken ct);
}
