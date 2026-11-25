using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Domain.FollowUps;

/// <summary>
/// A follow-up to a standup, with proposed Azure DevOps changes. Enforces customer requirement F7: nothing is written
/// to Azure DevOps unless a named person approved this follow-up.
/// </summary>
public sealed class FollowUp : AggregateRoot
{
    private const string LegacyText = "unverified (legacy)";
    private string? _approvedByText;
    private string? _rejectedByText;

    private FollowUp(FollowUpSnapshot s)
    {
        Id = s.Id;
        TeamId = s.TeamId;
        SessionId = s.SessionId;
        MemberId = s.MemberId;
        Kind = s.Kind;
        Message = s.Message;
        Changes = s.Changes;
        Status = s.Status;
        StandupDate = s.StandupDate;
        CreatedAt = s.CreatedAt;
        _approvedByText = s.ApprovedBy;
        _rejectedByText = s.RejectedBy;
        ApprovedByIdentity = s.ApprovedByIdentity;
        RejectedByIdentity = s.RejectedByIdentity;
        CorrelationId = s.CorrelationId ?? Correlation.ForLegacySession(s.SessionId);
        DecidedAt = s.DecidedAt;
        AppliedAt = s.AppliedAt;
        ApplyError = s.ApplyError;
        Ceremony = s.Ceremony;
        ForScrumMaster = s.ForScrumMaster;
        ApprovedVia = s.ApprovedVia;
    }

    /// <summary>Gets the id.</summary>
    public Guid Id { get; }

    /// <summary>Gets the team.</summary>
    public string TeamId { get; }

    /// <summary>Gets the standup it came from.</summary>
    public Guid SessionId { get; }

    /// <summary>Gets the member it is for, if any.</summary>
    public string? MemberId { get; }

    /// <summary>Gets the ceremony it came from.</summary>
    public CeremonyKind Ceremony { get; }

    /// <summary>Gets a value indicating whether it belongs to the Scrum Master rather than a member.</summary>
    public bool ForScrumMaster { get; }

    /// <summary>Gets where it was approved, once approved.</summary>
    public ApprovalChannel? ApprovedVia { get; private set; }

    /// <summary>Gets the kind.</summary>
    public FollowUpKind Kind { get; }

    /// <summary>Gets the card text.</summary>
    public string Message { get; }

    /// <summary>Gets the proposed changes.</summary>
    public IReadOnlyList<ProposedChange> Changes { get; }

    /// <summary>Gets the approval status.</summary>
    public FollowUpStatus Status { get; private set; }

    /// <summary>Gets the standup date.</summary>
    public DateOnly StandupDate { get; }

    /// <summary>Gets when it was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the session's correlation id (a W3C trace id) that ties this follow-up to its meeting, turns, approval and write.</summary>
    public string CorrelationId { get; }

    /// <summary>Gets the verified identity that approved it, or <see langword="null"/> when none was recorded.</summary>
    public ActorIdentity? ApprovedByIdentity { get; private set; }

    /// <summary>Gets the verified identity that rejected it, or <see langword="null"/> when none was recorded.</summary>
    public ActorIdentity? RejectedByIdentity { get; private set; }

    /// <summary>Gets the approver's display name for text, derived from the identity. Data saved before identities were recorded reads "unverified (legacy)".</summary>
    public string? ApprovedBy => ApprovedByIdentity?.DisplayName ?? (string.IsNullOrWhiteSpace(_approvedByText) ? null : LegacyText);

    /// <summary>Gets the rejecter's display name for text, derived from the identity. Data saved before identities were recorded reads "unverified (legacy)".</summary>
    public string? RejectedBy => RejectedByIdentity?.DisplayName ?? (string.IsNullOrWhiteSpace(_rejectedByText) ? null : LegacyText);

    /// <summary>Gets when it was decided.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Gets when its changes were written.</summary>
    public DateTimeOffset? AppliedAt { get; private set; }

    /// <summary>Gets why the last write failed.</summary>
    public string? ApplyError { get; private set; }

    /// <summary>Creates a new pending follow-up.</summary>
    /// <param name="id">Id.</param>
    /// <param name="teamId">Team.</param>
    /// <param name="sessionId">Standup.</param>
    /// <param name="memberId">Member, or <see langword="null"/> for an escalation.</param>
    /// <param name="kind">Kind.</param>
    /// <param name="message">Card text.</param>
    /// <param name="changes">Proposed changes.</param>
    /// <param name="standupDate">Standup date.</param>
    /// <param name="now">Current time.</param>
    /// <param name="ceremony">The ceremony it came from.</param>
    /// <param name="forScrumMaster">True when the Scrum Master, not a member, decides it.</param>
    /// <param name="correlationId">The session's correlation id; derived from the session id when omitted.</param>
    /// <returns>The follow-up.</returns>
    public static FollowUp Create(Guid id, string teamId, Guid sessionId, string? memberId, FollowUpKind kind, string message, IReadOnlyList<ProposedChange> changes, DateOnly standupDate, DateTimeOffset now, CeremonyKind ceremony = CeremonyKind.DailyScrum, bool forScrumMaster = false, string? correlationId = null) =>
        new(new FollowUpSnapshot(id, teamId, sessionId, memberId, kind, message, changes, FollowUpStatus.Pending, standupDate, now, null, null, null, null, null, ceremony, forScrumMaster, null, null, null, correlationId));

