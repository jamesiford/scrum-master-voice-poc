using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Mcm.Scrum.Application.Behaviors;

/// <summary>Innermost behaviour: maps unexpected exceptions to <see cref="Error.Unexpected"/> and logs them once.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class ExceptionMappingBehavior<TRequest, TResponse>(ILogger<ExceptionMappingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResultFactory<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unhandled exception in {Request}", typeof(TRequest).Name);
            return TResponse.Failure(Error.Unexpected("An unexpected error occurred."));
        }
    }
}
