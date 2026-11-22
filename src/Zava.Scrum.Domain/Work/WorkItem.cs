namespace Zava.Scrum.Domain.Work;

/// <summary>A sprint work item as read from Azure DevOps, already normalised (SPEC 9.3).</summary>
/// <param name="Id">Work item id.</param>
/// <param name="Title">Title.</param>
/// <param name="Type">Work item type, for example <c>User Story</c>, <c>Bug</c>, <c>Task</c>.</param>
/// <param name="State">Process state, for example <c>Active</c>.</param>
/// <param name="AssignedTo">Assignee unique name, or <see langword="null"/> when unassigned.</param>
/// <param name="OwnerKey">Roster key of the owner, or <see langword="null"/> when nobody owns it.</param>
/// <param name="StateChangedAt">When the item entered its current state.</param>
/// <param name="ChangedAt">When the item was last changed.</param>
/// <param name="StoryPoints">Story points, if the type carries them.</param>
/// <param name="RemainingWork">Remaining hours, if the type carries them.</param>
/// <param name="DueDate">Date the item should finish by, if set.</param>
/// <param name="Tags">Tags on the item.</param>
/// <param name="OriginalEstimate">Original estimate in hours, if the type carries it.</param>
/// <param name="AcceptanceCriteria">Acceptance criteria text, or <see langword="null"/> when none is written (Definition of Ready).</param>
public sealed record WorkItem(
    WorkItemId Id,
    string Title,
    string Type,
    string State,
    string? AssignedTo,
    string? OwnerKey,
    DateTimeOffset StateChangedAt,
    DateTimeOffset ChangedAt,
    double? StoryPoints,
    double? RemainingWork,
    DateTimeOffset? DueDate,
    IReadOnlyList<string> Tags,
    double? OriginalEstimate = null,
    string? AcceptanceCriteria = null)
{
    private static readonly string[] InProgressStates = ["Active", "In Progress", "Committed", "Doing"];
    private static readonly string[] NotStartedStates = ["New", "To Do", "Proposed"];
    private static readonly string[] DoneStates = ["Closed", "Done", "Resolved"];

    /// <summary>Gets a value indicating whether work has started and not finished.</summary>
    public bool IsInProgress => InProgressStates.Contains(State, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether work has not started.</summary>
    public bool IsNotStarted => NotStartedStates.Contains(State, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the item is finished and counts as completed.</summary>
    public bool IsDone => DoneStates.Contains(State, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the item carries the given tag.</summary>
    /// <param name="tag">Tag name, compared case-insensitively.</param>
    /// <returns><see langword="true"/> if tagged.</returns>
    public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
}
