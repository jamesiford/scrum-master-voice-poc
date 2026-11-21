using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>An event the model suggests recording from an update.</summary>
/// <param name="Type">commitment, blocker or decision.</param>
/// <param name="Text">Short description.</param>
/// <param name="WorkItemIds">Work items referred to (unverified until grounded).</param>
public sealed record SuggestedEvent(StandupEventType Type, string Text, IReadOnlyList<int> WorkItemIds);

/// <summary>Model output for a member's update (SPEC 10.2). Everything here is unverified until the handler grounds it.</summary>
/// <param name="Consistent">Whether the update matches the brief.</param>
/// <param name="MentionedWorkItemIds">Ids the member referred to.</param>
/// <param name="Risks">Short risk statements.</param>
/// <param name="SuggestedQuestion">One question to ask, if any.</param>
/// <param name="Events">Events worth recording.</param>
public sealed record ReconcileSuggestion(
    bool Consistent,
    IReadOnlyList<int> MentionedWorkItemIds,
    IReadOnlyList<string> Risks,
    string? SuggestedQuestion,
    IReadOnlyList<SuggestedEvent> Events);
