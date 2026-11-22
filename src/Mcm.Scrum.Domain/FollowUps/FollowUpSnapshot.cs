using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Domain.FollowUps;

/// <summary>Everything persisted about a <see cref="FollowUp"/>.</summary>
/// <param name="Id">Follow-up id.</param>
/// <param name="TeamId">Team id.</param>
/// <param name="SessionId">The standup it came from.</param>
/// <param name="MemberId">The member it is for, or <see langword="null"/> for an escalation to the Scrum Master.</param>
/// <param name="Kind">Kind.</param>
/// <param name="Message">Text shown on the card.</param>
/// <param name="Changes">Proposed Azure DevOps changes.</param>
/// <param name="Status">Approval status.</param>
/// <param name="StandupDate">Date of the standup.</param>
/// <param name="CreatedAt">When created.</param>
/// <param name="ApprovedBy">Who approved.</param>
/// <param name="RejectedBy">Who rejected.</param>
/// <param name="DecidedAt">When decided.</param>
/// <param name="AppliedAt">When written to Azure DevOps.</param>
/// <param name="ApplyError">Why the last write failed, if it did.</param>
/// <param name="Ceremony">The ceremony it came from; <see cref="CeremonyKind.DailyScrum"/> for data saved before ceremonies existed.</param>
/// <param name="ForScrumMaster">True when the Scrum Master decides it.</param>
/// <param name="ApprovedVia">Where it was approved.</param>
public sealed record FollowUpSnapshot(
    Guid Id,
    string TeamId,
    Guid SessionId,
    string? MemberId,
    FollowUpKind Kind,
    string Message,
    IReadOnlyList<ProposedChange> Changes,
    FollowUpStatus Status,
    DateOnly StandupDate,
    DateTimeOffset CreatedAt,
    string? ApprovedBy,
    string? RejectedBy,
    DateTimeOffset? DecidedAt,
    DateTimeOffset? AppliedAt,
    string? ApplyError,
    CeremonyKind Ceremony = CeremonyKind.DailyScrum,
    bool ForScrumMaster = false,
    ApprovalChannel? ApprovedVia = null);
