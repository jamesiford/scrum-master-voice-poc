using System.Net.WebSockets;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Voice;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Application.Behaviors;

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
    private DateTimeOffset? _vadStartedAt;
    private DateTimeOffset _lastInput;
    private bool _discard;
    private bool _finished;
    private long _playedMs;
    private long _turnStarted;
    private string _callState = "Joining";
    private object[] _participants = [];
    private readonly List<string> _parkingLot = [];
    private DateTimeOffset _nextUi;
    private EphemeralMediaBoard _board = null!;
    private string? _lastBoard;
    private string _boardState = "Waiting";
    private string? _boardFailure;

    /// <summary>Runs an ephemeral conversation; returns true only when the floor finishes, not on a reconnect.</summary>
    /// <param name="bridge">Authenticated media host connection.</param>
    /// <param name="context">Read-only team facts.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Whether the session ended normally.</returns>
    public async Task<bool> RunAsync(BridgeConnection bridge, MediaSessionContext context, CancellationToken ct)
    {
        _bridge = bridge;
        _context = context;
        using var sessionHop = ScrumTelemetry.StartHop("media.session", context.Session.TeamId, context.Session.Id, context.Session.CorrelationId);
        _boardState = context.BoardMode == "MediaShare" ? "Waiting" : "Disabled (Board:Mode=Share)";
        _board = new EphemeralMediaBoard(context.Session, context.JoinUrl);
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
            await PushBoardAsync();
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
                var previousFloor = _floor.State;
                var previousMember = _floor.CurrentMemberId;
                switch (input)
                {
                    case JsonElement json:
                        using (var frameHop = json.TryGetProperty("traceParent", out var trace)
                            && ActivityContext.TryParse(trace.GetString(), null, out var parent)
                            && parent.TraceId.ToHexString() == context.Session.CorrelationId
                            ? ScrumTelemetry.Source.StartActivity("media.frame.receive", ActivityKind.Consumer, parent,
                                new ActivityTagsCollection { [ScrumTelemetry.TeamTag] = context.Session.TeamId, ["session.id"] = context.Session.Id.ToString(), ["hop"] = "media.frame.receive" })
                            : null)
                        {
                            await OnBridgeAsync(json);
                        }
                        break;
                    case VoiceEvent ev: await OnVoiceAsync(ev); break;
                    case DateTimeOffset now: await OnTickAsync(now); break;
                    case string control: await OnControlAsync(control); break;
                }

                if (input is not DateTimeOffset || clock.UtcNow >= _nextUi) { PushState(); }
                if (input is not DateTimeOffset || previousFloor != _floor.State || previousMember != _floor.CurrentMemberId || _finished)
                {
                    await PushBoardAsync();
                }
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
            try
            {
                try { await Task.WhenAll(tasks); }
                catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
                catch (WebSocketException) { logger.LogWarning("Media transport ended for {TeamId}/{SessionId}", context.Session.TeamId, context.Session.Id); }
            }
            finally
            {
                while (_events.Reader.TryRead(out _)) { }
                _participants = [];
                _parkingLot.Clear();
                _lastBoard = null;
                _board = null!;
                live.Unregister(context.Session.Id);
            }
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
                var sources = frames.Select(f =>
                {
                    var objectId = f.GetProperty("participantObjectId");
                    var member = ParticipantAttribution.Resolve(_context.Session.TeamId,
                        objectId.ValueKind == JsonValueKind.String && Guid.TryParse(objectId.GetString(), out var id) ? id : null, _context.Roster);
                    return new FloorAudioSource(f.GetProperty("participantId").GetString()!, member?.Id, f.GetProperty("amplitude").GetInt32());
                }).ToArray();
                var selected = _floor.SelectAudioSource(sources);
                var guest = sources.Where(s => s.MemberId is null && s.Amplitude >= 300).MaxBy(s => s.Amplitude);
                await SetGuestSpeechAsync(guest is null ? null : "guest:" + guest.SourceId);
                if (guest is not null)
                {
                    _lastGuestInput = clock.UtcNow;
                    _guestSpeechAt ??= clock.UtcNow;
                }

                if (selected is not null)
                {
                    var frame = frames.First(f => f.GetProperty("participantId").GetString() == selected.SourceId);
                    var pcm = Convert.FromBase64String(frame.GetProperty("pcm").GetString()!);
                    if (pcm.Length != 960) { throw new WebSocketException("GraphMedia requires 20 ms PCM24K."); }
                    _speaker = selected.MemberId!;
                    _lastInput = clock.UtcNow;
                    using var forwarding = Activity.Current?.OperationName == "media.frame.receive"
                        ? ScrumTelemetry.StartHop("media.voice.send", _context.Session.TeamId, _context.Session.Id, _context.Session.CorrelationId)
                        : null;
                    await _voice.SendAudioAsync(pcm, _ct);
                }
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
                _board.SetParticipants(guests.Where(p => p.memberId is not null).Select(p => p.memberId!).ToHashSet(StringComparer.Ordinal));
                break;
            case "boardStatus":
                _boardState = json.GetProperty("state").GetString() ?? "Unknown";
                _boardFailure = json.TryGetProperty("message", out var message) ? message.GetString() : null;
                break;
            case "callState":
                var state = json.GetProperty("state").GetString();
                _callState = state ?? "Unknown";
                if (state == "Established" && _floor.State == FloorState.Idle)
                {
                    ScrumTelemetry.Count("join", _context.Session.TeamId, "success");
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
                    ScrumTelemetry.RecordHop("media.first_audio", _context.Session.TeamId, Stopwatch.GetElapsedTime(_turnStarted).TotalMilliseconds);
                    ScrumTelemetry.Count("turn", _context.Session.TeamId, "first_audio");
                    logger.LogInformation("Media turn {TeamId}/{SessionId} {MemberId} {State}, latency {LatencyMs} ms",
                        _context.Session.TeamId, _context.Session.Id, _floor.CurrentMemberId, _floor.State,
                        Stopwatch.GetElapsedTime(_turnStarted).TotalMilliseconds);
                    _turnStarted = 0;
                }

                break;
            case ResponseDone when !_discard: await _bridge.SendAsync(new { type = "responseDone" }, _ct); break;
            case UserSpeechStarted:
                _speechAt = clock.UtcNow;
                _vadStartedAt = clock.UtcNow;
                _speechSpeaker = _speaker;
                await ExecuteAsync(_floor.Handle(new SpeechActive(_speechSpeaker, true, clock.UtcNow)));
                break;
            case UserSpeechStopped:
                _speechAt = null;
                var duration = _vadStartedAt is { } began ? (int)Math.Clamp((clock.UtcNow - began).TotalMilliseconds, 0, int.MaxValue) : 0;
                _vadStartedAt = null;
                await ExecuteAsync(_floor.Handle(new SpeechActive(_speechSpeaker, false, clock.UtcNow)));
                await ExecuteAsync(_floor.Handle(new MemberSpoke(_speechSpeaker, duration, clock.UtcNow)));
                break;
            case UserTranscript transcript:
                _board.ApplyTranscript(_speechSpeaker, transcript.Text, _floor.State == FloorState.Clarifying, clock.UtcNow);
                break;
            case VoiceFault:
                logger.LogError("Voice service failed for {TeamId}/{SessionId}", _context.Session.TeamId, _context.Session.Id);
                throw new WebSocketException("Voice service disconnected.");
        }
    }

    private async Task OnTickAsync(DateTimeOffset now)
    {
        if (now - _lastGuestInput > TimeSpan.FromMilliseconds(40)) { await SetGuestSpeechAsync(null); }
        if (now - _lastInput > TimeSpan.FromMilliseconds(40))
        {
            await _voice.SendAudioAsync(new byte[960], _ct);
        }

        if (_speechAt is { } start && _floor.AgentHasFloor && now - start >= TimeSpan.FromMilliseconds(options.Value.BargeInMs))
        {
            await ExecuteAsync(_floor.Handle(new BargeIn(_speechSpeaker, IsHuman: false, Sustained: true)));
            await ExecuteAsync(_floor.Handle(new SpeechActive(_speechSpeaker, true, now)));
            _speechAt = null;
        }

        if (_guestSpeechAt is { } guestStart && _floor.AgentHasFloor && now - guestStart >= TimeSpan.FromMilliseconds(options.Value.BargeInMs))
        {
            await ExecuteAsync(_floor.Handle(new BargeIn(_guestSpeaker!, IsHuman: false, Sustained: true)));
            await ExecuteAsync(_floor.Handle(new SpeechActive(_guestSpeaker!, true, now)));
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

    private async Task SetGuestSpeechAsync(string? guest)
    {
        if (_guestSpeaker is { } previous && previous != guest)
        {
            await ExecuteAsync(_floor.Handle(new SpeechActive(previous, false, clock.UtcNow)));
            _guestSpeechAt = null;
        }

        _guestSpeaker = guest;
        if (guest is not null)
        {
            await ExecuteAsync(_floor.Handle(new SpeechActive(guest, true, clock.UtcNow)));
        }
    }

    private async Task ExecuteAsync(IReadOnlyList<FloorAction> actions)
    {
        foreach (var action in actions)
        {
            switch (action)
            {
                case AgentSpeak speak:
                    var brief = _context.Session.PreBrief!.Members.FirstOrDefault(m => m.MemberId == speak.MemberId);
                    var turn = JsonSerializer.Serialize(new
                    {
                        action = speak.Reason.ToString(),
                        member = brief,
                        kind = _context.Session.Kind.ToString(),
                        phase = _board.InActions ? "Actions" : "Round",
                        retro = _context.Session.Kind == CeremonyKind.Retrospective ? _board.Project(_floor.CurrentMemberId, true, false) : null,
                    });
                    _itemId = null;
                    _turnStarted = Stopwatch.GetTimestamp();
                    await _voice.CreateResponseAsync(turn, _ct);
                    break;
                case AdvanceSpeaker:
                    if (_context.Session.Kind == CeremonyKind.Retrospective)
                    {
                        _board.CompleteTurn(_floor.CurrentMemberId);
                        var step = _board.NextRetroStep();
                        if (step is not null) { _board.SelectRetroStep(step, clock.UtcNow); }
                        await ExecuteAsync(step switch
                        {
                            Announcement announcement => _floor.Handle(new AnnounceStep(announcement.Reason)),
                            MemberTurn member => _floor.Handle(new SpeakerSelected(member.Member.MemberId, _board.IsAbsent(member.Member.MemberId), CircleBack: member.CircleBack)),
                            _ => _floor.Handle(new SpeakerSelected(null, Absent: false)),
                        });
                    }
                    else
                    {
                        var next = _speakers.TryDequeue(out var selected) ? selected : null;
                        await ExecuteAsync(_floor.Handle(new SpeakerSelected(next, Absent: next is not null && _board.IsAbsent(next))));
                    }

                    break;
                case DeferSpeaker defer:
                    _speakers.Enqueue(defer.MemberId);
                    _board.SetStatus(defer.MemberId, defer.Reason == DeferReason.NoResponse ? "no response" : "away");
                    break;
                case ParkTopic:
                    if (_floor.CurrentMemberId is { } parked) { _parkingLot.Add(parked); }
                    break;
                case RunReconcile reconcile:
                    // No persistence or decision-model command in non-recording mode; the voice context stays in memory.
                    await ExecuteAsync(_floor.Handle(new ReconcileFinished(reconcile.Version, HasQuestion: true)));
                    break;
                case CancelResponse:
                    ScrumTelemetry.Count("barge_in", _context.Session.TeamId, "cancelled");
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
            boardState = _boardState,
            boardFailure = _boardFailure,
        }));
        _nextUi = clock.UtcNow.AddMilliseconds(500);
    }

    private async Task PushBoardAsync()
    {
        var board = _board.Project(_floor.CurrentMemberId, _floor.State != FloorState.Idle, _finished);
        var json = JsonSerializer.Serialize(board);
        if (json == _lastBoard) { return; }
        _lastBoard = json;
        await _bridge.SendAsync(board, _ct);
    }
}
