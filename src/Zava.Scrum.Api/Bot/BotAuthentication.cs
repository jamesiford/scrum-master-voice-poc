using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Mcm.Scrum.Api.Bot;

/// <summary>Validates inbound Bot Service and Entra tokens on <c>/api/messages</c>. The dev tunnel is public, so this is always on.</summary>
internal static class BotAuthentication
{
    private const string BotFrameworkConfig = "https://login.botframework.com/v1/.well-known/openidconfiguration";

    public static void Add(IHostApplicationBuilder builder, string appId, string tenantId)
    {
        var botKeys = Manager(BotFrameworkConfig);
        var tenantKeys = Manager($"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration");
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = ["https://api.botframework.com", $"https://sts.windows.net/{tenantId}/", $"https://login.microsoftonline.com/{tenantId}/v2.0"],
                    ValidateAudience = true,
                    ValidAudiences = [appId, $"api://{appId}"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                        botKeys.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult().SigningKeys
                            .Concat(tenantKeys.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult().SigningKeys),
                };
            });
    }

    private static ConfigurationManager<OpenIdConnectConfiguration> Manager(string address) =>
        new(address, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever());
}
