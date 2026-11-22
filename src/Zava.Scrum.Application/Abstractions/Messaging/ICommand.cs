using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Abstractions.Messaging;

/// <summary>Marker for a request that changes state. Returns a <see cref="Result"/>.</summary>
public interface ICommand : IRequest<Result>;
