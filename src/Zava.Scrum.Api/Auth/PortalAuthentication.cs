using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Infrastructure.Identity;

namespace Zava.Scrum.Api.Auth;

/// <summary>
/// Validates portal callers inside the API. It never reads the Easy Auth headers (<c>X-MS-CLIENT-PRINCIPAL*</c>): anything in front of
/// the API can be bypassed or misconfigured, so the API checks the token itself (ADR 0017).
/// </summary>
public static class PortalAuthentication
{
    /// <summary>Registers the <c>Portal</c> scheme and the three role policies.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder.</returns>
    public static IHostApplicationBuilder AddPortalAuthentication(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var options = new PortalAuthOptions();
        builder.Configuration.GetSection(PortalAuthOptions.SectionName).Bind(options);
        builder.Services.AddOptions<PortalAuthOptions>().Bind(builder.Configuration.GetSection(PortalAuthOptions.SectionName));
        var tenant = string.IsNullOrWhiteSpace(options.TenantId) ? IdentityOptions.ResolveTenant(builder.Configuration) : options.TenantId;
        var mode = string.IsNullOrWhiteSpace(options.ClientId) ? PortalAuthMode.Development : PortalAuthMode.Entra;

        var authentication = builder.Services.AddAuthentication();
        if (mode == PortalAuthMode.Entra)
        {
            if (!Guid.TryParse(tenant, out _))
            {
                throw new InvalidOperationException("Portal:ClientId is set, so a tenant is required: set Identity:TenantId (or Azure:TenantId).");
            }

            authentication.AddJwtBearer(Policies.Scheme, o => ConfigureEntra(o, options.ClientId!, tenant!));
        }
        else
        {
            // Outside Development a missing client id fails closed: the API stays up (the bot and health routes work) but every portal call is
            // a 401 until the deployment sets Portal:ClientId (infra/hooks/entra-post.ps1 re-applies it).
            if (builder.Environment.IsDevelopment())
            {
                authentication.AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(Policies.Scheme, null);
            }
            else
            {
                authentication.AddScheme<AuthenticationSchemeOptions, NoPortalAuthenticationHandler>(Policies.Scheme, null);
            }
        }

        // Any signed-in tenant user passes the coarse gate; what they can do is decided per team by the team filter (ADR 0018).
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Reader, p => p.AddAuthenticationSchemes(Policies.Scheme).RequireAuthenticatedUser());
        return builder;
    }

    /// <summary>Applies the Entra validation rules to a JwtBearer scheme. Public so tests can validate against a local signing key.</summary>
    /// <param name="options">The scheme options.</param>
    /// <param name="clientId">The portal app registration's client id.</param>
    /// <param name="tenantId">The tenant.</param>
    public static void ConfigureEntra(JwtBearerOptions options, string clientId, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(options);
        var issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Authority = issuer;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudiences = [clientId, $"api://{clientId}"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            RoleClaimType = "roles",
            NameClaimType = "name",
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal;
                var tid = principal?.FindFirstValue("tid");
                var oid = principal?.FindFirstValue("oid");
                if (!string.Equals(tid, tenantId, StringComparison.OrdinalIgnoreCase) || !Guid.TryParse(oid, out var id) || id == Guid.Empty)
                {
                    context.Fail("The token must carry this tenant's tid and a valid oid.");
                }

                return Task.CompletedTask;
            },
        };
    }

    /// <summary>Authenticates nobody, so every portal route answers 401 until a client id is configured.</summary>
    internal sealed class NoPortalAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }

    /// <summary>A fixed local principal with every role. Registered only in the Development environment.</summary>
    internal sealed class DevelopmentAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IOptions<PortalAuthOptions> portal, IConfiguration configuration)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var tenant = IdentityOptions.ResolveTenant(configuration) ?? Guid.Empty.ToString();
            List<Claim> claims =
            [
                new("oid", portal.Value.DevelopmentObjectId),
                new("tid", tenant),
                new("name", "Local developer"),
                new("preferred_username", "developer@localhost"),
                .. Policies.ReadRoles.Select(r => new Claim("roles", r)),
            ];
            var identity = new ClaimsIdentity(claims, Scheme.Name, "name", "roles");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
