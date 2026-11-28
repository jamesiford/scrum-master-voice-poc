using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Graph.Models;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Skype.Bots.Media;
using System.Diagnostics;
using Zava.Scrum.Telemetry;

namespace Zava.Scrum.MediaBot;

/// <summary>One team-scoped call, ephemeral unmixed audio and a bounded, paced return stream (ADR 0023).</summary>
public sealed class MediaCall : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly MeetingJoin _join;
    private readonly string _tenantId;
    private readonly ILocalMediaSession _media;
    private readonly ILogger<MediaCall> _logger;
    private readonly string _apiKey;
    private readonly CancellationTokenSource _stop = new();
    private readonly Channel<object> _incoming = System.Threading.Channels.Channel.CreateBounded<object>(100);
    private readonly Queue<byte[]> _outgoing = new();
    private readonly object _audioGate = new();
    private readonly ConcurrentDictionary<uint, ParticipantIdentity> _sources = new();
    private readonly ConcurrentDictionary<string, ParticipantIdentity> _participants = new();
    private Task? _run;
    private ICall? _call;
    private MediaBoardTransport? _boardTransport;
    private BoardShareSession? _boardShare;
    private volatile bool _sendActive;
    private volatile bool _inLobby;
    private bool _responseDone;
    private bool _playing;
    private long _playedFrames;
    private int _disposed;
    private long _lastFrameTrace;

    /// <summary>Subscribes before negotiation so no received media is missed.</summary>
    /// <param name="join">Verified team-scoped request.</param>
    /// <param name="tenantId">The deployment tenant.</param>
    /// <param name="media">The SDK media session.</param>
    /// <param name="logger">Metadata-only diagnostics.</param>
    /// <param name="apiKey">Secret authenticating this host to the gateway.</param>
    public MediaCall(MeetingJoin join, string tenantId, ILocalMediaSession media, ILogger<MediaCall> logger, string apiKey)
    {
        _join = join;
        _tenantId = tenantId;
        _media = media;
        _logger = logger;
        _apiKey = apiKey;
        media.AudioSocket.AudioMediaReceived += OnAudio;
        media.AudioSocket.AudioSendStatusChanged += OnSendStatus;
    }

    /// <summary>Gets the current Graph call, if negotiation completed.</summary>
    public ICall? Call => _call;

    /// <summary>Gets whether the media worker has ended.</summary>
    public bool Ended => _run?.IsCompleted == true;

    /// <summary>Lets the board submit its received recap before the owning call is deleted.</summary>
    /// <param name="ct">The call owner's cancellation token.</param>
    /// <returns>A task; audio processing remains independent while the board drains.</returns>
    public Task PresentCompletionAsync(CancellationToken ct) =>
        _boardShare?.PresentCompletionAsync(ct) ?? Task.CompletedTask;

    /// <summary>Attaches the negotiated call and starts the gateway connection.</summary>
    /// <param name="call">The call created by Graph.</param>
    public void Start(ICall call)
    {
        _call = call;
        if (_media.VbssSocket is { } vbss)
        {
            _boardTransport = new MediaBoardTransport(call, vbss, _logger, _join.TeamId, _join.SessionId);
            _boardShare = new BoardShareSession(_boardTransport, TimeProvider.System, _logger, _join.TeamId, _join.SessionId,
                (state, message) => Enqueue(new { type = "boardStatus", state, message }));
            _boardShare.SetEstablished(call.Resource.State == CallState.Established && !_inLobby);
            _boardShare.Start();
        }
        call.OnUpdated += OnCallUpdated;
        call.Participants.OnUpdated += OnRosterUpdated;
        foreach (var participant in call.Participants)
        {
            AddParticipant(participant);
        }

        Enqueue(new { type = "callState", state = CallStateText(), message = "Teams-native bot is joining; admission may be required." });
        _run = RunAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _stop.CancelAsync();
        try
        {
            if (_run is not null)
            {
                await _run;
            }
        }
        finally
        {
            if (_boardShare is not null) { await _boardShare.DisposeAsync(); }
            _boardTransport?.Dispose();
            _media.AudioSocket.AudioMediaReceived -= OnAudio;
            _media.AudioSocket.AudioSendStatusChanged -= OnSendStatus;
            if (_call is not null)
            {
                _call.OnUpdated -= OnCallUpdated;
                _call.Participants.OnUpdated -= OnRosterUpdated;
                foreach (var participant in _call.Participants)
                {
                    participant.OnUpdated -= OnParticipantUpdated;
                }
            }

            lock (_audioGate)
            {
                _outgoing.Clear();
            }

            while (_incoming.Reader.TryRead(out _)) { }
            _media.Dispose();
            _stop.Dispose();
        }
    }

    private void OnAudio(object? sender, AudioMediaReceivedEventArgs args)
    {
        var started = Stopwatch.GetTimestamp();
        var sample = Stopwatch.GetElapsedTime(_lastFrameTrace).TotalSeconds >= 1;
        if (sample) { _lastFrameTrace = started; }
        using var hop = sample && ActivityContext.TryParse(_join.TraceParent, null, out var parent)
            ? SessionTelemetry.Source.StartActivity("media.audio.frame", ActivityKind.Producer, parent,
                new ActivityTagsCollection { [SessionTelemetry.TeamTag] = _join.TeamId, ["session.id"] = _join.SessionId.ToString(), ["hop"] = "media.audio.frame" })
            : null;
        using var buffer = args.Buffer;
        var frames = new List<object>();
        foreach (var audio in buffer.UnmixedAudioBuffers ?? [])
        {
            if (audio.Length != 960)
            {
                _logger.LogError("Unexpected unmixed PCM frame size {Length} for {TeamId}/{SessionId}", audio.Length, _join.TeamId, _join.SessionId);
                _stop.Cancel();
                return;
            }

            var pcm = new byte[960];
            Marshal.Copy(audio.Data, pcm, 0, pcm.Length);
            _sources.TryGetValue(audio.ActiveSpeakerId, out var identity);
            if (identity is not null && identity.ParticipantId == _call?.Resource.MyParticipantId) { continue; }
            frames.Add(new
            {
                participantId = identity?.ParticipantId ?? $"source-{audio.ActiveSpeakerId}",
                participantObjectId = identity?.ObjectId,
                displayName = identity?.DisplayName ?? "Guest",
                pcm = Convert.ToBase64String(pcm),
                amplitude = TransientAudio.Amplitude(pcm),
            });
        }

        Enqueue(new { type = "audio", items = frames, traceParent = hop?.Id });
        if (sample) { SessionTelemetry.RecordHop("media.audio.frame", _join.TeamId, Stopwatch.GetElapsedTime(started).TotalMilliseconds); }
    }

    private void OnSendStatus(object? sender, AudioSendStatusChangedEventArgs args) =>
        _sendActive = args.MediaSendStatus == MediaSendStatus.Active;

    private void OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)
    {
        _boardShare?.SetEstablished(args.NewResource.State == CallState.Established && !_inLobby);
        Enqueue(new { type = "callState", state = CallStateText(), message = "Teams call state changed." });
        if (args.NewResource.State == CallState.Terminated)
        {
            _stop.Cancel();
        }
    }

    private void OnRosterUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
    {
        foreach (var participant in args.RemovedResources)
        {
            participant.OnUpdated -= OnParticipantUpdated;
            _participants.TryRemove(participant.Id, out _);
            foreach (var source in _sources.Where(s => s.Value.ParticipantId == participant.Id))
            {
                _sources.TryRemove(source.Key, out _);
            }
        }

        foreach (var participant in args.AddedResources)
        {
            AddParticipant(participant);
        }

        foreach (var participant in args.UpdatedResources)
        {
            ReadParticipant(participant);
        }

        Enqueue(new { type = "participants", items = HumanParticipants() });
    }

    private void AddParticipant(IParticipant participant)
    {
        participant.OnUpdated += OnParticipantUpdated;
        ReadParticipant(participant);
    }

    private void OnParticipantUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
    {
        ReadParticipant(sender);
        Enqueue(new { type = "participants", items = HumanParticipants() });
    }

    private void ReadParticipant(IParticipant participant)
    {
        if (participant.Id == _call?.Resource.MyParticipantId)
        {
            _inLobby = participant.Resource.IsInLobby == true;
            _boardShare?.SetEstablished(_call?.Resource.State == CallState.Established && !_inLobby);
            Enqueue(new { type = "callState", state = CallStateText(), message = _inLobby ? "Waiting in Teams lobby." : "Teams admission updated." });
        }

        var user = participant.Resource.Info?.Identity?.User;
        var tenant = user?.AdditionalData is { } data && data.TryGetValue("tenantId", out var value) ? value?.ToString() : null;
        var objectId = string.Equals(tenant, _tenantId, StringComparison.OrdinalIgnoreCase) && Guid.TryParse(user?.Id, out var id) ? id : (Guid?)null;
        var identity = new ParticipantIdentity(participant.Id, objectId, user?.DisplayName ?? "Guest");
        _participants[participant.Id] = identity;
        foreach (var old in _sources.Where(s => s.Value.ParticipantId == participant.Id))
        {
            _sources.TryRemove(old.Key, out _);
        }

        foreach (var stream in participant.Resource.MediaStreams ?? [])
        {
            if (stream.MediaType == Modality.Audio && uint.TryParse(stream.SourceId, out var sourceId))
            {
                _sources[sourceId] = identity;
            }
        }
    }

    private void Enqueue(object message)
    {
        if (!_stop.IsCancellationRequested && !_incoming.Writer.TryWrite(message))
        {
            _logger.LogError("Media gateway input exceeded its two-second bound for {TeamId}/{SessionId}", _join.TeamId, _join.SessionId);
            _stop.Cancel();
        }
    }

    private async Task RunAsync()
    {
        for (var attempt = 0; attempt < 3 && !_stop.IsCancellationRequested; attempt++)
        {
            try
            {
                using var socket = new ClientWebSocket();
                if (_join.TraceParent is { } traceParent) { socket.Options.SetRequestHeader("traceparent", traceParent); }
                socket.Options.SetRequestHeader("x-bridge-key", _apiKey);
                socket.Options.SetRequestHeader("x-bridge-token", _join.BridgeToken);
                var origin = new Uri(_join.ApiBase);
                var uri = new UriBuilder(origin)
                {
                    Scheme = origin.Scheme == "https" ? "wss" : "ws",
                    Path = "/invocations_ws",
                    Query = $"sessionId={_join.SessionId}&teamId={Uri.EscapeDataString(_join.TeamId)}",
                }.Uri;
                await socket.ConnectAsync(uri, _stop.Token);
                _boardShare?.Reconnect();
                Enqueue(new { type = "callState", state = CallStateText(), message = "Connected to the non-recording gateway." });
                Enqueue(new { type = "participants", items = HumanParticipants() });
                using var connected = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                var tasks = new[] { SendAsync(socket, connected.Token), ReceiveAsync(socket, connected.Token), PlayAsync(connected.Token) };
                await Task.WhenAny(tasks);
                await connected.CancelAsync();
                try { await Task.WhenAll(tasks); }
                catch (OperationCanceledException) when (connected.IsCancellationRequested) { }
                if (_stop.IsCancellationRequested)
                {
                    return;
                }

                throw new WebSocketException("The gateway closed the media connection.");
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (ex is WebSocketException or HttpRequestException)
            {
                _logger.LogWarning("Media gateway disconnected for {TeamId}/{SessionId}, attempt {Attempt}: {Failure}", _join.TeamId, _join.SessionId, attempt + 1, ex.GetType().Name);
                lock (_audioGate) { _outgoing.Clear(); _playing = false; _responseDone = false; _playedFrames = 0; }
                while (_incoming.Reader.TryRead(out _)) { }
                if (attempt < 2)
                {
                    try { await Task.Delay(1000, _stop.Token); }
                    catch (OperationCanceledException) when (_stop.IsCancellationRequested) { return; }
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                _logger.LogError("Media worker failed for {TeamId}/{SessionId}: {Failure}", _join.TeamId, _join.SessionId, ex.GetType().Name);
                return;
            }
        }
    }

    private async Task SendAsync(ClientWebSocket socket, CancellationToken ct)
    {
        await foreach (var message in _incoming.Reader.ReadAllAsync(ct))
        {
            await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message, Json), WebSocketMessageType.Text, true, ct);
        }
    }

    private async Task ReceiveAsync(ClientWebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var remainder = new List<byte>();
        while (!ct.IsCancellationRequested)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult frame;
            do
            {
                frame = await socket.ReceiveAsync(buffer, ct);
                if (frame.MessageType == WebSocketMessageType.Close) { return; }
                message.Write(buffer, 0, frame.Count);
                if (message.Length > 1024 * 1024) { throw new InvalidOperationException("Gateway frame exceeded 1 MB."); }
            }
            while (!frame.EndOfMessage);
            if (frame.MessageType == WebSocketMessageType.Binary)
            {
                remainder.AddRange(message.ToArray());
                lock (_audioGate)
                {
                    while (remainder.Count >= 960)
                    {
                        if (_outgoing.Count >= 250) { throw new InvalidOperationException("Agent audio exceeded the five-second playback bound."); }
                        _outgoing.Enqueue(remainder.GetRange(0, 960).ToArray());
                        remainder.RemoveRange(0, 960);
                    }

                    _playing = true;
                }
            }
            else
            {
                using var json = JsonDocument.Parse(message.ToArray());
                var type = json.RootElement.GetProperty("type").GetString();
                if (type == "board")
                {
                    var board = json.RootElement;
                    if (board.GetProperty("teamId").GetString() != _join.TeamId
                        || board.GetProperty("sessionId").GetGuid() != _join.SessionId)
                    {
                        throw new InvalidOperationException("The board belongs to another team or session.");
                    }

                    _boardShare?.Update(board);
                }
                else if (type == "stopAudio")
                {
                    long playedMs;
                    lock (_audioGate) { _outgoing.Clear(); _playing = false; playedMs = _playedFrames * 20; _playedFrames = 0; }
                    remainder.Clear();
                    Enqueue(new { type = "agentAudioState", state = "idle", interrupted = true, playedMs });
                }
                else if (type == "responseStarted")
                {
                    lock (_audioGate) { _responseDone = false; _playedFrames = 0; }
                }
                else if (type == "responseDone")
                {
                    if (remainder.Count > 0)
                    {
                        remainder.AddRange(new byte[960 - remainder.Count]);
                        lock (_audioGate) { _outgoing.Enqueue(remainder.ToArray()); }
                        remainder.Clear();
                    }

                    lock (_audioGate) { _responseDone = true; _playing = true; }
                }
            }
        }
    }

    private async Task PlayAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
        while (await timer.WaitForNextTickAsync(ct))
        {
            lock (_audioGate)
            {
                if (_sendActive && _outgoing.TryDequeue(out var pcm))
                {
                    var buffer = new OutgoingAudioBuffer(pcm);
                    try { _media.AudioSocket.Send(buffer); }
                    catch { buffer.Dispose(); throw; }
                    _playedFrames++;
                    if (_playedFrames % 5 == 0)
                    {
                        Enqueue(new { type = "audioProgress", playedMs = _playedFrames * 20 });
                    }
                }
                else if (_playing && _responseDone && _outgoing.Count == 0)
                {
                    _playing = false;
                    Enqueue(new { type = "agentAudioState", state = "idle", playedMs = _playedFrames * 20 });
                }
            }
        }
    }

    private sealed record ParticipantIdentity(string ParticipantId, Guid? ObjectId, string DisplayName);

    private string CallStateText() => _inLobby ? "Lobby" : _call?.Resource.State?.ToString() ?? "Joining";

    private ParticipantIdentity[] HumanParticipants() =>
        _participants.Values.Where(p => p.ParticipantId != _call?.Resource.MyParticipantId).ToArray();
}
