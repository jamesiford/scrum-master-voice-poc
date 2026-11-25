using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Who is in which team, and in what role (ADR 0018). Keyed by Entra object id, never by name.</summary>
public interface ITeamMembershipRepository
{
    /// <summary>Lists a team's memberships.</summary>
    /// <param name="teamId">The team.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The memberships.</returns>
    Task<IReadOnlyList<TeamMembership>> ListByTeamAsync(string teamId, CancellationToken ct);

    /// <summary>Lists every team a person is in.</summary>
    /// <param name="objectId">The person's Entra object id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The memberships.</returns>
    Task<IReadOnlyList<TeamMembership>> ListByMemberAsync(Guid objectId, CancellationToken ct);

    /// <summary>Adds a membership or changes its role.</summary>
    /// <param name="membership">The membership.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task UpsertAsync(TeamMembership membership, CancellationToken ct);

    /// <summary>Removes a membership. Safe when it does not exist.</summary>
    /// <param name="teamId">The team.</param>
    /// <param name="objectId">The person.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task RemoveAsync(string teamId, Guid objectId, CancellationToken ct);
}
