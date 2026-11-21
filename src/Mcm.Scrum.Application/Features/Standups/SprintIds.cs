using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Application.Features.Standups;

/// <summary>The set of work item ids the agent may mention: those in the current pre-brief (rule 2).</summary>
public static class SprintIds
{
    /// <summary>Collects every id in a pre-brief.</summary>
    /// <param name="brief">The pre-brief, or <see langword="null"/>.</param>
    /// <returns>The known ids; empty when there is no brief.</returns>
    public static HashSet<WorkItemId> From(PreBrief? brief) =>
        brief is null
            ? []
            : brief.Members.SelectMany(m => m.Items).Concat(brief.UnassignedItems).Select(i => i.Id).ToHashSet();
}
