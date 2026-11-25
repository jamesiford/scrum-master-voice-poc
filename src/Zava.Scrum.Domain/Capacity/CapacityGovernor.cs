namespace Zava.Scrum.Domain.Capacity;

/// <summary>A standup that is due to start.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="DueAt">When it was due.</param>
public sealed record DueStandup(string TeamId, DateTimeOffset DueAt);

/// <summary>What to do with a due standup.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="Start">Whether to start it now.</param>
/// <param name="Reason">Why it is waiting; <see langword="null"/> when it starts.</param>
/// <param name="Blocked">True when the standup cannot start at all today (no capacity within the grace window), so the team must be told.</param>
public sealed record CapacityDecision(string TeamId, bool Start, string? Reason, bool Blocked);

/// <summary>The limits the host runs under.</summary>
/// <param name="MaxConcurrentStandups">How many standups may run at once on this host (also bounded by realtime model and media bot capacity).</param>
/// <param name="StaggerSeconds">Minimum gap between two starts, so several due standups do not open realtime sessions in the same instant.</param>
/// <param name="GraceMinutes">How long a due standup may wait before it is reported as unable to start.</param>
public sealed record CapacityLimits(int MaxConcurrentStandups, int StaggerSeconds, int GraceMinutes)
{
    /// <summary>Gets the defaults: three at once, a five second stagger, a ten minute grace.</summary>
    public static CapacityLimits Default { get; } = new(3, 5, 10);
}

/// <summary>
/// Decides which due standups start now and which wait, with a visible reason. A ceremony is never dropped: a standup that cannot
/// start is reported <c>Blocked</c> after the grace window so someone is told, and it keeps being retried until its day ends.
/// </summary>
public static class CapacityGovernor
{
    /// <summary>Plans one tick.</summary>
    /// <param name="due">The standups due now, any order.</param>
    /// <param name="running">How many standups are running on this host.</param>
    /// <param name="lastStartAt">When the last standup was started on this host, or <see langword="null"/>.</param>
    /// <param name="now">Current time.</param>
    /// <param name="limits">The limits.</param>
    /// <returns>One decision per due standup, oldest first. At most one starts per tick (the stagger), and never past the limit.</returns>
    public static IReadOnlyList<CapacityDecision> Plan(IReadOnlyList<DueStandup> due, int running, DateTimeOffset? lastStartAt, DateTimeOffset now, CapacityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(due);
        ArgumentNullException.ThrowIfNull(limits);
        var decisions = new List<CapacityDecision>();
        var free = Math.Max(0, limits.MaxConcurrentStandups - running);
        var staggerOpen = lastStartAt is null || (now - lastStartAt.Value).TotalSeconds >= limits.StaggerSeconds;
        foreach (var d in due.OrderBy(x => x.DueAt).ThenBy(x => x.TeamId, StringComparer.Ordinal))
        {
            if (free > 0 && staggerOpen)
            {
                decisions.Add(new CapacityDecision(d.TeamId, true, null, false));
                free--;
                staggerOpen = false;
                continue;
            }

            var reason = free == 0
                ? $"{running} of {limits.MaxConcurrentStandups} standups are already running on this host."
                : $"Another standup started less than {limits.StaggerSeconds} seconds ago; starts are staggered.";
            var waited = now - d.DueAt;
            decisions.Add(new CapacityDecision(d.TeamId, false, reason, free == 0 && waited >= TimeSpan.FromMinutes(limits.GraceMinutes)));
        }

        return decisions;
    }
}
