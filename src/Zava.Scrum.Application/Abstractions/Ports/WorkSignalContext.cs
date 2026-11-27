using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Request-local delegated context; never persist or log its assertion (ADR 0027).</summary>
/// <param name="Team">The membership-selected team.</param>
/// <param name="OwnerAssertion">The named owner's current validated assertion, exchanged by the real adapter through OBO.</param>
/// <param name="Scope">Code-resolved channel, meetings, board and roster.</param>
/// <param name="CorrelationId">The team's request/ceremony correlation.</param>
public sealed record WorkSignalContext(Team Team, UserAssertion OwnerAssertion, WorkSignalScope Scope, Guid CorrelationId);
