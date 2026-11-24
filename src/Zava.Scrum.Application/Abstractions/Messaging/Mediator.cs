using System.Collections.Concurrent;
using Zava.Scrum.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Zava.Scrum.Application.Abstractions.Messaging;

/// <summary>
/// Hand-rolled mediator (SPEC 6.1.3). Closed-generic wrappers are built once per type and cached,
/// so there is no per-call reflection after the first dispatch.
/// </summary>
/// <remarks>
/// PROD: a failing notification handler is only logged; production would use an outbox for delivery guarantees.
/// </remarks>
internal sealed class Mediator(IServiceProvider services, ILogger<Mediator> logger) : ISender, IPublisher
{
    private static readonly ConcurrentDictionary<Type, object> RequestWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationWrapper> NotificationWrappers = new();

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var wrapper = (RequestWrapper<TResponse>)RequestWrappers.GetOrAdd(request.GetType(), static type =>
            Activator.CreateInstance(typeof(RequestWrapper<,>).MakeGenericType(type, typeof(TResponse)))!);
        return wrapper.Handle(request, services, ct);
    }

    /// <inheritdoc />
    public async Task Publish(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var wrapper = NotificationWrappers.GetOrAdd(domainEvent.GetType(), static type =>
            (NotificationWrapper)Activator.CreateInstance(typeof(NotificationWrapper<>).MakeGenericType(type))!);
        await wrapper.Handle(domainEvent, services, logger, ct);
    }

    private abstract class RequestWrapper<TResponse>
    {
        public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct);
    }

    private sealed class RequestWrapper<TRequest, TResponse> : RequestWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct)
        {
            var handler = sp.GetService<IRequestHandler<TRequest, TResponse>>()
                ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}.");
            var typed = (TRequest)request;
            var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

            Func<Task<TResponse>> pipeline = () => handler.Handle(typed, ct);
            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var next = pipeline;
                pipeline = () => behavior.Handle(typed, next, ct);
            }

            return pipeline();
        }
    }

    private abstract class NotificationWrapper
    {
        public abstract Task Handle(IDomainEvent domainEvent, IServiceProvider sp, ILogger logger, CancellationToken ct);
    }

    private sealed class NotificationWrapper<TEvent> : NotificationWrapper
        where TEvent : IDomainEvent
    {
        public override async Task Handle(IDomainEvent domainEvent, IServiceProvider sp, ILogger logger, CancellationToken ct)
        {
            foreach (var handler in sp.GetServices<INotificationHandler<TEvent>>())
            {
                try
                {
                    logger.LogDebug("Notification {Event} -> {Handler}", typeof(TEvent).Name, handler.GetType().Name);
                    await handler.Handle((TEvent)domainEvent, ct);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Notification handler {Handler} cancelled for {Event}; remaining handlers skipped", handler.GetType().Name, typeof(TEvent).Name);
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Notification handler {Handler} failed for {Event}", handler.GetType().Name, typeof(TEvent).Name);
                }
            }
        }
    }
}
