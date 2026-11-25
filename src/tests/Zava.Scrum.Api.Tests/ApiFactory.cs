using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Api.Tests;

/// <summary>
/// Hosts the real API with Entra validation on, a local signing key in place of the tenant's published keys, and a temporary data folder.
/// Nothing is replaced except where the keys come from: the issuer, audience, lifetime, tenant and oid checks all run as in production.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ClientId = "99999999-9999-9999-9999-999999999999";
    public const string TenantId = "11111111-1111-1111-1111-111111111111";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test-key" };
    private readonly string _data = Path.Combine(Path.GetTempPath(), "zava-api-tests-" + Guid.NewGuid().ToString("N"));

    public static string Issuer => $"https://login.microsoftonline.com/{TenantId}/v2.0";

    /// <summary>Creates a signed token. Every claim can be overridden or removed by passing null.</summary>
    public string Token(
        string[]? roles = null,
        string? tenant = TenantId,
        string? oid = "22222222-2222-2222-2222-222222222222",
        string audience = ClientId,
        string? issuer = null,
        SecurityKey? signingKey = null,
        DateTime? expires = null)
    {
        var claims = new Dictionary<string, object> { ["name"] = "Dana", ["preferred_username"] = "dana@zava.example", ["scp"] = "access_as_user" };
        if (tenant is not null)
        {
            claims["tid"] = tenant;
        }

        if (oid is not null)
        {
            claims["oid"] = oid;
        }

        if (roles is { Length: > 0 })
        {
            claims["roles"] = roles;
        }

        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Issuer,
            Audience = audience,
            Claims = claims,
            NotBefore = expires is null ? now.AddMinutes(-1) : expires.Value.AddHours(-1),
            Expires = expires ?? now.AddHours(1),
            SigningCredentials = new SigningCredentials(signingKey ?? _key, SecurityAlgorithms.RsaSha256),
        });
    }

    public static SecurityKey WrongKey() => new RsaSecurityKey(RSA.Create(2048)) { KeyId = "other" };

    /// <summary>Creates a client that sends the given bearer token.</summary>
    public HttpClient ClientWith(string? token)
    {
        var client = CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // UseSetting, not ConfigureAppConfiguration: Program reads these while building, before the factory's configuration callbacks run.
        builder.UseSetting("Portal:ClientId", ClientId);
        builder.UseSetting("Identity:TenantId", TenantId);
        builder.UseSetting("Azure:TenantId", TenantId);
        builder.UseSetting("Store:DataPath", _data);
        builder.ConfigureServices(services => services.PostConfigure<JwtBearerOptions>("Portal", o =>
        {
            o.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
            o.TokenValidationParameters.IssuerSigningKey = _key;
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_data))
        {
            Directory.Delete(_data, recursive: true);
        }
    }
}

internal static class Roles
{
    public static readonly string[] Reader = [AppRoles.Reader];
    public static readonly string[] Approver = [AppRoles.Approver];
    public static readonly string[] Admin = [AppRoles.Admin];
}
