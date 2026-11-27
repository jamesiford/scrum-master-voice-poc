using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Standups.GetSessionLatency;

/// <summary>Reads measured hop percentiles for a team-owned session without contacting a model.</summary>
/// <param name="SessionId">The session whose persisted timing events are summarised.</param>
public sealed record GetSessionLatencyQuery(Guid SessionId) : IQuery<SessionLatencyDto>;
