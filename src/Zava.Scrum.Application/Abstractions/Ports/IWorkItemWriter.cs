using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>
/// Writes approved changes to the work tracker. It accepts only <see cref="ApprovedChange"/>, which exists only for an approved
/// follow-up, so an unapproved change cannot reach it (customer requirement F7).
/// </summary>
public interface IWorkItemWriter
{
    /// <summary>Applies one approved change and records who approved it.</summary>
    /// <param name="change">The change.</param>
    /// <param name="target">The team's Azure DevOps connection the change is written to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The revision the write produced, or an <see cref="ErrorType.ExternalRejected"/> failure.</returns>
    Task<Result<WorkItemRevision>> ApplyAsync(ApprovedChange change, AdoSettings target, CancellationToken ct);
}
