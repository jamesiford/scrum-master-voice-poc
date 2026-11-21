using System.Reflection;
using Mcm.Scrum.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mcm.Scrum.Application.Abstractions.Messaging;

/// <summary>Registers the mediator and scans an assembly for handlers. Reflection happens only here, at startup.</summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ISender"/>, <see cref="IPublisher"/>, and every handler found in <paramref name="assembly"/>.
    /// Fails fast when a request has no handler or more than one.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">Assembly to scan.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="InvalidOperationException">A request has zero or several handlers.</exception>
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return services.AddMediator(assembly.GetTypes());
    }

    /// <summary>Same as <see cref="AddMediator(IServiceCollection, Assembly)"/> for an explicit set of candidate types (used by tests).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="candidates">Candidate request, handler and notification types.</param>
    /// <returns>The same collection.</returns>
    internal static IServiceCollection AddMediator(this IServiceCollection services, IEnumerable<Type> candidates)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(candidates);

        services.AddScoped<Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<Mediator>());
        services.AddScoped<IPublisher>(sp => sp.GetRequiredService<Mediator>());

        var types = candidates.Where(t => t is { IsClass: true, IsAbstract: false }).ToArray();
        var handled = RegisterRequestHandlers(services, types);
        EnsureEveryRequestHasAHandler(types, handled);
        RegisterNotificationHandlers(services, types);
        return services;
    }

    private static HashSet<Type> RegisterRequestHandlers(IServiceCollection services, Type[] types)
    {
        var handled = new HashSet<Type>();
        foreach (var type in types)
        {
            foreach (var contract in type.GetInterfaces().Where(i => IsGeneric(i, typeof(IRequestHandler<,>))))
            {
                var request = contract.GetGenericArguments()[0];
                if (!handled.Add(request))
                {
                    throw new InvalidOperationException($"More than one handler is registered for {request.Name}.");
                }

                services.AddScoped(contract, type);
            }
        }

        return handled;
    }

    private static void EnsureEveryRequestHasAHandler(Type[] types, HashSet<Type> handled)
    {
        var missing = types
            .Where(t => t.GetInterfaces().Any(i => IsGeneric(i, typeof(IRequest<>))) && !handled.Contains(t))
            .Select(t => t.Name)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"No handler registered for: {string.Join(", ", missing)}.");
        }
    }

    private static void RegisterNotificationHandlers(IServiceCollection services, Type[] types)
    {
        foreach (var type in types)
        {
            foreach (var contract in type.GetInterfaces().Where(i => IsGeneric(i, typeof(INotificationHandler<>))))
            {
                services.AddScoped(contract, type);
            }
        }
    }

    private static bool IsGeneric(Type type, Type definition) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == definition;
}
