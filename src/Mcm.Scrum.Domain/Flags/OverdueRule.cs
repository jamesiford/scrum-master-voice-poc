using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Flags;

/// <summary>Past its due date and not done.</summary>
public sealed class OverdueRule : IFlagRule
{
    /// <inheritdoc />
    public Flag Flag => Flag.Overdue;

    /// <inheritdoc />
    public bool Applies(WorkItem item, FlagContext context)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        return !item.IsDone && item.DueDate is { } due && due < context.Now;
    }
}
