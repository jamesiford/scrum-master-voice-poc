using System.Net.WebSockets;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Api.Voice;

/// <summary>
/// Non-recording Teams gateway. Uses the shared pure floor control but has no mediator, repositories,
/// tools or persistence adapters: audio-derived text cannot reach a store (ADR 0023).
/// </summary>
public sealed class GraphMediaVoiceGateway(
    IVoiceSessionFactory voices,
    IClock clock,
    LiveSessionRegistry live,
    IOptions<VoiceGatewayOptions> options,
    ILogger<GraphMediaVoiceGateway> logger)
{
    private readonly Channel<object> _events = Channel.CreateBounded<object>(200);
    private BridgeConnection _bridge = null!;
    private IVoiceSession _voice = null!;
    private MediaSessionContext _context = null!;
    private FloorControl _floor = null!;
    private CancellationToken _ct;
    private string _speaker = "guest";
    private string _speechSpeaker = "guest";
    private string? _guestSpeaker;
    private DateTimeOffset? _guestSpeechAt;
    private DateTimeOffset _lastGuestInput;
    private readonly Queue<string> _speakers = new();
    private string? _itemId;
    private string? _interruptedItemId;
    private DateTimeOffset? _speechAt;
    private DateTimeOffset _lastInput;
    private bool _discard;
    private bool _finished;
    private long _playedMs;
    private long _turnStarted;
    private string _callState = "Joining";
    private object[] _participants = [];
    private readonly List<string> _parkingLot = [];
    private DateTimeOffset _nextUi;

    /// <summary>Runs an ephemeral conversation; returns true only when the floor finishes, not on a reconnect.</summary>
    /// <param name="bridge">Authenticated media host connection.</param>
    /// <param name="context">Read-only team facts.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Whether the session ended normally.</returns>
    public async Task<bool> RunAsync(BridgeConnection bridge, MediaSessionContext context, CancellationToken ct)
    {
        _bridge = bridge;
        _context = context;
        foreach (var member in context.Roster.Where(m => m.Active && m.TeamId == context.Session.TeamId).OrderBy(m => m.SpeakingOrder))
        {
            _speakers.Enqueue(member.Id);
        }
        _floor = new FloorControl(new FloorOptions(TimeSpan.FromMinutes(context.Session.StandupMinutes),
            options.Value.SilenceMs, options.Value.ParkAfterSeconds * 1000, options.Value.NoResponseSeconds * 1000,
            PauseMs: options.Value.PauseMs, MaxClarifications: 1));
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ct = stop.Token;
        await using var voice = await voices.CreateAsync(new VoiceSessionConfig(context.Instructions, [], 600,
            context.Session.TeamId, context.Session.Id), _ct);
        _voice = voice;
        if (!live.TryRegister(context.Session.Id, action =>
        {
            if (!_events.Writer.TryWrite(action)) { stop.Cancel(); }
        }))
        {
            throw new WebSocketException("This session already has a gateway connection.");
        }
        PushState();
        var tasks = new[] { ReadBridgeAsync(), ReadVoiceAsync(), TickAsync() };
        try
        {
            await _bridge.SendAsync(new { type = "hello", teamId = context.Session.TeamId, recording = false }, _ct);
            while (!_finished)
            {
                var pending = _events.Reader.ReadAsync(_ct).AsTask();
                var completed = await Task.WhenAny(tasks.Append(pending));
                if (completed != pending)
                {
                    await completed;
                    throw new WebSocketException("A media gateway worker stopped.");
                }

                var input = await pending;
                switch (input)
                {
                    case JsonElement json: await OnBridgeAsync(json); break;
                    case VoiceEvent ev: await OnVoiceAsync(ev); break;
                    case DateTimeOffset now: await OnTickAsync(now); break;
                    case string control: await OnControlAsync(control); break;
                }

                if (input is not DateTimeOffset || clock.UtcNow >= _nextUi) { PushState(); }
            }
        }
        catch (OperationCanceledException) when (_ct.IsCancellationRequested) { }
        catch (WebSocketException)
        {
            logger.LogWarning("Media gateway disconnected for {TeamId}/{SessionId}", context.Session.TeamId, context.Session.Id);
        }
        finally
        {
            await stop.CancelAsync();
            try { await Task.WhenAll(tasks); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            catch (WebSocketException) { logger.LogWarning("Media transport ended for {TeamId}/{SessionId}", context.Session.TeamId, context.Session.Id); }
            live.Unregister(context.Session.Id);
        }

        return _finished;
    }

    private async Task ReadBridgeAsync()
    {
        var buffer = new byte[64 * 1024];
        while (await _bridge.ReceiveAsync(buffer, _ct) is { } frame)
        {
            if (frame.Type != WebSocketMessageType.Text) { throw new WebSocketException("GraphMedia requires identity-tagged JSON audio."); }
            using var json = JsonDocument.Parse(frame.Data);
            await _events.Writer.WriteAsync(json.RootElement.Clone(), _ct);
        }

        await _events.Writer.WriteAsync("disconnected", _ct);
    }

    private async Task ReadVoiceAsync()
    {
        await foreach (var ev in _voice.ReadEventsAsync(_ct))
        {
            await _events.Writer.WriteAsync(ev, _ct);
        }

        await _events.Writer.WriteAsync("disconnected", _ct);
    }

    private async Task TickAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
        while (await timer.WaitForNextTickAsync(_ct))
        {
            await _events.Writer.WriteAsync(clock.UtcNow, _ct);
        }
    }

    private async Task OnBridgeAsync(JsonElement json)
    {
        switch (json.GetProperty("type").GetString())
        {
            case "audio":
                var frames = json.GetProperty("items").EnumerateArray().ToArray();
                if (frames.Length == 0) { return; }
                var frame = frames.MaxBy(f => f.GetProperty("amplitude").GetInt32());
                var objectId = frame.GetProperty("participantObjectId");
                var member = ParticipantAttribution.Resolve(_context.Session.TeamId,
                    objectId.ValueKind == JsonValueKind.String && Guid.TryParse(objectId.GetString(), out var id) ? id : null, _context.Roster);
                var pcm = Convert.FromBase64String(frame.GetProperty("pcm").GetString()!);
                if (pcm.Length != 960) { throw new WebSocketException("GraphMedia requires 20 ms PCM24K."); }
                if (member is not null)
                {
                    _speaker = member.Id;
                    _lastInput = clock.UtcNow;
                    _guestSpeechAt = null;
                    await _voice.SendAudioAsync(pcm, _ct);
                }
                else if (frame.GetProperty("amplitude").GetInt32() >= 300)
                {
                    var guest = "guest:" + frame.GetProperty("participantId").GetString();
                    if (_guestSpeaker != guest) { _guestSpeechAt = null; }
                    _guestSpeaker = guest;
                    _lastGuestInput = clock.UtcNow;
                    _guestSpeechAt ??= clock.UtcNow;
                }
                else { _guestSpeechAt = null; }

                break;
            case "participants":
                var guests = json.GetProperty("items").EnumerateArray().Select(p =>
                {
                    var objectId = p.GetProperty("objectId");
                    var member = ParticipantAttribution.Resolve(_context.Session.TeamId,
                        objectId.ValueKind == JsonValueKind.String && Guid.TryParse(objectId.GetString(), out var id) ? id : null, _context.Roster);
                    return new { participantId = p.GetProperty("participantId").GetString(), memberId = member?.Id, displayName = p.GetProperty("displayName").GetString(), guest = member is null };
                }).ToArray();
                _participants = guests;
                break;
            case "callState":
                var state = json.GetProperty("state").GetString();
                _callState = state ?? "Unknown";
                if (state == "Established" && _floor.State == FloorState.Idle)
                {
                    await ExecuteAsync(_floor.Handle(new StartFloor(clock.UtcNow)));
                }

                break;
            case "agentAudioState":
                _playedMs = json.TryGetProperty("playedMs", out var played) ? played.GetInt64() : _playedMs;
                if (json.TryGetProperty("interrupted", out var wasInterrupted) && wasInterrupted.GetBoolean())
                {
                    if (_interruptedItemId is { } interrupted)
                    {
                        _interruptedItemId = null;
                        await _voice.TruncateAsync(interrupted, (int)Math.Min(_playedMs, int.MaxValue), _ct);
                    }
                }
                else
                {
                    await ExecuteAsync(_floor.Handle(new AgentSpeechEnded(clock.UtcNow)));
                }

                break;
            case "audioProgress":
                _playedMs = json.GetProperty("playedMs").GetInt64();
                break;
        }
    }

    private async Task OnVoiceAsync(VoiceEvent ev)
    {
        switch (ev)
        {
            case AgentResponseStarted:
                if (!_floor.AgentHasFloor) { await _voice.CancelResponseAsync(_ct); _discard = true; break; }
                _discard = false;
                _playedMs = 0;
                await _bridge.SendAsync(new { type = "responseStarted" }, _ct);
                break;
            case AgentItemStarted item: _itemId = item.ItemId; break;
            case AgentAudio audio when !_discard:
                await _bridge.SendAudioAsync(audio.Pcm, _ct);
                if (_turnStarted != 0)
                {
                    logger.LogInformation("Media turn {TeamId}/{SessionId} {MemberId} {State}, latency {LatencyMs} ms",
                        _context.Session.TeamId, _context.Session.Id, _floor.CurrentMemberId, _floor.State,
                        Stopwatch.GetElapsedTime(_turnStarted).TotalMilliseconds);
                    _turnStarted = 0;
                }

                break;
            case ResponseDone when !_discard: await _bridge.SendAsync(new { type = "responseDone" }, _ct); break;
            case UserSpeechStarted:
                _speechAt = clock.UtcNow;
                _speechSpeaker = _speaker;
                await ExecuteAsync(_floor.Handle(new SpeechActive(_speechSpeaker, true, clock.UtcNow)));
                break;
            case UserSpeechStopped:
                _speechAt = null;
                await ExecuteAsync(_floor.Handle(new SpeechActive(_speechSpeaker, false, clock.UtcNow)));
                await ExecuteAsync(_floor.Handle(new MemberSpoke(_speechSpeaker, 20, clock.UtcNow)));
                break;
            case VoiceFault:
                logger.LogError("Voice service failed for {TeamId}/{SessionId}", _context.Session.TeamId, _context.Session.Id);
                throw new WebSocketException("Voice service disconnected.");
        }
    }

    private async Task OnTickAsync(DateTimeOffset now)
    {
        if (now - _lastGuestInput > TimeSpan.FromMilliseconds(40)) { _guestSpeechAt = null; }
        if (now - _lastInput > TimeSpan.FromMilliseconds(40))
        {
            await _voice.SendAudioAsync(new byte[960], _ct);
        }

        if (_speechAt is { } start && _floor.AgentHasFloor && now - start >= TimeSpan.FromMilliseconds(options.Value.BargeInMs))
        {
            await ExecuteAsync(_floor.Handle(new BargeIn(_speechSpeaker, IsHuman: false, Sustained: true)));
            _speechAt = null;
        }

        if (_guestSpeechAt is { } guestStart && _floor.AgentHasFloor && now - guestStart >= TimeSpan.FromMilliseconds(options.Value.BargeInMs))
        {
            await ExecuteAsync(_floor.Handle(new BargeIn(_guestSpeaker!, IsHuman: false, Sustained: true)));
            _guestSpeechAt = null;
        }

        await ExecuteAsync(_floor.Handle(new Tick(now)));
    }

    private async Task OnControlAsync(string action)
    {
        var actions = action switch
        {
            "takeover" => _floor.Handle(new HumanOverride(true)),
            "release" => _floor.Handle(new HumanOverride(false)),
            "mute" => _floor.Handle(new PauseFloor(true)),
            "unmute" => _floor.Handle(new PauseFloor(false)),
            "skip" => _floor.Handle(new SkipSpeaker()),
            "end" => [new EndSession()],
            "disconnected" => throw new WebSocketException("Media connection closed."),
            _ => Array.Empty<FloorAction>(),
        };
        await ExecuteAsync(actions);
    }

    private async Task ExecuteAsync(IReadOnlyList<FloorAction> actions)
    {
        foreach (var action in actions)
        {
            switch (action)
            {
                case AgentSpeak speak:
                    var brief = _context.Session.PreBrief!.Members.FirstOrDefault(m => m.MemberId == speak.MemberId);
                    var turn = JsonSerializer.Serialize(new { action = speak.Reason.ToString(), member = brief });
                    _itemId = null;
                    _turnStarted = Stopwatch.GetTimestamp();
                    await _voice.CreateResponseAsync(turn, _ct);
                    break;
                case AdvanceSpeaker:
                    await ExecuteAsync(_floor.Handle(new SpeakerSelected(_speakers.TryDequeue(out var next) ? next : null, Absent: false)));
                    break;
                case DeferSpeaker defer: _speakers.Enqueue(defer.MemberId); break;
                case ParkTopic:
                    if (_floor.CurrentMemberId is { } parked) { _parkingLot.Add(parked); }
                    break;
                case RunReconcile reconcile:
                    // No persistence or decision-model command in non-recording mode; the voice context stays in memory.
                    await ExecuteAsync(_floor.Handle(new ReconcileFinished(reconcile.Version, HasQuestion: true)));
                    break;
                case CancelResponse:
                    _discard = true;
                    _interruptedItemId = _itemId;
                    await _bridge.SendAsync(new { type = "stopAudio" }, _ct);
                    await _voice.CancelResponseAsync(_ct);
                    break;
                case EndSession: _finished = true; break;
            }
        }
    }

    private void PushState()
    {
        var elapsed = _context.Session.StartedAt is { } started ? (clock.UtcNow - started).TotalSeconds : 0;
        live.Update(_context.Session.Id, JsonSerializer.Serialize(new
        {
            teamId = _context.Session.TeamId,
            recording = false,
            callState = _callState,
            participants = _participants,
            floor = _floor.State.ToString(),
            currentSpeaker = _context.Roster.FirstOrDefault(m => m.Id == _floor.CurrentMemberId)?.DisplayName,
            agentSpeaking = _floor.AgentHasFloor,
            remainingSec = Math.Max(0, (int)(_context.Session.StandupMinutes * 60 - elapsed)),
            parkingLot = _parkingLot,
            absent = Array.Empty<string>(),
            p50 = 0,
            p95 = 0,
            running = !_finished,
            ended = _finished,
        }));
        _nextUi = clock.UtcNow.AddMilliseconds(500);
    }
}
