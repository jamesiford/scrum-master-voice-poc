using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.ReconcileUpdate;

/// <summary>Compares a member's spoken update with their brief. A query: it records nothing; the caller records returned events.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MemberId">Member id.</param>
/// <param name="Utterance">What they said.</param>
/// <param name="Asked">The clarifying question this utterance answers, if any, so the answer is attributed to that item.</param>
/// <param name="Covered">Work item ids already discussed in this standup; questions never ask about them again.</param>
public sealed record ReconcileUpdateQuery(Guid SessionId, string MemberId, string Utterance, string? Asked = null, IReadOnlyCollection<int>? Covered = null) : IQuery<ReconcileResult>;
