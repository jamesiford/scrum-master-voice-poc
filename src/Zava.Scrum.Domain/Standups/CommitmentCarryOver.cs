using Zava.Scrum.Domain.Flags;

namespace Zava.Scrum.Domain.Standups;

/// <summary>Works out commitments and unblockers carried over from earlier standups. Pure; sessions are passed in (M9-R2, M9-R3).</summary>
public static class CommitmentCarryOver
{
    /// <summary>A commitment still open this many standups later counts as missed.</summary>
    public const int MissedAfterStandups = 2;

    private const int SessionsConsidered = 3;

    /// <summary>Collects the carry-over from earlier daily standups.</summary>
    /// <param name="previous">Earlier sessions of the team, newest first. Other ceremonies and sessions that never started are ignored.</param>
    /// <param name="openItems">Ids of items in the current sprint that are not done.</param>
    /// <returns>The carry-over.</returns>
    public static CarryOver Collect(IReadOnlyList<StandupSession> previous, IReadOnlySet<int> openItems)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(openItems);
        var standups = previous.Where(s => s.Kind == CeremonyKind.DailyScrum && s.StartedAt is not null).Take(SessionsConsidered).ToList();
        var commitments = new Dictionary<(string Member, int Item), PreviousCommitment>();
        var blocked = new Dictionary<int, StillBlocked>();
        for (var i = 0; i < standups.Count; i++)
        {
            foreach (var e in standups[i].ActiveEvents.Where(e => e.MemberId is not null))
            {
                Collect(e, i + 1, openItems, commitments, blocked);
            }
        }

        var perMember = commitments
            .GroupBy(c => c.Key.Member)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PreviousCommitment>)g.Select(c => c.Value).OrderBy(c => c.WorkItemId).ToList());
        return new CarryOver(perMember, [.. blocked.Values]);
    }

    private static void Collect(
        StandupEvent e,
        int standupsAgo,
        IReadOnlySet<int> openItems,
        Dictionary<(string Member, int Item), PreviousCommitment> commitments,
        Dictionary<int, StillBlocked> blocked)
    {
        foreach (var id in e.WorkItemIds.Where(openItems.Contains))
        {
            if (e.Type == StandupEventType.Commitment)
            {
                // Keep the oldest mention, so the age reflects how long the promise has stood.
                commitments[(e.MemberId!, id)] = new PreviousCommitment(id, e.Text.Trim(), standupsAgo);
            }
            else if (e.Type == StandupEventType.Blocker && e.UnblockerMemberId is not null && !blocked.ContainsKey(id))
            {
                blocked[id] = new StillBlocked(id, e.UnblockerMemberId);
            }
        }
    }

    /// <summary>Gets the items a member missed their commitment on.</summary>
    /// <param name="carry">The carry-over.</param>
    /// <returns>Member id and commitment, for commitments that count as missed.</returns>
    public static IEnumerable<(string MemberId, PreviousCommitment Commitment)> Missed(CarryOver carry)
    {
        ArgumentNullException.ThrowIfNull(carry);
        return carry.Commitments.SelectMany(c => c.Value.Where(p => p.IsMissed).Select(p => (c.Key, p)));
    }

    /// <summary>Marks <see cref="Flag.MissedCommitment"/> on the items whose commitment was missed.</summary>
    /// <param name="flags">The item's current flags.</param>
    /// <param name="missed">Whether the item is a missed commitment.</param>
    /// <returns>The flags, with the new one added once.</returns>
    public static IReadOnlyList<Flag> WithMissed(IReadOnlyList<Flag> flags, bool missed)
    {
        ArgumentNullException.ThrowIfNull(flags);
        return missed && !flags.Contains(Flag.MissedCommitment) ? [.. flags, Flag.MissedCommitment] : flags;
    }
}
