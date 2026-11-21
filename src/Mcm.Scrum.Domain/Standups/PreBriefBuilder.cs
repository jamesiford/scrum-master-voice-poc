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
    /// <returns>The pre-brief.</returns>
    public PreBrief Build(Team team, IReadOnlyList<Member> members, SprintSnapshot sprint, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(team);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(sprint);

        var context = new FlagContext(now, team.Behaviours);
        var open = sprint.Items.Where(i => !i.IsDone).Select(i => ToBriefItem(i, context, now)).ToList();
        var byId = sprint.Items.ToDictionary(i => i.Id);
        var burndown = BurndownCalculator.Calculate(sprint, now);

        var memberBriefs = members
            .Where(m => m.Active)
            .OrderBy(m => m.SpeakingOrder)
            .Select(m => ToMemberBrief(m, open, byId))
            .ToList();
        var unassigned = open.Where(i => i.Flags.Contains(Flag.Unassigned)).ToList();
        var risks = BuildRisks(open, unassigned, burndown);

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

    private BriefItem ToBriefItem(WorkItem item, FlagContext context, DateTimeOffset now) =>
        new(item.Id, item.Title, item.Type, item.State, Math.Max(0, (int)(now - item.StateChangedAt).TotalDays), evaluator.Evaluate(item, context));

    private static MemberBrief ToMemberBrief(Member member, List<BriefItem> open, Dictionary<WorkItemId, WorkItem> byId)
    {
        var items = open.Where(i => Owns(member, byId[i.Id])).ToList();
        return new MemberBrief(member.Id, member.DisplayName, items, FallbackQuestions.For(items));
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
