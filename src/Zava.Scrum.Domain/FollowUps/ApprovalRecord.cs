using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Domain.FollowUps;

/// <summary>What an <see cref="ApprovalRecord"/> records.</summary>
public enum ApprovalRecordKind
{
    /// <summary>A person approved the follow-up.</summary>
    Approved,

    /// <summary>A person rejected the follow-up.</summary>
    Rejected,

    /// <summary>The approved changes were written to Azure DevOps; the record carries the revision ids.</summary>
    Applied,
}

/// <summary>An Azure DevOps work item revision that an approved change produced.</summary>
/// <param name="WorkItemId">The work item.</param>
/// <param name="Revision">The revision number Azure DevOps returned.</param>
public sealed record AdoRevision(int WorkItemId, int Revision);

/// <summary>
/// One immutable entry in the append-only approval trail (Cosmos container <c>ApprovalRecords</c>). Records are only ever
/// appended: the later write to Azure DevOps is a second <see cref="ApprovalRecordKind.Applied"/> record that points at the decision.
/// </summary>
/// <param name="Id">Record id.</param>
/// <param name="CorrelationId">The session's W3C trace id.</param>
/// <param name="FollowUpId">The follow-up.</param>
/// <param name="TeamId">The team (every record is team-scoped).</param>
/// <param name="Kind">What happened.</param>
/// <param name="ProposalHash">SHA-256 of the canonical proposed changes.</param>
/// <param name="Actor">Who approved or rejected; for <see cref="ApprovalRecordKind.Applied"/>, who approved it.</param>
/// <param name="Channel">Where it was decided.</param>
/// <param name="OccurredAt">When it happened.</param>
/// <param name="Authentication">The claims that proved the sign-in (no tokens).</param>
/// <param name="DecisionRecordId">For <see cref="ApprovalRecordKind.Applied"/>, the approval record it follows.</param>
/// <param name="AdoRevisions">For <see cref="ApprovalRecordKind.Applied"/>, the revisions written.</param>
public sealed record ApprovalRecord(
    Guid Id,
    string CorrelationId,
    Guid FollowUpId,
    string TeamId,
    ApprovalRecordKind Kind,
    string ProposalHash,
    ActorIdentity Actor,
    ApprovalChannel Channel,
    DateTimeOffset OccurredAt,
    AuthenticationContext Authentication,
    Guid? DecisionRecordId,
    IReadOnlyList<AdoRevision> AdoRevisions);
