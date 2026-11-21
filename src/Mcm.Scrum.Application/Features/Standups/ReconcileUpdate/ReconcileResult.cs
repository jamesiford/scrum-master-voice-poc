using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Standups.ReconcileUpdate;

/// <summary>An event worth recording, with grounded work item ids.</summary>
/// <param name="Type">Kind.</param>
/// <param name="Text">Description.</param>
/// <param name="WorkItemIds">Ids that exist in the sprint.</param>
public sealed record ReconcileEvent(StandupEventType Type, string Text, IReadOnlyList<int> WorkItemIds);

/// <summary>The grounded result of a reconcile (SPEC 10.2). Nothing here mentions an id outside the sprint.</summary>
/// <param name="Consistent">Whether the update matched the brief.</param>
/// <param name="MentionedWorkItemIds">Ids the member referred to.</param>
/// <param name="Risks">Short risk statements.</param>
/// <param name="SuggestedQuestion">One grounded question, or <see langword="null"/>.</param>
/// <param name="Events">Events to record.</param>
/// <param name="RetractedEventIds">Earlier events the member corrected; the caller retracts them.</param>
/// <param name="QuestionItemId">The work item the suggested question is about, so the next answer is attributed to it.</param>
public sealed record ReconcileResult(
    bool Consistent,
    IReadOnlyList<int> MentionedWorkItemIds,
    IReadOnlyList<string> Risks,
    string? SuggestedQuestion,
    IReadOnlyList<ReconcileEvent> Events,
    int? QuestionItemId = null,
    IReadOnlyList<Guid>? RetractedEventIds = null);
