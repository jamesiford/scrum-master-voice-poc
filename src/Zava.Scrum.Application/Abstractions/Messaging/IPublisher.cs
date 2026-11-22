using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Abstractions.Messaging;

/// <summary>Publishes a domain event to zero or more handlers.</summary>
public interface IPublisher
{
    /// <summary>Publishes an event.</summary>
    /// <param name="domainEvent">The event.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task Publish(IDomainEvent domainEvent, CancellationToken ct = default);
}
