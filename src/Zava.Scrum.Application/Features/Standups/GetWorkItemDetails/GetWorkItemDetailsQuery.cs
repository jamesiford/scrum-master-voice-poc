using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Standups.GetWorkItemDetails;

/// <summary>The facts a teammate may ask about during standup, all computed in code.</summary>
/// <param name="Id">Work item id.</param>
/// <param name="Title">Title.</param>
/// <param name="Type">Work item type.</param>
/// <param name="State">Current state.</param>
/// <param name="AssignedTo">Owner display name, or null.</param>
/// <param name="DaysInState">Whole days in the current state.</param>
/// <param name="DueDate">Due date as text, or "none set".</param>
/// <param name="Overdue">Whether it is past due and not done.</param>
/// <param name="DaysOverdue">Days past due, 0 when not overdue.</param>
/// <param name="StoryPoints">Story points, or null.</param>
/// <param name="RemainingHours">Remaining work in hours, or null.</param>
/// <param name="Tags">Tags.</param>
public sealed record WorkItemDetails(
    int Id,
    string Title,
    string Type,
    string State,
    string? AssignedTo,
    int DaysInState,
    string DueDate,
    bool Overdue,
    int DaysOverdue,
    double? StoryPoints,
    double? RemainingHours,
    IReadOnlyList<string> Tags);

/// <summary>Reads one current-sprint work item. Ids outside the sprint are refused (rule 2).</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="WorkItemId">Work item id.</param>
public sealed record GetWorkItemDetailsQuery(Guid SessionId, int WorkItemId) : IQuery<WorkItemDetails>;
