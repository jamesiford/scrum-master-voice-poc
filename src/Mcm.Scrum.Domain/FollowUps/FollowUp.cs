using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Domain.FollowUps;

/// <summary>
/// A follow-up to a standup, with proposed Azure DevOps changes. Enforces customer requirement F7: nothing is written
/// to Azure DevOps unless a named person approved this follow-up.
/// </summary>
public sealed class FollowUp : AggregateRoot
{
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
        ApprovedBy = s.ApprovedBy;
        RejectedBy = s.RejectedBy;
        DecidedAt = s.DecidedAt;
        AppliedAt = s.AppliedAt;
        ApplyError = s.ApplyError;
    }

    /// <summary>Gets the id.</summary>
    public Guid Id { get; }

    /// <summary>Gets the team.</summary>
    public string TeamId { get; }

    /// <summary>Gets the standup it came from.</summary>
    public Guid SessionId { get; }

    /// <summary>Gets the member it is for, if any.</summary>
    public string? MemberId { get; }

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

    /// <summary>Gets who approved it.</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Gets who rejected it.</summary>
    public string? RejectedBy { get; private set; }

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
    /// <returns>The follow-up.</returns>
    public static FollowUp Create(Guid id, string teamId, Guid sessionId, string? memberId, FollowUpKind kind, string message, IReadOnlyList<ProposedChange> changes, DateOnly standupDate, DateTimeOffset now) =>
        new(new FollowUpSnapshot(id, teamId, sessionId, memberId, kind, message, changes, FollowUpStatus.Pending, standupDate, now, null, null, null, null, null));

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
        new(Id, TeamId, SessionId, MemberId, Kind, Message, Changes, Status, StandupDate, CreatedAt, ApprovedBy, RejectedBy, DecidedAt, AppliedAt, ApplyError);

    /// <summary>Records that the card was sent.</summary>
    public void MarkSent()
    {
        if (Status == FollowUpStatus.Pending)
        {
            Status = FollowUpStatus.Sent;
        }
    }

    /// <summary>Approves the follow-up. Only a named person can approve, and only while it is undecided.</summary>
    /// <param name="approver">Who approved (display name).</param>
    /// <param name="now">Current time.</param>
    /// <returns>Success, or a validation or conflict error.</returns>
    public Result Approve(string approver, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(approver))
        {
            return Result.Failure(FollowUpErrors.ApproverRequired);
        }

        if (Status is not (FollowUpStatus.Pending or FollowUpStatus.Sent))
        {
            return Result.Failure(FollowUpErrors.AlreadyDecided(Status));
        }

        Status = FollowUpStatus.Approved;
        ApprovedBy = approver.Trim();
        DecidedAt = now;
        Raise(new FollowUpApproved(Id, now));
        return Result.Success();
    }

    /// <summary>Rejects the follow-up. Its changes are never written.</summary>
    /// <param name="rejecter">Who rejected (display name).</param>
    /// <param name="now">Current time.</param>
    /// <returns>Success, or a validation or conflict error.</returns>
    public Result Reject(string rejecter, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(rejecter))
        {
            return Result.Failure(FollowUpErrors.ApproverRequired);
        }

        if (Status is not (FollowUpStatus.Pending or FollowUpStatus.Sent))
        {
            return Result.Failure(FollowUpErrors.AlreadyDecided(Status));
        }

        Status = FollowUpStatus.Rejected;
        RejectedBy = rejecter.Trim();
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
        if (Status != FollowUpStatus.Approved || string.IsNullOrWhiteSpace(ApprovedBy))
        {
            return Result<IReadOnlyList<ApprovedChange>>.Failure(FollowUpErrors.NotApproved(Status));
        }

        IReadOnlyList<ApprovedChange> changes = Changes.Select(c => new ApprovedChange(c, ApprovedBy, StandupDate)).ToList();
        return Result<IReadOnlyList<ApprovedChange>>.Success(changes);
    }

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
