using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Graph.Communications.Client;
using Microsoft.Extensions.Options;
using Zava.Scrum.MediaBot;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<MediaBotOptions>().Bind(builder.Configuration.GetSection("MediaBot")).ValidateDataAnnotations()
    .Validate(o => Uri.TryCreate(o.ApiBase, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps,
        "MediaBot:ApiBase must be the trusted HTTPS API origin.").ValidateOnStart();
builder.Services.AddHttpClient("api", client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddSingleton<MediaBotService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MediaBotService>());
builder.WebHost.ConfigureKestrel(server => server.ListenAnyIP(443, endpoint =>
    endpoint.UseHttps(MediaBotService.LoadCertificate(builder.Configuration["MediaBot:CertificateThumbprint"] ?? string.Empty))));
var app = builder.Build();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/teams"))
    {
        var key = builder.Configuration["MediaBot:ApiKey"];
        var provided = context.Request.Headers["x-bridge-key"].ToString();
        if (string.IsNullOrWhiteSpace(key) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(provided)))
        {
            context.Response.StatusCode = 401;
            return;
        }
    }

    await next();
});
app.MapGet("/healthz", (IOptions<MediaBotOptions> options) => Results.Ok(new
{
    service = "media-bot",
    recording = false,
    maxConcurrentMeetings = options.Value.MaxConcurrentMeetings,
}));
app.MapGet("/privacy", () => Results.Text("Audio and audio-derived content are used transiently during the meeting and are not recorded or stored. This bot has no approval or Azure DevOps write capability."));
app.MapGet("/terms", () => Results.Text("Preview non-recording Scrum facilitation. Use the existing signed-in portal approval flow for work item changes."));
app.MapPost("/teams/join", async (MeetingJoin join, MediaBotService bot, CancellationToken ct) =>
{
    try { return await bot.JoinAsync(join, ct); }
    catch (Exception ex) when (ex is ArgumentException or JsonException or FormatException)
    {
        return Results.BadRequest(new { error = "media.invalid_link", detail = "The Teams link is invalid or belongs to another tenant." });
    }
});
app.MapDelete("/teams/{sessionId:guid}", async (Guid sessionId, string teamId, MediaBotService bot, CancellationToken ct) =>
{
    try { await bot.LeaveAsync(sessionId, teamId, ct); }
    catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
    return Results.NoContent();
});
app.MapPost("/callback", async (HttpContext context, MediaBotService bot) =>
{
    using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{context.Request.Host}{context.Request.Path}")
    {
        Content = new StreamContent(context.Request.Body),
    };
    foreach (var header in context.Request.Headers)
    {
        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    using var response = await bot.Client.ProcessNotificationAsync(request);
    context.Response.StatusCode = (int)response.StatusCode;
    if (response.Content is not null)
    {
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
});
app.Run();

/// <summary>The Windows application-hosted media bot entry point.</summary>
public partial class Program;
