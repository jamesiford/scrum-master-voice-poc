using FluentValidation;
using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Behaviors;

/// <summary>Runs FluentValidation validators and short-circuits with an invalid <see cref="Result"/>.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResultFactory<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var failures = new List<string>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, ct);
            failures.AddRange(result.Errors.Select(e => e.ErrorMessage));
        }

        return failures.Count == 0
            ? await next()
            : TResponse.Failure(Error.Invalid("validation.failed", string.Join("; ", failures)));
    }
}
