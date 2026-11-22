using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Abstractions.Messaging;

/// <summary>Publishes the events an aggregate raised, after it was saved.</summary>
public static class AggregateRootExtensions
{
    /// <summary>Publishes and clears pending domain events. Call after the repository save.</summary>
    /// <param name="aggregate">The aggregate.</param>
    /// <param name="publisher">The publisher.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task PublishEventsAsync(this AggregateRoot aggregate, IPublisher publisher, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentNullException.ThrowIfNull(publisher);
        var events = aggregate.DomainEvents.ToList();
        aggregate.ClearDomainEvents();
        foreach (var e in events)
        {
            await publisher.Publish(e, ct);
        }
    }
}
