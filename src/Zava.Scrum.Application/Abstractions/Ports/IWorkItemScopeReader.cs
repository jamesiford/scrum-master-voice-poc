using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Where a work item lives.</summary>
/// <param name="Project">The item's project.</param>
/// <param name="AreaPath">The item's area path.</param>
public sealed record WorkItemLocation(string Project, string AreaPath);

/// <summary>Reads where a work item lives, so a write can be refused when it is outside the team (<see cref="WorkItemScope"/>).</summary>
public interface IWorkItemScopeReader
{
    /// <summary>Finds an item's project and area path in the team's organisation.</summary>
    /// <param name="ado">The team's connection.</param>
    /// <param name="workItemId">The item.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The location, or a not found or external error.</returns>
    Task<Result<WorkItemLocation>> GetLocationAsync(AdoSettings ado, int workItemId, CancellationToken ct);
}
