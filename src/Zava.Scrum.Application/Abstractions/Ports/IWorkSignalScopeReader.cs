using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Resolves team-owned sources from code and delegated board access, never from model text.</summary>
public interface IWorkSignalScopeReader
{
    /// <summary>Resolves an exact channel, stored meetings, current board and verified roster for this owner.</summary>
    /// <param name="team">The selected team.</param>
    /// <param name="owner">The owner's validated assertion, not the service identity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bounded scope, or an explicit prerequisite/access failure.</returns>
    Task<Result<WorkSignalScope>> GetAsync(Team team, UserAssertion owner, CancellationToken ct);
}
