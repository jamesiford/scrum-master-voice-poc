using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Flags;

/// <summary>Not done and nobody owns it.</summary>
public sealed class UnassignedRule : IFlagRule
{
    /// <inheritdoc />
    public Flag Flag => Flag.Unassigned;

    /// <inheritdoc />
    public bool Applies(WorkItem item, FlagContext context)
    {
        ArgumentNullException.ThrowIfNull(item);
        return !item.IsDone && item.AssignedTo is null && item.OwnerKey is null;
    }
}
