using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Audit.GetAuditChain;

/// <summary>The audit chain for one correlation id.</summary>
/// <param name="CorrelationId">The full W3C trace id.</param>
/// <param name="Tag">The short form written at the end of Azure DevOps comments.</param>
/// <param name="TeamId">The team (taken from the records, so access can be checked per team).</param>
/// <param name="Session">The session, or <see langword="null"/> when the chain starts at a planning pack that has no standup session.</param>
/// <param name="Turns">The session events in order: joins, turns, tool calls.</param>
/// <param name="FollowUps">Every proposal in the chain.</param>
/// <param name="Records">The append-only approval records, in the order they were appended.</param>
/// <param name="Cards">The cards sent for the session.</param>
/// <param name="ApprovedCount">How many follow-ups were approved.</param>
/// <param name="Missing">What is absent from the chain. Empty means the chain is complete.</param>
public sealed record AuditChainDto(
    string CorrelationId,
    string Tag,
    string TeamId,
    AuditSessionDto? Session,
    IReadOnlyList<AuditTurnDto> Turns,
    IReadOnlyList<AuditFollowUpDto> FollowUps,
    IReadOnlyList<ApprovalRecord> Records,
    IReadOnlyList<AuditCardDto> Cards,
    int ApprovedCount,
    IReadOnlyList<string> Missing)
{
    /// <summary>Gets the correlated Teams meeting identity, absent for a Simulated Room or an unobserved meeting lifecycle.</summary>
    public MeetingLifecycle? Meeting { get; init; }

    /// <summary>Gets the stored meeting link, never a model-generated link.</summary>
    public string? JoinUrl { get; init; }

    /// <summary>Gets a value indicating whether every decided follow-up has its approval record, matching hash and write record.</summary>
    public bool Complete => Missing.Count == 0;
}

/// <summary>The session at the start of the chain.</summary>
/// <param name="Id">Session id.</param>
/// <param name="Kind">The ceremony.</param>
/// <param name="Mode">How the meeting was joined (<c>simulated</c> or <c>acs</c>).</param>
/// <param name="CreatedAt">When prepared.</param>
/// <param name="StartedAt">When started.</param>
/// <param name="EndedAt">When ended.</param>
public sealed record AuditSessionDto(Guid Id, CeremonyKind Kind, string? Mode, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt, DateTimeOffset? EndedAt);

/// <summary>One session event.</summary>
/// <param name="At">When.</param>
/// <param name="Type">The kind.</param>
/// <param name="MemberId">The member it concerns.</param>
/// <param name="WorkItemIds">The work items it refers to.</param>
public sealed record AuditTurnDto(DateTimeOffset At, StandupEventType Type, string? MemberId, IReadOnlyList<int> WorkItemIds);

/// <summary>One proposal.</summary>
/// <param name="Id">Follow-up id.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Status">Where it is.</param>
/// <param name="MemberId">The member it is for.</param>
/// <param name="ProposalHash">SHA-256 of the canonical proposed changes.</param>
/// <param name="Changes">How many changes it proposes.</param>
/// <param name="ApprovedBy">The approver's display name (text only).</param>
public sealed record AuditFollowUpDto(Guid Id, FollowUpKind Kind, FollowUpStatus Status, string? MemberId, string ProposalHash, int Changes, string? ApprovedBy);

/// <summary>One outbox entry.</summary>
/// <param name="Id">Outbox id.</param>
/// <param name="SentAt">When sent.</param>
/// <param name="Kind">Card kind.</param>
/// <param name="FollowUpId">The follow-up it concerns.</param>
public sealed record AuditCardDto(Guid Id, DateTimeOffset SentAt, CardKind Kind, Guid? FollowUpId);
