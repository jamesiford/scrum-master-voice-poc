using FluentValidation;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application;

/// <summary>Registers the Application layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the mediator, handlers, validators and pipeline behaviours.
    /// Behaviour order is fixed: Logging, Validation, ExceptionMapping (outermost first).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediator(assembly);
        services.AddScoped<Tools.IScrumTools, Tools.ScrumTools>();
        services.AddScoped<Features.Calendars.SprintCalendarBuilder>();
        services.AddScoped<Features.Identity.ApprovalGate>();
        services.AddScoped<Features.Teams.TeamRegistration>();
        services.AddScoped<Features.Teams.ITeamResolver, Features.Teams.TeamResolver>();
        services.AddScoped<Features.Teams.IResourceTeamLocator, Features.Teams.ResourceTeamLocator>();
        services.AddScoped<Features.Identity.RosterIdentityResolver>();
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        return services;
    }
}
