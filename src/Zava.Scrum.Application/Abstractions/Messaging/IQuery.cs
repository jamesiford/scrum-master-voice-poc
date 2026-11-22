using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Abstractions.Messaging;

/// <summary>Marker for a side-effect-free read.</summary>
/// <typeparam name="TResponse">The read model type.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
