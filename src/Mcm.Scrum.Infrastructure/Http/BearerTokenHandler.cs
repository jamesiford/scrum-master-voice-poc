using System.Net.Http.Headers;
using Azure.Core;

namespace Mcm.Scrum.Infrastructure.Http;

/// <summary>Adds an Entra bearer token from a <see cref="TokenCredential"/> (DefaultAzureCredential everywhere, no keys).</summary>
/// <param name="credential">The credential.</param>
/// <param name="scope">The token scope, for example <c>499b84ac-1321-427f-aa17-267ca6975798/.default</c> for Azure DevOps.</param>
public sealed class BearerTokenHandler(TokenCredential credential, string scope) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var token = await credential.GetTokenAsync(new TokenRequestContext([scope]), cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}
