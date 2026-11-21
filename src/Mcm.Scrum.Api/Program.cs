using Mcm.Scrum.Api.Bot;
using Mcm.Scrum.Api.Endpoints;
using Microsoft.Agents.Hosting.AspNetCore;
using Mcm.Scrum.Api.Voice;
using Mcm.Scrum.Application;
using Mcm.Scrum.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOptions<VoiceGatewayOptions>().Bind(builder.Configuration.GetSection(VoiceGatewayOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton<BridgeTokenService>();
builder.Services.AddTransient<VoiceGateway>();

var botConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["Bot:AppId"]);
if (botConfigured)
{
    // Teams bot (SPEC 12). Off until Bot:AppId is set, so the Simulated Room and portal work without Teams.
    builder.AddAgentDefaults().AddAgent<ScrumAgent>().AddAgentAuthorization(
        b => BotAuthentication.Add(b, builder.Configuration["Bot:AppId"]!, builder.Configuration["Azure:TenantId"] ?? builder.Configuration["Bot:TenantId"]!),
        forceEnable: true);
}

var app = builder.Build();

// The dev tunnel is public and anonymous. Through it only the bot endpoint is reachable; the portal API stays local (F7).
var tunnelSuffix = builder.Configuration["Bot:TunnelHostSuffix"];
if (!string.IsNullOrWhiteSpace(tunnelSuffix))
{
    app.Use(async (context, next) =>
    {
        // The relay may rewrite Host to localhost, so also treat forwarding headers as proof the call came through the tunnel.
        var viaTunnel = context.Request.Host.Host.EndsWith(tunnelSuffix, StringComparison.OrdinalIgnoreCase)
            || context.Request.Headers.ContainsKey("X-Forwarded-For")
            || context.Request.Headers.ContainsKey("X-Original-Host");
        if (viaTunnel && !context.Request.Path.StartsWithSegments("/api/messages", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next();
    });
}

app.UseWebSockets();
// The portal build is copied to wwwroot in the container (M7); locally Vite serves it.
app.UseDefaultFiles();
app.UseStaticFiles();
if (botConfigured)
{
    app.UseAgents();
    app.MapDefaultAgentEndpoints();
}

app.MapHealthChecks("/healthz");
app.MapHealthEndpoints();
app.MapTeamsEndpoints();
app.MapSessionsEndpoints();
app.MapFollowUpsEndpoints();
app.MapBridgeEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>Entry point marker so integration tests can host the API.</summary>
public partial class Program;
