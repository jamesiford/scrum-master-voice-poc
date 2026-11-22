namespace Zava.Scrum.Domain.Planning;

/// <summary>An item the plan proposes for the next sprint.</summary>
/// <param name="WorkItemId">Work item id (always from Azure DevOps).</param>
/// <param name="Type">Work item type.</param>
/// <param name="Title">Title.</param>
/// <param name="Points">Story points.</param>
/// <param name="Source">Carry-over or backlog.</param>
/// <param name="OwnerMemberId">The member who owns it, or is suggested to own it.</param>
/// <param name="OwnerName">Their name.</param>
/// <param name="OwnerSuggested"><see langword="true"/> when the item had no owner and the owner is a suggestion that approval would assign.</param>
public sealed record PlannedItem(
    int WorkItemId,
    string Type,
    string Title,
    double Points,
    PlanningSource Source,
    string? OwnerMemberId,
    string? OwnerName,
    bool OwnerSuggested);
