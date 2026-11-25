using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Infrastructure.Delegation;

/// <summary>
/// Exchanges the person's token for an Azure DevOps token on their behalf. The app proves itself with the managed identity's federated
/// token (no client secret), and the result is never cached here or stored. When the exchange cannot complete the write is refused:
/// nothing falls back to the managed identity (ADR 0020).
/// </summary>
/// <remarks>VERIFY: https://learn.microsoft.com/entra/identity-platform/v2-oauth2-on-behalf-of-flow and https://learn.microsoft.com/azure/devops/integrate/get-started/authentication/entra-oauth.</remarks>
public sealed class OboAdoTokenSource(IOptions<DelegationOptions> options, ILogger<OboAdoTokenSource> logger) : IApproverTokenSource
{
    private const string AdoScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";
    private const string FederatedAudience = "api://AzureADTokenExchange/.default";

    // Entra answers interaction_required, consent_required or a claims challenge (AADSTS50076 MFA, 50079, 53003 Conditional Access, 65001 consent).
    private static readonly string[] InteractionMarkers = ["interaction_required", "consent_required", "AADSTS50076", "AADSTS50079", "AADSTS53003", "AADSTS65001", "AADSTS50158", "claims"];

    private static bool NeedsInteraction(string message) => InteractionMarkers.Any(m => message.Contains(m, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public async Task<Result<DelegatedAdoAccess>> ExchangeAsync(UserAssertion assertion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.TenantId) || string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId))
        {
            return Result<DelegatedAdoAccess>.Failure(DelegationErrors.NotConfigured);
        }

        try
        {
            var proof = new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(settings.ManagedIdentityClientId));
            var obo = new OnBehalfOfCredential(
                settings.TenantId,
                settings.ClientId,
                async token => (await proof.GetTokenAsync(new TokenRequestContext([FederatedAudience]), token)).Token,
                assertion.Token);
            var issued = await obo.GetTokenAsync(new TokenRequestContext([AdoScope]), ct);
            return Result<DelegatedAdoAccess>.Success(new DelegatedAdoAccess(issued.Token));
        }
        catch (Exception ex) when (ex is AuthenticationFailedException or CredentialUnavailableException)
        {
            if (NeedsInteraction(ex.Message))
            {
                logger.LogWarning("On-behalf-of exchange needs the person to sign in again (consent, multi-factor or Conditional Access)");
                return Result<DelegatedAdoAccess>.Failure(DelegationErrors.InteractionRequired);
            }

            // The message carries Entra's reason (consent or sign-in needed); the token and the assertion are never logged.
            logger.LogWarning("On-behalf-of exchange for Azure DevOps failed: {Reason}", ex.Message.Split('\n')[0]);
            return Result<DelegatedAdoAccess>.Failure(DelegationErrors.ExchangeFailed);
        }
    }
}
