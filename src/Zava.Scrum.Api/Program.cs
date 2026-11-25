using Zava.Scrum.Api.Auth;
using Zava.Scrum.Api.Bot;
using Zava.Scrum.Api.Endpoints;
using Zava.Scrum.Api.Telemetry;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Zava.Scrum.Api.Voice;
using Zava.Scrum.Application;
using Zava.Scrum.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddScrumTelemetry();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.AddPortalAuthentication();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOptions<VoiceGatewayOptions>().Bind(builder.Configuration.GetSection(VoiceGatewayOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton<BridgeTokenService>();
builder.Services.AddTransient<VoiceGateway>();
builder.Services.AddSingleton<LiveSessionRegistry>();
builder.Services.AddSingleton<Zava.Scrum.Application.Abstractions.Ports.ILiveSessions>(sp => sp.GetRequiredService<LiveSessionRegistry>());
builder.Services.AddSingleton<Zava.Scrum.Application.Abstractions.Ports.IBridgeAccess, BridgeAccess>();
builder.Services.AddHostedService<Zava.Scrum.Api.Seeding.DemoTeamSeeder>();
builder.Services.AddHostedService<Zava.Scrum.Api.Scheduling.ScheduleWorker>();

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

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserAssertionMiddleware>();
app.UseWebSockets();
// The portal build is copied to wwwroot in the container (M7); locally Vite serves it.
app.UseDefaultFiles();
app.UseStaticFiles();
if (botConfigured)
{
    app.UseAgents();
    // Not MapDefaultAgentEndpoints: it also claims GET / with a version string, which hides the portal.
    app.MapPost("/api/messages", (HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken ct) =>
        adapter.ProcessAsync(request, response, agent, ct)).RequireAuthorization();
}

app.MapHealthChecks("/healthz");
app.MapPortalConfigEndpoint();

// Every portal route is validated in-app (ADR 0017): reader for reads, admin for changes, approver for decisions.
var api = app.MapGroup(string.Empty).RequireRolesByDefault();
api.MapHealthEndpoints();
api.MapTeamsEndpoints();
api.MapSessionsEndpoints();
api.MapFollowUpsEndpoints();
api.MapScheduleEndpoints();
api.MapDemoEndpoints();
api.MapReportsEndpoints();
api.MapPlanningEndpoints();
api.MapCalendarEndpoints();
api.MapCeremoniesEndpoints();
api.MapInsightsEndpoints();
api.MapAuditEndpoints();

// The bridge websocket authenticates with its own short-lived, session-bound token, not a portal bearer token.
app.MapBridgeEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>Entry point marker so integration tests can host the API.</summary>
public partial class Program;
