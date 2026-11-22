using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Flags;

/// <summary>
/// Due within the team's at-risk window and unlikely to finish: more than the configured share of the original estimate is still
/// remaining, or work has not started. Items already past due are <see cref="Flag.Overdue"/>, not at risk.
/// </summary>
public sealed class AtRiskRule : IFlagRule
{
    /// <inheritdoc />
    public Flag Flag => Flag.AtRisk;

    /// <inheritdoc />
    public bool Applies(WorkItem item, FlagContext context)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(context);
        if (item.IsDone || item.DueDate is not { } due || due < context.Now || due > context.Now.AddDays(context.Behaviours.AtRiskDays))
        {
            return false;
        }

        return item.IsNotStarted || RemainingShare(item) > context.Behaviours.AtRiskRemainingPct;
    }

    private static double RemainingShare(WorkItem item) =>
        item is { OriginalEstimate: > 0, RemainingWork: { } remaining } ? remaining / item.OriginalEstimate.Value : 0;
}
