using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>Raised when a standup has been prepared. A handler sends the pre-brief card.</summary>
/// <param name="SessionId">The session.</param>
/// <param name="OccurredAt">When.</param>
public sealed record StandupPrepared(Guid SessionId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised once when a standup ends. Handlers post the recap and send the follow-ups.</summary>
/// <param name="SessionId">The session.</param>
/// <param name="OccurredAt">When.</param>
public sealed record StandupEnded(Guid SessionId, DateTimeOffset OccurredAt) : IDomainEvent;
