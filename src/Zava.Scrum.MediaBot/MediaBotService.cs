using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;

namespace Zava.Scrum.MediaBot;

/// <summary>Owns the pinned Windows media platform and enforces the VM's hard concurrent call limit.</summary>
public sealed class MediaBotService : BackgroundService
{
    private readonly MediaBotOptions _options;
    private readonly ILoggerFactory _logs;
    private readonly IHttpClientFactory _http;
    private readonly Dictionary<Guid, (string TeamId, MediaCall Call)> _calls = [];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly X509Certificate2 _certificate;
    private readonly ICommunicationsClient _client;

    /// <summary>Initialises the SDK using a certificate registered on the calling application.</summary>
    /// <param name="options">Host settings.</param>
    /// <param name="logs">Metadata-only logging.</param>
    /// <param name="http">Clients for the trusted API.</param>
    public MediaBotService(IOptions<MediaBotOptions> options, ILoggerFactory logs, IHttpClientFactory http)
    {
        _options = options.Value;
        _logs = logs;
        _http = http;
        _certificate = LoadCertificate(_options.CertificateThumbprint);
        var credential = new ClientCertificateCredential(_options.TenantId, _options.AppId, _certificate);
        var graphLogger = new GraphLogger("ZavaMedia", redirectToTrace: false);
        _client = new CommunicationsClientBuilder("Zava Scrum Agent", _options.AppId, graphLogger)
            .SetAuthentication(_options.AppId, new GraphTokenProvider(credential, _options.TenantId))
            .SetServiceBaseUrl(new Uri("https://graph.microsoft.com/v1.0"))
            .SetNotificationUrl(new Uri($"https://{_options.ServiceFqdn}/callback"))
            .SetMediaPlatformSettings(new MediaPlatformSettings
            {
                ApplicationId = _options.AppId,
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings
                {
                    CertificateThumbprint = _options.CertificateThumbprint,
                    InstanceInternalPort = _options.MediaPort,
                    InstancePublicPort = _options.MediaPort,
                    InstancePublicIPAddress = IPAddress.Parse(_options.PublicIp),
                    ServiceFqdn = _options.ServiceFqdn,
                },
            }).Build();
    }

    /// <summary>Gets the communications client for authenticated notification processing.</summary>
    public ICommunicationsClient Client => _client;

    /// <summary>Validates the team at the API, then starts a real Graph call with unmixed PCM24K.</summary>
    /// <param name="join">Team-scoped request from the API.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP result, with no meeting content.</returns>
    public async Task<IResult> JoinAsync(MeetingJoin join, CancellationToken ct)
    {
        if (join.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(join.TeamId) || string.IsNullOrWhiteSpace(join.ApiBase)
            || !string.Equals(join.ApiBase.TrimEnd('/'), _options.ApiBase.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "media.invalid_join", detail = "The join must specify a session, team and the configured API origin." });
        }

        using var http = _http.CreateClient("api");
        http.DefaultRequestHeaders.Add("x-bridge-key", _options.ApiKey);
        http.DefaultRequestHeaders.Add("x-bridge-token", join.BridgeToken);
        var contextUrl = $"{_options.ApiBase.TrimEnd('/')}/api/media/context?sessionId={join.SessionId}&teamId={Uri.EscapeDataString(join.TeamId)}";
        using var context = await http.GetAsync(contextUrl, ct);
        if (!context.IsSuccessStatusCode)
        {
            return Results.Json(new { error = "media.session_refused", detail = "The API refused the team/session capability." }, statusCode: 403);
        }

        var verified = await context.Content.ReadFromJsonAsync<MeetingContext>(ct);
        if (verified is null || verified.SessionId != join.SessionId || verified.TeamId != join.TeamId || verified.JoinUrl != join.JoinUrl)
        {
            return Results.BadRequest(new { error = "media.session_mismatch", detail = "Use the live session's stored calendar link." });
        }

        var (chat, meeting) = MeetingCoordinates.Parse(join.JoinUrl, _options.TenantId);
        await _gate.WaitAsync(ct);
        try
        {
            if (_calls.TryGetValue(join.SessionId, out var existing))
            {
                return existing.TeamId == join.TeamId ? Results.Accepted() : Results.StatusCode(403);
            }

            if (_calls.Count >= _options.MaxConcurrentMeetings)
            {
                return Results.Json(new { error = "media.capacity", detail = "The media VM's concurrent meeting cap has been reached." }, statusCode: 409);
            }

            var media = _client.CreateMediaSession(new AudioSocketSettings
            {
                StreamDirections = StreamDirection.Sendrecv,
                SupportedAudioFormat = AudioFormat.Pcm24K,
                ReceiveUnmixedMeetingAudio = true,
            });
            var stream = new MediaCall(join, _options.TenantId, media, _logs.CreateLogger<MediaCall>(), _options.ApiKey);
            try
            {
                // No GuestIdentity: join as the consented application, not an anonymous display name.
                var call = await _client.Calls().AddAsync(new JoinMeetingParameters(chat, meeting, media)
                {
                    TenantId = _options.TenantId,
                }, join.SessionId, ct);
                stream.Start(call);
                _calls.Add(join.SessionId, (join.TeamId, stream));
                return Results.Accepted(value: new { sessionId = join.SessionId, teamId = join.TeamId, state = "joining" });
            }
            catch
            {
                await stream.DisposeAsync();
                throw;
            }
        }
        finally { _gate.Release(); }
    }

    /// <summary>Leaves a session idempotently; failed Graph deletes are surfaced, not treated as success.</summary>
    /// <param name="sessionId">The session.</param>
    /// <param name="teamId">The owning team.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task LeaveAsync(Guid sessionId, string teamId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!_calls.TryGetValue(sessionId, out var entry)) { return; }
            if (entry.TeamId != teamId) { throw new UnauthorizedAccessException("The call belongs to another team."); }
            if (entry.Call.Call is { } call && call.Resource.State != Microsoft.Graph.Models.CallState.Terminated)
            {
                await call.DeleteAsync(cancellationToken: ct);
            }

            await entry.Call.DisposeAsync();
            _calls.Remove(sessionId);
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await _gate.WaitAsync(stoppingToken);
                (Guid Id, string TeamId)[] ended;
                try { ended = _calls.Where(c => c.Value.Call.Ended).Select(c => (c.Key, c.Value.TeamId)).ToArray(); }
                finally { _gate.Release(); }
                foreach (var session in ended)
                {
                    await LeaveAsync(session.Id, session.TeamId, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        foreach (var session in _calls.Select(c => (Id: c.Key, c.Value.TeamId)).ToArray())
        {
            await LeaveAsync(session.Id, session.TeamId, cancellationToken);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _client.Dispose();
        _certificate.Dispose();
        _gate.Dispose();
        base.Dispose();
    }

    /// <summary>Loads the private-key certificate from the machine store, never a repository file.</summary>
    /// <param name="thumbprint">Provisioned thumbprint.</param>
    /// <returns>The valid certificate.</returns>
    public static X509Certificate2 LoadCertificate(string thumbprint)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: true);
        var certificate = certs.SingleOrDefault(c => c.HasPrivateKey);
        return certificate ?? throw new InvalidOperationException("A valid media TLS certificate with a private key is required in LocalMachine/My.");
    }

    private sealed record MeetingContext(Guid SessionId, string TeamId, string JoinUrl);
}
