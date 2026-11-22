namespace Zava.Scrum.Domain.Work;

/// <summary>The current sprint and its work items.</summary>
/// <param name="IterationPath">Azure DevOps iteration path.</param>
/// <param name="StartsAt">Sprint start.</param>
/// <param name="EndsAt">Sprint end.</param>
/// <param name="Items">All non-removed work items in the sprint, open and done.</param>
public sealed record SprintSnapshot(
    string IterationPath,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    IReadOnlyList<WorkItem> Items);
