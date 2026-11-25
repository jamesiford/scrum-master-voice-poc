using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>An Azure DevOps access token issued for one person, used for one write. Never logged and never stored.</summary>
/// <param name="AccessToken">The delegated access token.</param>
public sealed record DelegatedAdoAccess(string AccessToken);

/// <summary>
/// Exchanges the signed-in person's token for an Azure DevOps token as that person (on-behalf-of). It never falls back to the
/// service identity: when the exchange fails the write does not happen (ADR 0020).
/// </summary>
public interface IApproverTokenSource
{
    /// <summary>Exchanges the assertion for an Azure DevOps access token.</summary>
    /// <param name="assertion">The approver's validated token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The delegated access, or an <see cref="ErrorType.ExternalRejected"/> failure that says what to do next.</returns>
    Task<Result<DelegatedAdoAccess>> ExchangeAsync(UserAssertion assertion, CancellationToken ct);
}
