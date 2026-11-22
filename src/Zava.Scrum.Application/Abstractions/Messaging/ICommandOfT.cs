using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Abstractions.Messaging;

/// <summary>Marker for a request that changes state and returns a value (usually an id).</summary>
/// <typeparam name="TResponse">The value type.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
