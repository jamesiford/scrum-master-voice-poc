using Mcm.Scrum.Api.Endpoints;
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

var app = builder.Build();

app.UseWebSockets();
app.MapHealthChecks("/healthz");
app.MapHealthEndpoints();
app.MapTeamsEndpoints();
app.MapSessionsEndpoints();
app.MapFollowUpsEndpoints();
app.MapBridgeEndpoints();

app.Run();

/// <summary>Entry point marker so integration tests can host the API.</summary>
public partial class Program;
