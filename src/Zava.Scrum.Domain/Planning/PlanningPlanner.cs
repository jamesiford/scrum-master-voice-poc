using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Planning;

/// <summary>
/// Sprint planning (SPEC M8, CEREMONIES C2). <see cref="Plan"/> is the advice page: load, carry-over, readiness and owner suggestions.
/// <see cref="Propose"/> is the plan itself: capacity from the velocity range, carry-over first, then ready backlog items in order.
/// Pure rules, no model (rule 3).
/// </summary>
public static class PlanningPlanner
{
    /// <summary>The largest estimate that still counts as ready; a bigger item should be split first.</summary>
    public const double MaxReadyPoints = 8;

    /// <summary>How many of the most recent finished sprints set the velocity range.</summary>
    public const int VelocitySprints = 6;

    private static readonly string[] PlannedTypes = ["User Story", "Bug"];

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

    /// <summary>Proposes the next sprint.</summary>
    /// <param name="sprint">The current sprint; its started stories and bugs carry over and are selected first.</param>
    /// <param name="backlog">Backlog items outside the current sprint, in backlog order.</param>
    /// <param name="roster">The roster; availability scales each member's capacity.</param>
    /// <param name="completedPoints">Completed points of recent finished sprints (the velocity history).</param>
    /// <returns>The proposal.</returns>
    public static PlanningProposal Propose(SprintSnapshot sprint, IReadOnlyList<WorkItem> backlog, IReadOnlyList<Member> roster, IReadOnlyList<double> completedPoints)
    {
        ArgumentNullException.ThrowIfNull(sprint);
        ArgumentNullException.ThrowIfNull(backlog);
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(completedPoints);
        var warnings = new List<string>();
        var velocity = VelocityFrom(sprint, completedPoints, warnings);
        var active = roster.Where(m => m.Active).ToList();
        var byKey = active.ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

        var selected = new List<(WorkItem Item, PlanningSource Source)>();
        var excluded = new List<ExcludedItem>();
        SelectCarryOver(sprint, selected, excluded);
        SelectBacklog(backlog, velocity, selected, excluded);

        var planned = AssignOwners(selected, active, byKey);
        var members = Capacities(active, velocity, planned);
        var total = planned.Sum(p => p.Points);
        var level = total > velocity.High ? CommitmentLevel.Over : total < velocity.Low ? CommitmentLevel.Under : CommitmentLevel.Within;
        Warn(level, total, velocity, members, warnings);
        return new PlanningProposal(velocity, members, planned, excluded, total, level, warnings);
    }

    private static VelocityRange VelocityFrom(SprintSnapshot sprint, IReadOnlyList<double> completedPoints, List<string> warnings)
    {
        var recent = completedPoints.TakeLast(VelocitySprints).ToList();
        if (recent.Count > 0)
        {
            return new VelocityRange(recent.Min(), recent.Max());
        }

        // Without history the only evidence is this sprint's own commitment, so say so rather than invent a range.
        var committed = sprint.Items.Where(i => PlannedTypes.Contains(i.Type)).Sum(i => i.StoryPoints ?? 0);
        warnings.Add($"No velocity history: capacity is this sprint's {committed:0.#} committed points.");
        return new VelocityRange(committed, committed);
    }

    private static void SelectCarryOver(SprintSnapshot sprint, List<(WorkItem, PlanningSource)> selected, List<ExcludedItem> excluded)
    {
        foreach (var item in sprint.Items.Where(i => !i.IsDone && PlannedTypes.Contains(i.Type)))
        {
            if (item.IsInProgress)
            {
                selected.Add((item, PlanningSource.CarryOver));
            }
            else
            {
                excluded.Add(new ExcludedItem(item.Id.Value, item.Title, item.StoryPoints, ["Not started in the current sprint; it stays in that sprint's backlog until you move it."]));
            }
        }
    }

    // Backlog order is respected: once a ready item does not fit, every later ready item is "beyond capacity" rather than skipped over.
    private static void SelectBacklog(IReadOnlyList<WorkItem> backlog, VelocityRange velocity, List<(WorkItem Item, PlanningSource Source)> selected, List<ExcludedItem> excluded)
    {
        var total = selected.Sum(s => s.Item.StoryPoints ?? 0);
        var full = false;
        foreach (var item in backlog)
        {
            var reasons = NotReadyReasons(item);
            if (reasons.Count > 0)
            {
                excluded.Add(new ExcludedItem(item.Id.Value, item.Title, item.StoryPoints, reasons));
                continue;
            }

            var points = item.StoryPoints!.Value;
            if (!full && total + points <= velocity.High)
            {
                selected.Add((item, PlanningSource.Backlog));
                total += points;
                continue;
            }

            full = true;
            excluded.Add(new ExcludedItem(item.Id.Value, item.Title, item.StoryPoints, [$"Beyond capacity: {total:0.#} of {velocity.High:0.#} points are already planned."]));
        }
    }

