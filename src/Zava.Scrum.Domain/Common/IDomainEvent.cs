namespace Mcm.Scrum.Domain.Common;

/// <summary>A fact that happened in the domain, dispatched after the aggregate is saved.</summary>
public interface IDomainEvent
{
    /// <summary>Gets when the event occurred.</summary>
    DateTimeOffset OccurredAt { get; }
}
