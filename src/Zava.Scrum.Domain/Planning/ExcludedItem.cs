namespace Mcm.Scrum.Domain.Planning;

/// <summary>An item the plan does not select, with why.</summary>
/// <param name="WorkItemId">Work item id.</param>
/// <param name="Title">Title.</param>
/// <param name="Points">Story points, if estimated.</param>
/// <param name="Reasons">Why it was left out.</param>
public sealed record ExcludedItem(int WorkItemId, string Title, double? Points, IReadOnlyList<string> Reasons);
