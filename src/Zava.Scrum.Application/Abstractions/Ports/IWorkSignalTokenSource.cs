using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Exchanges only the named signal owner's validated assertion; never falls back to an application identity.</summary>
public interface IWorkSignalTokenSource
{
    /// <summary>Obtains request-local delegated Work IQ access without storing a token.</summary>
    /// <param name="assertion">The current owner's validated portal assertion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Delegated access or a visible sign-in/configuration failure.</returns>
    Task<Result<DelegatedWorkSignalAccess>> ExchangeAsync(UserAssertion assertion, CancellationToken ct);
}
