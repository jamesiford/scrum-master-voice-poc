using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Flags;

/// <summary>In progress and not changed for more than a day.</summary>
public sealed class NoRecentUpdateRule : IFlagRule
{
    /// <inheritdoc />
    public Flag Flag => Flag.NoRecentUpdate;

    /// <inheritdoc />
    public bool Applies(WorkItem item, FlagContext context)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        return item.IsInProgress && item.ChangedAt < context.Now.AddDays(-1);
    }
}
