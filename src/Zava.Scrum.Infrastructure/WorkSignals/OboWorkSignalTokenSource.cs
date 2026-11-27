using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Infrastructure.Delegation;

namespace Zava.Scrum.Infrastructure.WorkSignals;

/// <summary>Secretless owner-OBO Work IQ access; deliberately unregistered until the scoped live spike passes (ADR 0034).</summary>
public sealed class OboWorkSignalTokenSource(
    IOptions<DelegationOptions> options, ILogger<OboWorkSignalTokenSource> logger) : IWorkSignalTokenSource
{
    /// <inheritdoc />
    public async Task<Result<DelegatedWorkSignalAccess>> ExchangeAsync(UserAssertion assertion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(assertion.Token) ||
            assertion.TenantId.ToString() != settings.TenantId ||
            string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId))
        {
            return Result<DelegatedWorkSignalAccess>.Failure(
                Error.Conflict("signals.delegation_unavailable", "No signals available. The owner's delegated sign-in is required."));
        }

        try
        {
            var proof = new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(settings.ManagedIdentityClientId));
            var obo = new OnBehalfOfCredential(settings.TenantId, settings.ClientId,
                async token => (await proof.GetTokenAsync(new TokenRequestContext(["api://AzureADTokenExchange/.default"]), token)).Token,
                assertion.Token);
            var issued = await obo.GetTokenAsync(new TokenRequestContext(["api://workiq.svc.cloud.microsoft/.default"]), ct);
            return Result<DelegatedWorkSignalAccess>.Success(new(issued.Token));
        }
        catch (Exception ex) when (ex is AuthenticationFailedException or CredentialUnavailableException)
        {
            // Provider exception text can carry claims challenges; log classification, never tokens or claims.
            logger.LogWarning("Work IQ owner-OBO failed ({FailureType}); no alternate identity is used", ex.GetType().Name);
            return Result<DelegatedWorkSignalAccess>.Failure(
                Error.Conflict("signals.sign_in_required", "No signals available. The signal owner must sign in again; check consent and policy."));
        }
    }
}
