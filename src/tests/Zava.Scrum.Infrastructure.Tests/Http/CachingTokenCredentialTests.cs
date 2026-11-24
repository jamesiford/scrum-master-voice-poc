using Azure.Core;
using Zava.Scrum.Infrastructure.Http;

namespace Zava.Scrum.Infrastructure.Tests.Http;

public sealed class CachingTokenCredentialTests
{
    private sealed class CountingCredential(TimeSpan lifetime) : TokenCredential
    {
        public int Calls { get; private set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new($"token-{++Calls}", DateTimeOffset.UtcNow + lifetime);

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(GetToken(requestContext, cancellationToken));
    }

    [Fact]
    public async Task GetToken_SameScopeTwice_CallsTheInnerCredentialOnce()
    {
        var inner = new CountingCredential(TimeSpan.FromHours(1));
        using var cache = new CachingTokenCredential(inner);

        var first = await cache.GetTokenAsync(new TokenRequestContext(["a/.default"]), CancellationToken.None);
        var second = await cache.GetTokenAsync(new TokenRequestContext(["a/.default"]), CancellationToken.None);

        Assert.Equal(first.Token, second.Token);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task GetToken_DifferentScopes_AreCachedSeparately()
    {
        var inner = new CountingCredential(TimeSpan.FromHours(1));
        using var cache = new CachingTokenCredential(inner);

        await cache.GetTokenAsync(new TokenRequestContext(["a/.default"]), CancellationToken.None);
        await cache.GetTokenAsync(new TokenRequestContext(["b/.default"]), CancellationToken.None);

        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task GetToken_NearExpiry_RefreshesTheToken()
    {
        var inner = new CountingCredential(TimeSpan.FromMinutes(2));
        using var cache = new CachingTokenCredential(inner);

        await cache.GetTokenAsync(new TokenRequestContext(["a/.default"]), CancellationToken.None);
        await cache.GetTokenAsync(new TokenRequestContext(["a/.default"]), CancellationToken.None);

        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task GetToken_WithClaimsChallenge_BypassesTheCache()
    {
        var inner = new CountingCredential(TimeSpan.FromHours(1));
        using var cache = new CachingTokenCredential(inner);

        await cache.GetTokenAsync(new TokenRequestContext(["a/.default"]), CancellationToken.None);
        await cache.GetTokenAsync(new TokenRequestContext(["a/.default"], claims: "{\"x\":1}"), CancellationToken.None);

        Assert.Equal(2, inner.Calls);
    }
}
