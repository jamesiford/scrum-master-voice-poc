using Zava.Scrum.Domain.Kpis;

namespace Zava.Scrum.Domain.Standups;

/// <summary>Everything the facilitator needs before a standup (SPEC 10.2). Facts and numbers are built in code.</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="IterationPath">Current iteration.</param>
/// <param name="SprintGoal">Sprint goal.</param>
/// <param name="DaysLeft">Whole days left in the sprint.</param>
/// <param name="Burndown">Burndown figures.</param>
/// <param name="Members">Per-member briefs, in speaking order.</param>
/// <param name="UnassignedItems">Open items nobody owns.</param>
/// <param name="TeamRisks">Team-level risks as short sentences.</param>
/// <param name="Summary">Two-sentence team summary.</param>
public sealed record PreBrief(
    string TeamId,
    string IterationPath,
    string SprintGoal,
    int DaysLeft,
    Burndown Burndown,
    IReadOnlyList<MemberBrief> Members,
    IReadOnlyList<BriefItem> UnassignedItems,
    IReadOnlyList<string> TeamRisks,
    string Summary);
