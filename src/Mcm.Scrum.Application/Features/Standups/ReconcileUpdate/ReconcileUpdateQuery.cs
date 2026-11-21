using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.ReconcileUpdate;

/// <summary>Compares a member's spoken update with their brief. A query: it records nothing; the caller records returned events.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MemberId">Member id.</param>
/// <param name="Utterance">What they said.</param>
public sealed record ReconcileUpdateQuery(Guid SessionId, string MemberId, string Utterance) : IQuery<ReconcileResult>;
