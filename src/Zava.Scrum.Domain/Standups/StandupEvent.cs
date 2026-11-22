namespace Mcm.Scrum.Domain.Standups;

/// <summary>One thing that happened in a standup.</summary>
/// <param name="Id">Event id.</param>
/// <param name="At">When it happened.</param>
/// <param name="Type">Kind.</param>
/// <param name="MemberId">The member it concerns, or <see langword="null"/> for the agent or the team.</param>
/// <param name="WorkItemIds">Work items it refers to (always from the sprint).</param>
/// <param name="Retracted">True when the speaker corrected themselves; retracted events are kept for the audit trail but ignored by every rule.</param>
/// <param name="Text">What was said or decided.</param>
/// <param name="UnblockerMemberId">For a blocker, the roster member named as able to help (M9-R2).</param>
/// <param name="Category">For a retrospective note, its column; <see langword="null"/> until the agent has asked and been answered.</param>
/// <param name="OwnerMemberId">For a retrospective action item, the roster member who owns it.</param>
public sealed record StandupEvent(
    Guid Id,
    DateTimeOffset At,
    StandupEventType Type,
    string? MemberId,
    IReadOnlyList<int> WorkItemIds,
    string Text,
    bool Retracted = false,
    string? UnblockerMemberId = null,
    RetroCategory? Category = null,
    string? OwnerMemberId = null);
