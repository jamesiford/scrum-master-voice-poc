namespace Mcm.Scrum.Application.Abstractions.Messaging;

/// <summary>Handles exactly one request type.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handles the request.</summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
