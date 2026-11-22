using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Standups.GetSessionEvents;

/// <summary>Reads a session's events in order.</summary>
/// <param name="SessionId">Session id.</param>
public sealed record GetSessionEventsQuery(Guid SessionId) : IQuery<IReadOnlyList<StandupEvent>>;
