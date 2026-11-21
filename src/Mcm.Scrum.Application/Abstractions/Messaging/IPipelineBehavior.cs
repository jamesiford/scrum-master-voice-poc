namespace Mcm.Scrum.Application.Abstractions.Messaging;

/// <summary>Cross-cutting step wrapped around every request, in registration order (first registered is outermost).</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Runs the behaviour.</summary>
    /// <param name="request">The request.</param>
    /// <param name="next">The rest of the pipeline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}
