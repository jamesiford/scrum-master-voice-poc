namespace Mcm.Scrum.Domain.Standups;

/// <summary>One thing that happened in a standup.</summary>
/// <param name="Id">Event id.</param>
/// <param name="At">When it happened.</param>
/// <param name="Type">Kind.</param>
/// <param name="MemberId">The member it concerns, or <see langword="null"/> for the agent or the team.</param>
/// <param name="WorkItemIds">Work items it refers to (always from the sprint).</param>
/// <param name="Retracted">True when the speaker corrected themselves; retracted events are kept for the audit trail but ignored by every rule.</param>
/// <param name="Text">What was said or decided.</param>
public sealed record StandupEvent(
    Guid Id,
    DateTimeOffset At,
    StandupEventType Type,
    string? MemberId,
    IReadOnlyList<int> WorkItemIds,
    string Text,
    bool Retracted = false);