    /// <summary>Rehydrates a persisted follow-up.</summary>
    /// <param name="snapshot">Persisted state.</param>
    /// <returns>The follow-up.</returns>
    public static FollowUp Restore(FollowUpSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new FollowUp(snapshot);
    }

    /// <summary>Captures the state for persistence.</summary>
    /// <returns>The snapshot.</returns>
    public FollowUpSnapshot ToSnapshot() =>
        new(Id, TeamId, SessionId, MemberId, Kind, Message, Changes, Status, StandupDate, CreatedAt, _approvedByText, _rejectedByText, DecidedAt, AppliedAt, ApplyError, Ceremony, ForScrumMaster, ApprovedVia, ApprovedByIdentity, RejectedByIdentity, CorrelationId);

    /// <summary>Captures the state for display: the approver and rejecter read as their derived text, so a legacy decision shows as unverified.</summary>
    /// <returns>The snapshot with derived display names.</returns>
    public FollowUpSnapshot ToReadModel() => ToSnapshot() with { ApprovedBy = ApprovedBy, RejectedBy = RejectedBy };

    /// <summary>Records that the card was sent.</summary>
    public void MarkSent()
    {
        if (Status == FollowUpStatus.Pending)
        {
            Status = FollowUpStatus.Sent;
        }
    }

    /// <summary>Approves the follow-up. Only an identified person can approve, and only while it is undecided.</summary>
    /// <param name="approver">The verified identity of who approved. A display name alone is not an identity.</param>
    /// <param name="now">Current time.</param>
    /// <param name="channel">Where the approval happened; written into the Azure DevOps attribution.</param>
    /// <returns>Success, or a validation or conflict error.</returns>
    public Result Approve(ActorIdentity? approver, DateTimeOffset now, ApprovalChannel channel = ApprovalChannel.Teams)
    {
        if (approver is null || approver.ObjectId == Guid.Empty || string.IsNullOrWhiteSpace(approver.DisplayName))
        {
            return Result.Failure(FollowUpErrors.ApproverRequired);
        }

        if (Status is not (FollowUpStatus.Pending or FollowUpStatus.Sent))
        {
            return Result.Failure(FollowUpErrors.AlreadyDecided(Status));
        }

        Status = FollowUpStatus.Approved;
        ApprovedByIdentity = approver;
        _approvedByText = approver.DisplayName;
        ApprovedVia = channel;
        DecidedAt = now;
        Raise(new FollowUpApproved(Id, now));
        return Result.Success();
    }

    /// <summary>Rejects the follow-up. Its changes are never written.</summary>
    /// <param name="rejecter">The verified identity of who rejected.</param>
    /// <param name="now">Current time.</param>
    /// <returns>Success, or a validation or conflict error.</returns>
    public Result Reject(ActorIdentity? rejecter, DateTimeOffset now)
    {
        if (rejecter is null || rejecter.ObjectId == Guid.Empty || string.IsNullOrWhiteSpace(rejecter.DisplayName))
        {
            return Result.Failure(FollowUpErrors.ApproverRequired);
        }

        if (Status is not (FollowUpStatus.Pending or FollowUpStatus.Sent))
        {
            return Result.Failure(FollowUpErrors.AlreadyDecided(Status));
        }

        Status = FollowUpStatus.Rejected;
        RejectedByIdentity = rejecter;
        _rejectedByText = rejecter.DisplayName;
        DecidedAt = now;
        Raise(new FollowUpRejected(Id, now));
        return Result.Success();
    }

    /// <summary>
    /// Gets the changes that may be written. This is the only way to obtain an <see cref="ApprovedChange"/>, and it
    /// refuses unless a person approved the follow-up (customer requirement F7). An already applied follow-up has nothing left to write.
    /// </summary>
    /// <returns>The changes, or a <see cref="ErrorType.NotApproved"/> error.</returns>
    public Result<IReadOnlyList<ApprovedChange>> GetApprovedChanges()
    {
        if (Status != FollowUpStatus.Approved)
        {
            return Result<IReadOnlyList<ApprovedChange>>.Failure(FollowUpErrors.NotApproved(Status));
        }

        // A legacy approval has a name but no verified identity, so it is never written (ADR 0017).
        if (ApprovedByIdentity is null)
        {
            return Result<IReadOnlyList<ApprovedChange>>.Failure(IdentityErrors.LegacyUnverified);
        }

        IReadOnlyList<ApprovedChange> changes = Changes.Select(c => new ApprovedChange(c, ApprovedByIdentity.DisplayName, StandupDate, Ceremony, ApprovedVia ?? ApprovalChannel.Teams, CorrelationId)).ToList();
        return Result<IReadOnlyList<ApprovedChange>>.Success(changes);
    }

    /// <summary>Computes the SHA-256 of the canonical proposed changes, recorded with the approval.</summary>
    /// <returns>Upper-case hex.</returns>
    public string ComputeProposalHash() => ProposalHash.Of(Changes);

    /// <summary>Records that every approved change was written.</summary>
    /// <param name="now">Current time.</param>
    public void MarkApplied(DateTimeOffset now)
    {
        if (Status != FollowUpStatus.Approved)
        {
            return;
        }

        Status = FollowUpStatus.Applied;
        AppliedAt = now;
        ApplyError = null;
        Raise(new FollowUpApplied(Id, now));
    }

    /// <summary>Records that writing failed; the follow-up stays approved so the write can be retried.</summary>
    /// <param name="error">Plain description.</param>
    public void MarkApplyFailed(string error) => ApplyError = error;
}
