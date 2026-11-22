using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Planning;

/// <summary>Raised when a planning pack is prepared. A handler sends its card to the Scrum Master.</summary>
/// <param name="PackId">The pack.</param>
/// <param name="OccurredAt">When it was prepared.</param>
public sealed record PlanningPrepared(Guid PackId, DateTimeOffset OccurredAt) : IDomainEvent;
