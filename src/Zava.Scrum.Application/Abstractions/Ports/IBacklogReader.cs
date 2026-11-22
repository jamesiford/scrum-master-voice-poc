using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Reads the product backlog that is not in the current sprint. Kept apart from <see cref="IWorkItemReader"/> because only sprint planning needs it (SOLID: I).</summary>
public interface IBacklogReader
{
    /// <summary>Gets open stories and bugs outside the current sprint, in backlog order (top first).</summary>
    /// <param name="team">The team.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The items, or an <see cref="ErrorType.ExternalRejected"/> failure.</returns>
    Task<Result<IReadOnlyList<WorkItem>>> GetBacklogAsync(Team team, CancellationToken ct);
}
