using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Flags;

/// <summary>Tagged <c>Blocked</c> and not done.</summary>
public sealed class BlockedRule : IFlagRule
{
    /// <inheritdoc />
    public Flag Flag => Flag.Blocked;

    /// <inheritdoc />
    public bool Applies(WorkItem item, FlagContext context)
    {
        ArgumentNullException.ThrowIfNull(item);
        return !item.IsDone && item.HasTag("Blocked");
    }
}
