using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Abstractions.Messaging;

/// <summary>Reacts to a domain event. Implementations must be idempotent.</summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface INotificationHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>Handles the event.</summary>
    /// <param name="domainEvent">The event.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task Handle(TEvent domainEvent, CancellationToken ct);
}
