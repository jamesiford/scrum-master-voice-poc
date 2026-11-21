using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.GetSession;

/// <summary>Reads one session with its pre-brief.</summary>
/// <param name="SessionId">Session id.</param>
public sealed record GetSessionQuery(Guid SessionId) : IQuery<SessionDto>;
