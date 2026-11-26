using Azure.Core;
using Microsoft.Graph.Communications.Client.Authentication;

namespace Zava.Scrum.MediaBot;

/// <summary>Supplies certificate-backed tokens to the SDK's current built-in notification validator.</summary>
public sealed class GraphTokenProvider(TokenCredential credential, string tenantId) : ITokenProvider
{
    /// <inheritdoc />
    public async Task<string> AcquireTokenAsync(string tenant)
    {
        if (!string.Equals(tenant, tenantId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Graph attempted to authenticate in another tenant.");
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = await credential.GetTokenAsync(new TokenRequestContext(["https://graph.microsoft.com/.default"]), timeout.Token);
        return token.Token;
    }
}
