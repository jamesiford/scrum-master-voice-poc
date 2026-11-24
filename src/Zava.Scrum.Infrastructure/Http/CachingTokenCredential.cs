using Azure.Core;

namespace Zava.Scrum.Infrastructure.Http;

/// <summary>
/// Caches access tokens per scope until shortly before they expire. <c>DefaultAzureCredential</c> does not cache every credential it
/// wraps (the Azure CLI credential used on a developer machine starts a process on each call, about 1.2 s), and the live standup
/// calls Azure DevOps, Foundry and the decision model on every utterance. Managed identity in Azure already caches, so there this is a cheap safeguard.
/// </summary>
/// <param name="inner">The credential that gets tokens.</param>
public sealed class CachingTokenCredential(TokenCredential inner) : TokenCredential, IDisposable
{
    private static readonly TimeSpan RefreshBefore = TimeSpan.FromMinutes(5);

    private readonly Dictionary<string, AccessToken> _tokens = [];
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    /// <inheritdoc />
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        // A request with claims (a conditional access challenge) must reach the real credential.
        if (requestContext.Claims is not null)
        {
            return await inner.GetTokenAsync(requestContext, cancellationToken);
        }

        var key = string.Join(' ', requestContext.Scopes) + '|' + requestContext.TenantId;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_tokens.TryGetValue(key, out var cached) && cached.ExpiresOn - DateTimeOffset.UtcNow > RefreshBefore)
            {
                return cached;
            }

            var fresh = await inner.GetTokenAsync(requestContext, cancellationToken);
            _tokens[key] = fresh;
            return fresh;
        }
        finally
        {
            _gate.Release();
        }
    }
}
