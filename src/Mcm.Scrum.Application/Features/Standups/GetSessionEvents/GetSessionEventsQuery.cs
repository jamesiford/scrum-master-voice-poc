using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Standups.GetSessionEvents;

/// <summary>Reads a session's events in order.</summary>
/// <param name="SessionId">Session id.</param>
public sealed record GetSessionEventsQuery(Guid SessionId) : IQuery<IReadOnlyList<StandupEvent>>;
