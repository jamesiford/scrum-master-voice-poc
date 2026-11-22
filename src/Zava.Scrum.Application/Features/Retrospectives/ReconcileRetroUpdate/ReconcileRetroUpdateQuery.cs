using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Features.Standups.ReconcileUpdate;

namespace Mcm.Scrum.Application.Features.Retrospectives.ReconcileRetroUpdate;

/// <summary>
/// The retrospective's counterpart of <c>ReconcileUpdateQuery</c>: sorts what a member said into notes or an action item and returns the one
/// question to ask. Rule based, so it needs no model and is safe to run speculatively. It records nothing; the caller records the returned events.
/// </summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MemberId">The member who spoke.</param>
/// <param name="Utterance">What they said.</param>
/// <param name="Answering">True when the utterance answers the agent's question.</param>
/// <param name="Correction">True when the member is correcting themselves.</param>
public sealed record ReconcileRetroUpdateQuery(Guid SessionId, string MemberId, string Utterance, bool Answering = false, bool Correction = false) : IQuery<ReconcileResult>;
