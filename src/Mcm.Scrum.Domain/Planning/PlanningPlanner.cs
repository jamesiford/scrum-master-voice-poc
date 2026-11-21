using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Planning;

/// <summary>
/// Sprint planning prep (SPEC M8): capacity, carry-over and readiness against a simple Definition of Ready, plus owner suggestions
/// for unassigned items. Pure rules; the suggestion is the active member with the least open story points.
/// </summary>
public static class PlanningPlanner
{
    /// <summary>Builds the planning prep.</summary>
    /// <param name="sprint">The current sprint.</param>
    /// <param name="roster">The roster.</param>
    /// <returns>The prep.</returns>
    public static PlanningPrep Plan(SprintSnapshot sprint, IReadOnlyList<Member> roster)
    {
        ArgumentNullException.ThrowIfNull(sprint);
        ArgumentNullException.ThrowIfNull(roster);
        var active = roster.Where(m => m.Active).ToList();
        var open = sprint.Items.Where(i => !i.IsDone).ToList();
        var byKey = active.ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

        var loads = active
            .Select(m =>
            {
                var mine = open.Where(i => string.Equals(i.OwnerKey, m.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                return new MemberLoad(m.Id, m.DisplayName, mine.Count, mine.Sum(i => i.StoryPoints ?? 0), mine.Sum(i => i.RemainingWork ?? 0));
            })
            .ToList();

        var carry = open
            .Select(i => new CarryOverItem(i.Id.Value, i.Title, i.State, i.StoryPoints, i.OwnerKey is not null && byKey.TryGetValue(i.OwnerKey, out var o) ? o.DisplayName : null))
            .ToList();

        return new PlanningPrep(loads, carry, ReadinessGaps(open), Suggest(open, loads));
    }

    private static List<ReadinessGap> ReadinessGaps(List<WorkItem> open) =>
        open.Select(i => (Item: i, Reasons: Reasons(i)))
            .Where(x => x.Reasons.Count > 0)
            .Select(x => new ReadinessGap(x.Item.Id.Value, x.Item.Title, x.Reasons))
            .ToList();

    private static List<string> Reasons(WorkItem i)
    {
        var reasons = new List<string>();
        if (i.Type is "User Story" or "Bug" && (i.StoryPoints is null or <= 0))
        {
            reasons.Add("No story points");
        }

        if (i.OwnerKey is null)
        {
            reasons.Add("No owner");
        }

        if (i.Type == "Task" && (i.RemainingWork is null or <= 0) && !i.IsDone)
        {
            reasons.Add("No remaining-work estimate");
        }

        return reasons;
    }

    private static List<AssignmentSuggestion> Suggest(List<WorkItem> open, List<MemberLoad> loads)
    {
        var points = loads.ToDictionary(l => l.MemberId, l => l.OpenPoints);
        var result = new List<AssignmentSuggestion>();
        foreach (var item in open.Where(i => i.OwnerKey is null).OrderByDescending(i => i.StoryPoints ?? 0))
        {
            var pick = loads.OrderBy(l => points[l.MemberId]).ThenBy(l => l.DisplayName, StringComparer.Ordinal).FirstOrDefault();
            if (pick is null)
            {
                break;
            }

            result.Add(new AssignmentSuggestion(item.Id.Value, item.Title, pick.MemberId, pick.DisplayName, $"{pick.DisplayName} has the fewest open story points ({points[pick.MemberId]:0.#})."));
            points[pick.MemberId] += item.StoryPoints ?? 0;
        }

        return result;
    }
}
