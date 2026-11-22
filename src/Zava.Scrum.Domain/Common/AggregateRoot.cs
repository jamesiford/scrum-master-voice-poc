namespace Zava.Scrum.Domain.Common;

/// <summary>Base class for aggregates that raise domain events.</summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Gets events raised since the aggregate was loaded or last cleared.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>Removes all pending events, normally after they were published.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Records an event for later dispatch.</summary>
    /// <param name="domainEvent">The event.</param>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
