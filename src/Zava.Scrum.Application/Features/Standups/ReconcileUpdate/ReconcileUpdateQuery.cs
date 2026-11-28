using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Features.Standups.ReconcileUpdate;

/// <summary>Compares a member's spoken update with their brief. A query: it records nothing; the caller records returned events.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MemberId">Member id.</param>
/// <param name="Utterance">What they said.</param>
/// <param name="Asked">The clarifying question this utterance answers, if any, so the answer is attributed to that item.</param>
/// <param name="FocusItemId">The item the asked question was about; an answer that names no item is attributed to it.</param>
/// <param name="Correction">True when the member is correcting an earlier statement.</param>
/// <param name="Covered">Work item ids already discussed in this standup; questions never ask about them again.</param>
/// <param name="AnalysedItemIds">Items the utterance analysis linked, added to the ones found by rules.</param>
/// <param name="Comments">The classified comments from the utterance analysis. <see langword="null"/> when no analysis ran (the reconcile model then proposes the events); a list, even an empty one, is authoritative.</param>
public sealed record ReconcileUpdateQuery(Guid SessionId, string MemberId, string Utterance, string? Asked = null, IReadOnlyCollection<int>? Covered = null, int? FocusItemId = null, bool Correction = false, IReadOnlyList<int>? AnalysedItemIds = null, IReadOnlyList<ItemComment>? Comments = null) : IQuery<ReconcileResult>
{
    /// <summary>Gets whether the caller needs the model's consistency and risk assessment in addition to authoritative comments.</summary>
    /// <remarks>Corrections and updates without authoritative comments always ask the model. When false, consistency and risks retain the existing unassessed fallback values; voice facilitation consumes neither (ADR 0044).</remarks>
    public bool IncludeModelAssessment { get; init; } = true;
}
