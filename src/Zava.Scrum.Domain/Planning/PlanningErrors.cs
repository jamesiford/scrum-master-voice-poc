using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Planning;

/// <summary>Error codes for sprint planning.</summary>
public static class PlanningErrors
{
    /// <summary>Gets the error for an unknown pack.</summary>
    /// <param name="id">The id.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(Guid id) => Error.NotFound("planning.not_found", $"Planning pack '{id}' does not exist.");

    /// <summary>Gets the error for a team that has no pack yet.</summary>
    public static Error NoPack { get; } = Error.NotFound("planning.no_pack", "No planning pack has been prepared yet.");

    /// <summary>Gets the error for a pack that was already decided.</summary>
    /// <param name="status">Current status.</param>
    /// <returns>The error.</returns>
    public static Error AlreadyDecided(PlanningPackStatus status) => Error.Conflict("planning.already_decided", $"This plan is already {status}.");

    /// <summary>Gets the error for a decision with no approver name.</summary>
    public static Error ApproverRequired { get; } = Error.Invalid("planning.approver_required", "A named approver is required.");

    /// <summary>Gets the error for an approval that selects nothing.</summary>
    public static Error NothingSelected { get; } = Error.Invalid("planning.nothing_selected", "Select at least one item to plan.");

    /// <summary>Gets the error for a selected item that is not part of the proposal (rule 2).</summary>
    /// <param name="id">The id.</param>
    /// <returns>The error.</returns>
    public static Error UnknownItem(int id) => Error.Invalid("planning.unknown_item", $"Work item {id} is not part of this plan.");

    /// <summary>Gets the error for a plan that cannot move items because Azure DevOps has no next iteration.</summary>
    public static Error NoNextIteration { get; } = Error.Conflict("planning.no_next_iteration", "Azure DevOps has no future iteration for this team, so there is nowhere to move the items. Add one and prepare the pack again.");
}
