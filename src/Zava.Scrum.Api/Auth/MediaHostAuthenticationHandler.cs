using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Zava.Scrum.Api.Voice;
using Zava.Scrum.Infrastructure.Teams;

namespace Zava.Scrum.Api.Auth;

/// <summary>Authenticates a media host's session capability without granting it a portal or approver identity.</summary>
internal sealed class MediaHostAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemes,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<BridgeOptions> bridge,
    BridgeTokenService tokens) : AuthenticationHandler<AuthenticationSchemeOptions>(schemes, logger, encoder)
{
    internal const string SchemeName = "MediaHost";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = bridge.Value;
        var teamId = Request.Query["teamId"].ToString();
        if (options.Mode != "GraphMedia" || string.IsNullOrWhiteSpace(teamId)
            || !Guid.TryParse(Request.Query["sessionId"], out var sessionId)
            || string.IsNullOrWhiteSpace(options.ApiKey)
            || !tokens.IsValid(Request.Headers["x-bridge-token"], sessionId)
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(options.ApiKey),
                Encoding.UTF8.GetBytes(Request.Headers["x-bridge-key"].ToString())))
        {
            return Task.FromResult(AuthenticateResult.Fail("A valid media-host session capability is required."));
        }

        var identity = new ClaimsIdentity([new Claim("team", teamId), new Claim("session_id", sessionId.ToString())], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
