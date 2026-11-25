using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>What the work tracker reported after a write: the item and the revision the write produced.</summary>
/// <param name="WorkItemId">The work item.</param>
/// <param name="Revision">The revision number; zero when the change was already in effect and nothing was written.</param>
public sealed record WorkItemRevision(int WorkItemId, int Revision)
{
    /// <summary>Gets a value indicating whether the write produced a revision.</summary>
    public bool Written => Revision > 0;

    /// <summary>Converts to the form kept in the approval trail.</summary>
    /// <returns>The revision.</returns>
    public AdoRevision ToRecord() => new(WorkItemId, Revision);
}
