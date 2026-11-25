using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>
/// The append-only approval trail. There is deliberately no update and no delete: a later fact (the Azure DevOps write) is a new
/// record that points at the decision.
/// </summary>
public interface IApprovalRecordStore
{
    /// <summary>Appends a record.</summary>
    /// <param name="record">The record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task AppendAsync(ApprovalRecord record, CancellationToken ct);

    /// <summary>Lists the records of a follow-up in the order they were appended.</summary>
    /// <param name="followUpId">The follow-up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The records.</returns>
    Task<IReadOnlyList<ApprovalRecord>> ListByFollowUpAsync(Guid followUpId, CancellationToken ct);

    /// <summary>Lists the records whose correlation id starts with a prefix, in the order they were appended.</summary>
    /// <param name="correlationPrefix">The full id or its short form without the <c>zsa-</c> tag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The records.</returns>
    Task<IReadOnlyList<ApprovalRecord>> ListByCorrelationAsync(string correlationPrefix, CancellationToken ct);
}
