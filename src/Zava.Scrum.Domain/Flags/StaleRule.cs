using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Flags;

/// <summary>In progress and in the same state for more than the team's stale days.</summary>
public sealed class StaleRule : IFlagRule
{
    /// <inheritdoc />
    public Flag Flag => Flag.Stale;

    /// <inheritdoc />
    public bool Applies(WorkItem item, FlagContext context)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        return item.IsInProgress && item.StateChangedAt < context.Now.AddDays(-context.Behaviours.StaleDays);
    }
}
