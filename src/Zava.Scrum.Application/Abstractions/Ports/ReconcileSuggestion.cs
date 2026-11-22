using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Abstractions.Ports;

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
/// <param name="Retracts">Numbers (from the recorded statements given to the model) of earlier statements the member just corrected.</param>
public sealed record ReconcileSuggestion(
    bool Consistent,
    IReadOnlyList<int> MentionedWorkItemIds,
    IReadOnlyList<string> Risks,
    string? SuggestedQuestion,
    IReadOnlyList<SuggestedEvent> Events,
    IReadOnlyList<int>? Retracts = null);

/// <summary>An earlier statement shown to the model so it can tell which one a correction refers to.</summary>
/// <param name="Ref">Number the model uses to refer to it.</param>
/// <param name="Type">Commitment, blocker or decision.</param>
/// <param name="Text">What was recorded.</param>
public sealed record RecordedStatement(int Ref, StandupEventType Type, string Text);
