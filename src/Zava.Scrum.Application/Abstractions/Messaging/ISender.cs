namespace Zava.Scrum.Application.Abstractions.Messaging;

/// <summary>Sends a command or query through the pipeline to its single handler.</summary>
public interface ISender
{
    /// <summary>Sends a request.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The response.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
