using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Reads the current sprint with one asker's delegated Azure DevOps access, never the agent credential.</summary>
public interface IDelegatedWorkItemReader
{
    /// <summary>Reads only the team's area and sprint as the signed-in asker.</summary>
    /// <param name="team">The membership-resolved team.</param>
    /// <param name="access">The asker's uncached on-behalf-of token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Readable sprint data, or an explicit permission or external failure.</returns>
    Task<Result<SprintSnapshot>> GetCurrentSprintAsync(Team team, DelegatedAdoAccess access, CancellationToken ct);
}
