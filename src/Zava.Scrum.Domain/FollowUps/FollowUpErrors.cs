using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.FollowUps;

/// <summary>Error codes for follow-ups.</summary>
public static class FollowUpErrors
{
    /// <summary>Gets the error for a decision with no approver name.</summary>
    public static Error ApproverRequired { get; } = Error.Invalid("followup.approver_required", "A named approver is required.");

    /// <summary>Gets the error for a follow-up that was already decided.</summary>
    /// <param name="status">Current status.</param>
    /// <returns>The error.</returns>
    public static Error AlreadyDecided(FollowUpStatus status) => Error.Conflict("followup.already_decided", $"This follow-up is already {status}.");

    /// <summary>Gets the error for a write attempted without approval (customer requirement F7).</summary>
    /// <param name="status">Current status.</param>
    /// <returns>The error.</returns>
    public static Error NotApproved(FollowUpStatus status) =>
        Error.NotApproved("followup.not_approved", $"Nothing is written to Azure DevOps without an approved follow-up. This one is {status}.");

    /// <summary>Gets the error for a work item that is not in the current sprint (rule 2).</summary>
    /// <param name="id">The id.</param>
    /// <returns>The error.</returns>
    public static Error UnknownWorkItem(int id) => Error.Invalid("followup.unknown_work_item", $"Work item {id} is not in the current sprint.");

    /// <summary>Gets the error for an unknown follow-up.</summary>
    /// <param name="id">The id.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(Guid id) => Error.NotFound("followup.not_found", $"Follow-up '{id}' does not exist.");
}
