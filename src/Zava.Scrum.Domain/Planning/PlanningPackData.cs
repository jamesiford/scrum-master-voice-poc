namespace Zava.Scrum.Domain.Planning;

/// <summary>Everything persisted about a <see cref="PlanningPack"/>.</summary>
/// <param name="Id">Pack id.</param>
/// <param name="TeamId">Team id.</param>
/// <param name="CreatedAt">When it was prepared.</param>
/// <param name="IterationPath">The current iteration the plan starts from.</param>
/// <param name="NextIterationPath">The iteration the plan moves items to, or <see langword="null"/> when Azure DevOps has none.</param>
/// <param name="VelocityIllustrative"><see langword="true"/> when the velocity history is the illustrative fixture.</param>
/// <param name="Proposal">The computed proposal.</param>
/// <param name="DraftGoal">The draft sprint goal (model-phrased or the code fallback).</param>
/// <param name="GoalFromModel"><see langword="true"/> when a model phrased the goal.</param>
/// <param name="Status">Life-cycle status.</param>
/// <param name="DecidedBy">Who approved or rejected it.</param>
/// <param name="DecidedAt">When.</param>
/// <param name="ApprovedItemIds">The item ids the approver selected.</param>
/// <param name="FollowUpId">The follow-up created on approval.</param>
public sealed record PlanningPackData(
    Guid Id,
    string TeamId,
    DateTimeOffset CreatedAt,
    string IterationPath,
    string? NextIterationPath,
    bool VelocityIllustrative,
    PlanningProposal Proposal,
    string DraftGoal,
    bool GoalFromModel,
    PlanningPackStatus Status,
    string? DecidedBy,
    DateTimeOffset? DecidedAt,
    IReadOnlyList<int> ApprovedItemIds,
    Guid? FollowUpId);
