using Mcm.Scrum.Domain.Teams;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Persistence for <see cref="Team"/> settings.</summary>
public interface ITeamRepository
{
    /// <summary>Gets a team by id.</summary>
    /// <param name="id">Team id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team, or <see langword="null"/> when unknown.</returns>
    Task<Team?> GetAsync(string id, CancellationToken ct);

    /// <summary>Inserts or replaces a team.</summary>
    /// <param name="team">The team.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(Team team, CancellationToken ct);
}
