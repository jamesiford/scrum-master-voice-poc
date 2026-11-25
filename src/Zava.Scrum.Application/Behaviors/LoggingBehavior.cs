using System.Diagnostics;
using System.Linq.Expressions;
using Zava.Scrum.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

namespace Zava.Scrum.Application.Behaviors;

/// <summary>
/// Outermost behaviour: one structured log line per request with latency in ms (demo metric). A request that carries a <c>TeamId</c> has it
/// on the log scope, the span and the duration metric, so every record can be filtered by team (ADR 0018).
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Built once per request type: a compiled read of the request's TeamId property, or null when it has none.
    private static readonly Func<TRequest, string?>? TeamIdOf = BuildTeamIdReader();

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var teamId = TeamIdOf?.Invoke(request);
        using var activity = ScrumTelemetry.Source.StartActivity(typeof(TRequest).Name);
        activity?.SetTag(ScrumTelemetry.TeamTag, teamId);
        using var scope = teamId is null ? null : logger.BeginScope(new Dictionary<string, object?> { ["TeamId"] = teamId });
        var response = await next();
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        ScrumTelemetry.UseCaseDuration.Record(elapsed, new KeyValuePair<string, object?>("use_case", typeof(TRequest).Name), new KeyValuePair<string, object?>(ScrumTelemetry.TeamTag, teamId ?? "none"));
        logger.LogInformation("Handled {Request} for team {TeamId} in {ElapsedMs} ms", typeof(TRequest).Name, teamId ?? "none", elapsed);
        return response;
    }

    private static Func<TRequest, string?>? BuildTeamIdReader()
    {
        var property = typeof(TRequest).GetProperty("TeamId");
        if (property is null || property.PropertyType != typeof(string))
        {
            return null;
        }

        var parameter = Expression.Parameter(typeof(TRequest), "request");
        return Expression.Lambda<Func<TRequest, string?>>(Expression.Property(parameter, property), parameter).Compile();
    }
}
