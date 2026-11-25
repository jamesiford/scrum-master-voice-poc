using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Removes the comments this agent wrote to Azure DevOps, so a demo can be re-run from a clean board (SPEC 2).</summary>
/// <remarks>Only the identity that wrote a comment can delete it, so this has to run inside the API.</remarks>
public interface IDemoAdoReset
{
    /// <summary>Deletes every work item comment in the project that carries the agent's approval stamp.</summary>
    /// <param name="ado">The team's Azure DevOps connection (organisation and project).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>How many comments were deleted.</returns>
    Task<int> RemoveAgentCommentsAsync(AdoSettings ado, CancellationToken ct);
}
