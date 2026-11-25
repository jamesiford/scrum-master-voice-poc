using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

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

/// <summary>
/// Everything a write needs to be made as the approver: who they are and how to get their Azure DevOps token. The writer asks for the token
/// when it writes, so there is no way to write without one, and no overload of the port exists without this context (ADR 0020).
/// </summary>
/// <param name="Approver">The approver recorded on the approval record; Azure DevOps shows them as the author.</param>
/// <param name="GetAccessAsync">Gets the approver's delegated access, or the reason it cannot be had (consent, Conditional Access, no sign-in).</param>
public sealed record DelegatedWriteContext(ActorIdentity Approver, Func<CancellationToken, Task<Result<DelegatedAdoAccess>>> GetAccessAsync);
