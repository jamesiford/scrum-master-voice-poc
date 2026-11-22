using System.Diagnostics;
using Zava.Scrum.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

namespace Zava.Scrum.Application.Behaviors;

/// <summary>Outermost behaviour: one structured log line per request with latency in ms (demo metric).</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        using var activity = ScrumTelemetry.Source.StartActivity(typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation(
            "Handled {Request} in {ElapsedMs} ms",
            typeof(TRequest).Name,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return response;
    }
}
