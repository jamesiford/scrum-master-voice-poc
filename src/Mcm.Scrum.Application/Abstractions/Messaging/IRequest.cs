namespace Mcm.Scrum.Application.Abstractions.Messaging;

/// <summary>A request dispatched through <see cref="ISender"/>.</summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequest<out TResponse>;
