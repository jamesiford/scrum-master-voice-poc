using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Reads the current sprint from the work tracker (Azure DevOps). Read-only by design (writes use <c>IWorkItemWriter</c>, M3).</summary>
public interface IWorkItemReader
{
    /// <summary>Gets the team's current sprint with all non-removed items.</summary>
    /// <param name="team">The team, carrying its Azure DevOps location and thresholds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sprint, or an <see cref="ErrorType.ExternalRejected"/> / <see cref="ErrorType.NotFound"/> failure.</returns>
    Task<Result<SprintSnapshot>> GetCurrentSprintAsync(Team team, CancellationToken ct);
}
