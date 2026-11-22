namespace Zava.Scrum.Domain.Planning;

/// <summary>What the planner proposes: the selection, what was left out and how it compares with capacity. Every figure is computed in code (rule 3).</summary>
/// <param name="Velocity">Velocity range the capacity is based on.</param>
/// <param name="Members">Capacity and planned load per member.</param>
/// <param name="Selected">Selected items: carry-over first, then ready backlog items in backlog order.</param>
/// <param name="Excluded">Items left out, with reasons.</param>
/// <param name="TotalPoints">Points of the selected items.</param>
/// <param name="Commitment">Over, under or within the velocity range.</param>
/// <param name="Warnings">Plain-language warnings (over or under commitment, overloaded members, no history).</param>
public sealed record PlanningProposal(
    VelocityRange Velocity,
    IReadOnlyList<MemberCapacity> Members,
    IReadOnlyList<PlannedItem> Selected,
    IReadOnlyList<ExcludedItem> Excluded,
    double TotalPoints,
    CommitmentLevel Commitment,
    IReadOnlyList<string> Warnings);
