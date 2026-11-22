using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Standups;

/// <summary>An open work item as shown in the pre-brief.</summary>
/// <param name="Id">Work item id (always from the current sprint).</param>
/// <param name="Title">Title.</param>
/// <param name="Type">Work item type.</param>
/// <param name="State">State.</param>
/// <param name="DaysInState">Whole days in the current state.</param>
/// <param name="Flags">Flags raised by the flag rules.</param>
public sealed record BriefItem(
    WorkItemId Id,
    string Title,
    string Type,
    string State,
    int DaysInState,
    IReadOnlyList<Flag> Flags);
