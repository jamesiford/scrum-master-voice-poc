using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>
/// Writes approved changes to the work tracker. It accepts only <see cref="ApprovedChange"/>, which exists only for an approved
/// follow-up, so an unapproved change cannot reach it (customer requirement F7).
/// </summary>
public interface IWorkItemWriter
{
    /// <summary>Applies one approved change and records who approved it.</summary>
    /// <param name="change">The change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success, or an <see cref="ErrorType.ExternalRejected"/> failure.</returns>
    Task<Result> ApplyAsync(ApprovedChange change, CancellationToken ct);
}