    /// <summary>Lists why a backlog item is not ready to plan (Definition of Ready: estimated, acceptance criteria, small enough).</summary>
    /// <param name="item">The backlog item.</param>
    /// <returns>The reasons; empty when ready.</returns>
    public static IReadOnlyList<string> NotReadyReasons(WorkItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var reasons = new List<string>();
        if (item.StoryPoints is null or <= 0)
        {
            reasons.Add("No story points");
        }
        else if (item.StoryPoints > MaxReadyPoints)
        {
            reasons.Add($"Too large ({item.StoryPoints:0.#} points): split it first");
        }

        if (string.IsNullOrWhiteSpace(item.AcceptanceCriteria))
        {
            reasons.Add("No acceptance criteria");
        }

        return reasons;
    }

    // An owned item counts against its owner first; unowned items then go to the least-loaded member, the same rule as the advice page.
    private static List<PlannedItem> AssignOwners(List<(WorkItem Item, PlanningSource Source)> selected, List<Member> active, Dictionary<string, Member> byKey)
    {
        var points = active.ToDictionary(m => m.Id, _ => 0.0);
        foreach (var (item, _) in selected.Where(s => OwnerOf(s.Item, byKey) is not null))
        {
            points[OwnerOf(item, byKey)!.Id] += item.StoryPoints ?? 0;
        }

        var result = new List<PlannedItem>();
        foreach (var (item, source) in selected)
        {
            var owner = OwnerOf(item, byKey);
            var suggested = false;
            if (owner is null && active.Count > 0)
            {
                owner = LeastLoaded(active.Select(m => (m.Id, m.DisplayName)), points) is { } pick ? active.First(m => m.Id == pick.Id) : null;
                suggested = owner is not null;
                if (owner is not null)
                {
                    points[owner.Id] += item.StoryPoints ?? 0;
                }
            }

            result.Add(new PlannedItem(item.Id.Value, item.Type, item.Title, item.StoryPoints ?? 0, source, owner?.Id, owner?.DisplayName, suggested));
        }

        return result;
    }

    private static Member? OwnerOf(WorkItem item, Dictionary<string, Member> byKey) =>
        item.OwnerKey is not null && byKey.TryGetValue(item.OwnerKey, out var m) ? m : null;

    private static List<MemberCapacity> Capacities(List<Member> active, VelocityRange velocity, List<PlannedItem> planned)
    {
        if (active.Count == 0)
        {
            return [];
        }

        // Members share the team's velocity equally; availability scales each share (a member at 50 percent has half a share).
        var share = 1.0 / active.Count;
        return active
            .Select(m =>
            {
                var availability = Math.Clamp(m.AvailabilityPct, 0, 100) / 100.0;
                return new MemberCapacity(
                    m.Id,
                    m.DisplayName,
                    Math.Clamp(m.AvailabilityPct, 0, 100),
                    Math.Round(velocity.Low * share * availability, 1),
                    Math.Round(velocity.High * share * availability, 1),
                    planned.Where(p => p.OwnerMemberId == m.Id).Sum(p => p.Points));
            })
            .ToList();
    }

    private static void Warn(CommitmentLevel level, double total, VelocityRange velocity, List<MemberCapacity> members, List<string> warnings)
    {
        if (level == CommitmentLevel.Over)
        {
            warnings.Add($"Over-committed: {total:0.#} points planned against a velocity range of {velocity.Low:0.#} to {velocity.High:0.#}. Remove something or move carry-over.");
        }
        else if (level == CommitmentLevel.Under)
        {
            warnings.Add($"Under-committed: {total:0.#} points planned against a velocity range of {velocity.Low:0.#} to {velocity.High:0.#}. There is room for more ready items.");
        }

        warnings.AddRange(members.Where(m => m.Overloaded).Select(m => $"{m.DisplayName} has {m.PlannedPoints:0.#} points planned against a capacity of {m.CapacityHigh:0.#}."));
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
            var pick = LeastLoaded(loads.Select(l => (l.MemberId, l.DisplayName)), points);
            if (pick is null)
            {
                break;
            }

            result.Add(new AssignmentSuggestion(item.Id.Value, item.Title, pick.Value.Id, pick.Value.Name, $"{pick.Value.Name} has the fewest open story points ({points[pick.Value.Id]:0.#})."));
            points[pick.Value.Id] += item.StoryPoints ?? 0;
        }

        return result;
    }

    // The one suggested-owner rule: fewest open points, ties broken by name so the result is stable.
    private static (string Id, string Name)? LeastLoaded(IEnumerable<(string Id, string Name)> members, Dictionary<string, double> points)
    {
        var pick = members.OrderBy(m => points[m.Id]).ThenBy(m => m.Name, StringComparer.Ordinal).Select(m => ((string, string)?)m).FirstOrDefault();
        return pick is { } p ? (p.Item1, p.Item2) : null;
    }
}
