using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Where a team's roster comes from.</summary>
public interface IRosterSource
{
    /// <summary>Gets the roster for a team.</summary>
    /// <param name="team">The team.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Members, unsaved.</returns>
    Task<IReadOnlyList<Member>> GetRosterAsync(Team team, CancellationToken ct);
}
