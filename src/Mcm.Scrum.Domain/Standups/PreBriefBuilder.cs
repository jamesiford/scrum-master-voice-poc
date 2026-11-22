using Mcm.Scrum.Domain.Flags;
using Mcm.Scrum.Domain.Kpis;
using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>Builds the factual part of a <see cref="PreBrief"/>: ownership, flags, burndown and team risks. No model involved.</summary>
public sealed class PreBriefBuilder(FlagEvaluator evaluator)
{
    /// <summary>Builds a pre-brief with deterministic questions; a model may later replace the questions and summary.</summary>
    /// <param name="team">The team.</param>
    /// <param name="members">The roster.</param>
    /// <param name="sprint">The current sprint.</param>
    /// <param name="now">The current time.</param>
    /// <param name="carryOver">Commitments and unblockers from earlier standups, if any.</param>
    /// <returns>The pre-brief.</returns>
    public PreBrief Build(Team team, IReadOnlyList<Member> members, SprintSnapshot sprint, DateTimeOffset now, CarryOver? carryOver = null)
    {
        ArgumentNullException.ThrowIfNull(team);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(sprint);

        carryOver ??= CarryOver.None;
        var context = new FlagContext(now, team.Behaviours);
        var missed = CommitmentCarryOver.Missed(carryOver).Select(m => m.Commitment.WorkItemId).ToHashSet();
        var open = sprint.Items.Where(i => !i.IsDone).Select(i => ToBriefItem(i, context, now, missed)).ToList();
        var byId = sprint.Items.ToDictionary(i => i.Id);
        var burndown = BurndownCalculator.Calculate(sprint, now);

        var memberBriefs = members
            .Where(m => m.Active)
            .OrderBy(m => m.SpeakingOrder)
            .Select(m => ToMemberBrief(m, open, byId, carryOver, members))
            .ToList();
        var unassigned = open.Where(i => i.Flags.Contains(Flag.Unassigned)).ToList();
        var risks = BuildRisks(open, unassigned, burndown);
        risks.AddRange(CarryOverRisks(open, carryOver, members));

        return new PreBrief(
            team.Id,
            sprint.IterationPath,
            team.SprintGoal,
            burndown.DaysLeft,
            burndown,
            memberBriefs,
            unassigned,
            risks,
            BuildSummary(burndown, risks));
    }

    private BriefItem ToBriefItem(WorkItem item, FlagContext context, DateTimeOffset now, HashSet<int> missed) =>
        new(item.Id, item.Title, item.Type, item.State, Math.Max(0, (int)(now - item.StateChangedAt).TotalDays), CommitmentCarryOver.WithMissed(evaluator.Evaluate(item, context), missed.Contains(item.Id.Value)));

    private static MemberBrief ToMemberBrief(Member member, List<BriefItem> open, Dictionary<WorkItemId, WorkItem> byId, CarryOver carryOver, IReadOnlyList<Member> roster)
    {
        var items = open.Where(i => Owns(member, byId[i.Id])).ToList();
        var previous = carryOver.Commitments.TryGetValue(member.Id, out var p) ? p.Where(c => items.Any(i => i.Id.Value == c.WorkItemId)).ToList() : [];
        var help = carryOver.Blocked.Where(b => b.UnblockerMemberId == member.Id).Select(b => HelpFor(b, open, byId, roster, member)).OfType<HelpRequest>().ToList();
        return new MemberBrief(member.Id, member.DisplayName, items, FallbackQuestions.For(items), previous, help);
    }

    private static HelpRequest? HelpFor(StillBlocked blocked, List<BriefItem> open, Dictionary<WorkItemId, WorkItem> byId, IReadOnlyList<Member> roster, Member helper)
    {
        var item = open.FirstOrDefault(i => i.Id.Value == blocked.WorkItemId && i.Flags.Contains(Flag.Blocked));
        if (item is null)
        {
            return null;
        }

        var owner = roster.FirstOrDefault(m => m.Id != helper.Id && Owns(m, byId[item.Id]));
        return new HelpRequest(item.Id.Value, item.Type, item.Title, owner?.DisplayName ?? "a teammate");
    }

    // DEMO: the sandbox org has one real user, so demo-owner-{key} tags map items to the cast; PROD: AssignedTo only.
    private static bool Owns(Member member, WorkItem item) =>
        item.OwnerKey is not null
            ? string.Equals(item.OwnerKey, member.Key, StringComparison.OrdinalIgnoreCase)
            : member.AdoUniqueName is not null && string.Equals(item.AssignedTo, member.AdoUniqueName, StringComparison.OrdinalIgnoreCase);

    private static List<string> BuildRisks(List<BriefItem> open, List<BriefItem> unassigned, Burndown burndown)
    {
        var risks = new List<string>();
        if (unassigned.Count > 0)
        {
            risks.Add($"{unassigned.Count} item{(unassigned.Count == 1 ? " is" : "s are")} unassigned");
        }

        risks.AddRange(open.Where(i => i.Flags.Contains(Flag.Blocked)).Select(i => $"{i.Type} {i.Id} blocked for {i.DaysInState} days"));
        risks.AddRange(open
            .Where(i => i.Flags.Contains(Flag.Stale) && !i.Flags.Contains(Flag.Blocked))
            .Select(i => $"{i.Type} {i.Id} has been {i.State} for {i.DaysInState} days"));
        if (burndown.AtRisk)
        {
            risks.Add($"Burndown at risk: {burndown.RemainingPoints:0.#} points left with {burndown.DaysLeft} days to go");
        }

        return risks;
    }

    private static IEnumerable<string> CarryOverRisks(List<BriefItem> open, CarryOver carryOver, IReadOnlyList<Member> members)
    {
        foreach (var b in carryOver.Blocked)
        {
            var item = open.FirstOrDefault(i => i.Id.Value == b.WorkItemId && i.Flags.Contains(Flag.Blocked));
            var helper = members.FirstOrDefault(m => m.Id == b.UnblockerMemberId);
            if (item is not null && helper is not null)
            {
                yield return $"{item.Type} {item.Id} is still blocked; {helper.DisplayName} was named to help";
            }
        }

        foreach (var (memberId, commitment) in CommitmentCarryOver.Missed(carryOver))
        {
            var name = members.FirstOrDefault(m => m.Id == memberId)?.DisplayName ?? memberId;
            yield return $"{name} committed to item {commitment.WorkItemId} {commitment.StandupsAgo} standups ago and it is still open";
        }
    }

    private static string BuildSummary(Burndown burndown, List<string> risks) =>
        $"{burndown.CompletedPoints:0.#} of {burndown.CommittedPoints:0.#} points are done with {burndown.DaysLeft} days left. " +
        (risks.Count == 0 ? "No team-level risks." : $"{risks.Count} risk{(risks.Count == 1 ? string.Empty : "s")} to discuss.");

    /// <summary>Replaces questions and the summary after grounding, leaving every fact untouched.</summary>
    /// <param name="brief">The deterministic brief.</param>
    /// <param name="summary">Replacement summary, or <see langword="null"/> to keep it.</param>
    /// <param name="questions">Replacement questions by member id.</param>
    /// <returns>The updated brief.</returns>
    public static PreBrief WithSuggestions(PreBrief brief, string? summary, IReadOnlyDictionary<string, string> questions)
    {
        ArgumentNullException.ThrowIfNull(brief);
        ArgumentNullException.ThrowIfNull(questions);
        var members = brief.Members
            .Select(m => questions.TryGetValue(m.MemberId, out var q) ? m with { SuggestedQuestion = q } : m)
            .ToList();
        return brief with { Members = members, Summary = summary ?? brief.Summary };
    }
}
