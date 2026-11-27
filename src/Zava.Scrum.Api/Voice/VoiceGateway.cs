using System.Text.Json;
using System.Threading.Channels;
using Zava.Scrum.Api.Endpoints;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Retrospectives.GetRetroBoard;
using Zava.Scrum.Application.Features.Retrospectives.NameRetroThemes;
using Zava.Scrum.Application.Features.Retrospectives.ReconcileRetroUpdate;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Application.Features.Standups.CompleteSpeakerTurn;
using Zava.Scrum.Application.Features.Standups.EndStandup;
using Zava.Scrum.Application.Features.Standups.GetFacilitatorInstructions;
using Zava.Scrum.Application.Features.Standups.GetNextSpeaker;
using Zava.Scrum.Application.Features.Standups.GetSession;
using Zava.Scrum.Application.Features.Standups.GetTurnInstruction;
using Zava.Scrum.Application.Features.Standups.MarkAbsent;
using Zava.Scrum.Application.Features.Standups.ParkTopic;
using Zava.Scrum.Application.Features.Standups.RecordAgentLatency;
using Zava.Scrum.Application.Features.Standups.RecordStandupEvent;
using Zava.Scrum.Application.Features.Standups.ReconcileUpdate;
using Zava.Scrum.Application.Features.Standups.ResolveUnblocker;
using Zava.Scrum.Application.Features.Standups.RetractStandupEvent;
using Zava.Scrum.Application.Features.Voice.AnalyseUtterance;
using Zava.Scrum.Application.Tools;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Zava.Scrum.Application.Behaviors;

namespace Zava.Scrum.Api.Voice;

/// <summary>
/// The voice gateway (SPEC 11): one Voice Live session per standup, wired to a bridge websocket.
/// It owns no decisions. <see cref="FloorControl"/> decides when the agent speaks, queries decide what is said and who is next,
/// and this class only moves events in and actions out. Audio frames bypass the mediator for latency (SPEC 6.1.1).
/// </summary>
public sealed class VoiceGateway(
    ISender sender,
    IScrumTools tools,
    IVoiceSessionFactory voiceFactory,
    IRetroSettings retro,
    IClock clock,
    LiveSessionRegistry live,
    IOptions<VoiceGatewayOptions> options,
    ILogger<VoiceGateway> logger)
{
    private readonly Channel<Msg> _inbox = Channel.CreateUnbounded<Msg>();
    private readonly object _gate = new();
    private readonly Dictionary<string, System.Text.StringBuilder> _utterance = [];
    private readonly Dictionary<string, (string Text, Task<Result<ReconcileResult>> Task)> _speculative = [];
    private readonly Dictionary<string, string?> _pendingQuestion = [];
    private readonly Dictionary<string, string> _asked = [];
    private readonly Dictionary<string, int> _focus = [];
    private readonly Dictionary<string, string> _proxy = [];
    private readonly Dictionary<string, string> _status = [];
    private readonly Dictionary<string, int> _noResponses = [];
    private readonly HashSet<string> _deferredNow = [];
    private readonly HashSet<string> _correcting = [];
    private readonly HashSet<string> _redirected = [];
    private readonly HashSet<string> _askUnblocker = [];
    private readonly HashSet<string> _unblockerAsked = [];
    private readonly HashSet<string> _awaitingUnblocker = [];
    private List<NameRef> _roster = [];
    private string? _directive;
    private string? _deferNotes;
    private string? _speechSpeaker;
    private string? _lastActiveId;
    private DateTimeOffset _lastActiveAt;
    private readonly Dictionary<string, int?> _pendingItem = [];
    private readonly List<string> _offTopic = [];
    private readonly List<string> _parking = [];
    private readonly List<int> _latencies = [];
    private readonly HashSet<string> _absent = [];
    private BridgeConnection _bridge = null!;
    private IVoiceSession _voice = null!;
    private FloorControl _floor = null!;
    private SessionDto _session = null!;
    private Guid _sessionId;
    private CancellationToken _run;
    private string _presenter = string.Empty;
    private bool _begun;
    private bool _finished;
    private bool _responseDone;
    private bool _audioIdle;
    private bool _audioStarted;
    private int _firstAudioPending;
    private DateTimeOffset _beganAt;
    private DateTimeOffset? _lastHumanEnd;
    private DateTimeOffset? _latencyBaseline;
    private DateTimeOffset _createSentAt;
    private DateTimeOffset? _speechStartedAt;
    private DateTimeOffset? _bargeCheckAt;
    private DateTimeOffset? _bargeSustainedAt;
    private string? _bargeSpeaker;
    private int _discardAgentAudio;
    private volatile string? _agentItemId;
    private DateTimeOffset? _firstAudioAt;
    private int _turnTranscriptMs;
    private int _turnAnalysisMs;
    private int _turnModelMs;
    private string _turnSource = "none";
    private DateTimeOffset? _truncatedAt;
    private (AgentSpeak Speak, string? Question)? _lastSpeak;
    private (string ByMemberId, string Saying)? _interrupted;
    private readonly Dictionary<string, (List<int> Items, List<ItemComment> Comments, bool AllModel)> _analysis = [];
    private DateTimeOffset _lastUi;
    private int _pendingTranscripts;
    private DateTimeOffset _lastStoppedAt;
    private readonly HashSet<string> _offTopicSpeakers = [];
    private readonly HashSet<int> _covered = [];
    private string? _lastBoard;
    private readonly Dictionary<string, string> _noted = [];

    private abstract record Msg;

    private sealed record FromBridge(JsonElement Json) : Msg;

    private sealed record FromVoice(VoiceEvent Event) : Msg;

    private sealed record FirstAudio : Msg;

    private sealed record Clock : Msg;

    private sealed record ReconcileDone(string MemberId, int Version, ReconcileResult? Result, bool Background) : Msg;

    private sealed record BridgeClosed : Msg;

    private sealed record FromControl(string Action) : Msg;

    /// <summary>Runs the gateway until the standup ends or the bridge disconnects.</summary>
    /// <param name="bridge">The bridge websocket.</param>
    /// <param name="sessionId">The session.</param>
    /// <param name="ct">Cancellation token (request aborted).</param>
    /// <returns>A task.</returns>
    public async Task RunAsync(BridgeConnection bridge, Guid sessionId, CancellationToken ct)
    {
        _bridge = bridge;
        _sessionId = sessionId;
        using var run = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _run = run.Token;

        if (!await PrepareAsync(ct))
        {
            return;
        }

        // One structured scope per run (set here, not in PrepareAsync, because a scope set in a callee does not flow back), so every line the gateway writes for a turn carries the session's correlation id (ADR 0017).
        using var correlation = logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = _session.CorrelationId, ["SessionId"] = sessionId, ["TeamId"] = _session.TeamId });
        using var sessionHop = ScrumTelemetry.StartHop("voice.session", _session.TeamId, sessionId, _session.CorrelationId);
        WarmUpAnalyser();
        var readers = new[] { ReadBridgeAsync(), ReadVoiceAsync(), TickAsync() };
        live.Register(sessionId, a => _inbox.Writer.TryWrite(new FromControl(a)));
        try
        {
            await foreach (var msg in _inbox.Reader.ReadAllAsync(_run))
            {
                await HandleAsync(msg);
                if (_finished)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The bridge went away or the host is stopping.
        }
        finally
        {
            live.Unregister(sessionId);
            await run.CancelAsync();
            await Task.WhenAll(readers.Select(r => r.ContinueWith(_ => { }, TaskScheduler.Default)));
            await _voice.DisposeAsync();
            await sender.Send(new EndStandupCommand(sessionId), CancellationToken.None);
            await _bridge.CloseAsync();
        }
    }

    // The first call to the decision model pays for the token and the connection (about 2 s); do it now, while the room is still joining,
    // so the first real utterance does not.
    private void WarmUpAnalyser()
    {
        if (_roster.Count == 0)
        {
            return;
        }

        var speaker = _roster[0].MemberId;
        _ = Task.Run(() => sender.Send(new AnalyseUtteranceQuery(_sessionId, speaker, speaker, "hello", _roster), _run), _run);
    }

    private async Task<bool> PrepareAsync(CancellationToken ct)
    {
        var session = await sender.Send(new GetSessionQuery(_sessionId), ct);
        if (!session.IsSuccess)
        {
            await Fail("SESSION_NOT_FOUND", session.Error!.Message);
            return false;
        }

        if (session.Value.Status != StandupStatus.Live || session.Value.PreBrief is null)
        {
            await Fail("SESSION_NOT_LIVE", "Start the session from the portal (Sessions > Start in Simulated Room) before connecting.");
            return false;
        }

        _session = session.Value;
        using var connectHop = ScrumTelemetry.StartHop("voice.connect", _session.TeamId, _sessionId, _session.CorrelationId);
        _roster = _session.PreBrief?.Members.Select(m => new NameRef(m.MemberId, m.DisplayName)).ToList() ?? [];
        var o = options.Value;
        _floor = new FloorControl(new FloorOptions(TimeSpan.FromMinutes(_session.StandupMinutes), o.SilenceMs, o.ParkAfterSeconds * 1000, o.NoResponseSeconds * 1000, PauseMs: o.PauseMs));

        var instructions = await sender.Send(new GetFacilitatorInstructionsQuery(_sessionId), ct);
        if (!instructions.IsSuccess)
        {
            await Fail("PROMPT_MISSING", instructions.Error!.Message);
            return false;
        }

        try
        {
            _voice = await voiceFactory.CreateAsync(new VoiceSessionConfig(instructions.Value, tools.FacilitatorDefinitions, 600, _session.TeamId, _sessionId), ct);
            _instructions = instructions.Value;
        }
        catch (Exception ex) when (ex is System.Net.WebSockets.WebSocketException or HttpRequestException or Azure.Identity.AuthenticationFailedException or Azure.Identity.CredentialUnavailableException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Voice Live connection failed for session {SessionId}", _sessionId);
            await Fail("VOICE_UNAVAILABLE", "Voice Live is unavailable: " + ex.Message);
            return false;
        }

        await _bridge.SendAsync(
            new
            {
                type = "hello",
                teamId = _session.TeamId,
                traceParent = connectHop?.Id,
                sessionId = _sessionId,
                mode = _session.Mode,
                kind = _session.Kind.ToString(),
                version = 1,
                standupMinutes = _session.StandupMinutes,
                members = _roster.Select(m => new { memberId = m.MemberId, displayName = m.DisplayName }),
            },
            ct);
        return true;
    }

    private string _instructions = string.Empty;

    private async Task Fail(string code, string message)
    {
        await _bridge.SendAsync(new { type = "error", code, message }, CancellationToken.None);
        await _bridge.CloseAsync();
    }

    // ---- readers -------------------------------------------------------------------------------------------------

    private async Task ReadBridgeAsync()
    {
        var buffer = new byte[64 * 1024];
        try
        {
            while (!_run.IsCancellationRequested)
            {
                var message = await _bridge.ReceiveAsync(buffer, _run);
                if (message is null)
                {
                    break;
                }

                if (message.Value.Type == System.Net.WebSockets.WebSocketMessageType.Binary)
                {
                    await _voice.SendAudioAsync(message.Value.Data, _run);
                    continue;
                }

                using var doc = JsonDocument.Parse(message.Value.Data);
                _inbox.Writer.TryWrite(new FromBridge(doc.RootElement.Clone()));
            }
        }
        catch (Exception ex) when (ex is System.Net.WebSockets.WebSocketException or OperationCanceledException or JsonException)
        {
            logger.LogDebug(ex, "Bridge reader ended");
        }

        _inbox.Writer.TryWrite(new BridgeClosed());
    }

    private async Task ReadVoiceAsync()
    {
        for (var attempt = 0; !_run.IsCancellationRequested; attempt++)
        {
            VoiceFault? fault = null;
            await foreach (var ev in _voice.ReadEventsAsync(_run))
            {
                switch (ev)
                {
                    case AgentAudio audio:
                        // After a barge-in the service may still deliver audio it had already generated; playing it is the agent talking on.
                        if (Volatile.Read(ref _discardAgentAudio) == 1)
                        {
                            break;
                        }

                        await _bridge.SendAudioAsync(audio.Pcm, _run);
                        if (Interlocked.Exchange(ref _firstAudioPending, 0) == 1)
                        {
                            _inbox.Writer.TryWrite(new FirstAudio());
                        }

                        break;
                    case AgentResponseStarted:
                        Interlocked.Exchange(ref _discardAgentAudio, 0);
                        break;
                    case AgentItemStarted item:
                        _agentItemId = item.ItemId;
                        break;
                    case VoiceFault f:
                        fault = f;
                        break;
                    default:
                        _inbox.Writer.TryWrite(new FromVoice(ev));
                        break;
                }
            }

            if (fault is null || _run.IsCancellationRequested || attempt >= 2 || !await TryReconnectAsync(fault))
            {
                if (fault is not null)
                {
                    _inbox.Writer.TryWrite(new FromVoice(fault));
                }

                return;
            }
        }
    }

    private async Task<bool> TryReconnectAsync(VoiceFault fault)
    {
        await _bridge.SendAsync(new { type = "error", code = "VOICE_RETRYING", message = "Voice Live disconnected - retrying" }, CancellationToken.None);
        logger.LogWarning("{Fault}; reconnecting", fault.Message);
        try
        {
            await Task.Delay(1500, _run);
            _voice = await voiceFactory.CreateAsync(new VoiceSessionConfig(_instructions, tools.FacilitatorDefinitions, 600, _session.TeamId, _sessionId), _run);
            await _bridge.SendAsync(new { type = "info", code = "VOICE_RESTORED", message = "Voice Live reconnected" }, _run);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Voice Live reconnect failed");
            return false;
        }
    }

    private async Task TickAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try
        {
            while (await timer.WaitForNextTickAsync(_run))
            {
                _inbox.Writer.TryWrite(new Clock());
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    // ---- inbox ---------------------------------------------------------------------------------------------------

    private async Task HandleAsync(Msg msg)
    {
        using var hop = msg is FromVoice { Event: UserTranscript } || msg is FromBridge bridgeMessage && Str(bridgeMessage.Json, "type") == "utterance"
            ? ScrumTelemetry.StartHop("voice.transcript", _session.TeamId, _sessionId, _session.CorrelationId)
            : null;
        switch (msg)
        {
            case FromBridge b:
                await OnBridgeAsync(b.Json);
                break;
            case FromVoice v:
                await OnVoiceAsync(v.Event);
                break;
            case FirstAudio:
                await OnFirstAudioAsync();
                break;
            case Clock:
                await OnClockAsync();
                break;
            case ReconcileDone r:
                await OnReconcileDoneAsync(r);
                break;
            case FromControl c:
                await OnCommandAsync(c.Action, null);
                break;
            case BridgeClosed:
                _finished = true;
                break;
        }
    }

    private async Task OnBridgeAsync(JsonElement json)
    {
        switch (Str(json, "type"))
        {
            case "participants":
                await OnParticipantsAsync(json);
                break;
            case "localSpeaker":
                _presenter = Str(json, "memberId") ?? _presenter;
                break;
            case "speaking":
                var talker = Str(json, "memberId") ?? string.Empty;
                if (Bool(json, "active"))
                {
                    _lastActiveId = talker;
                }

                _lastActiveAt = clock.UtcNow;
                await ExecuteAsync(_floor.Handle(new SpeechActive(Attribute(talker), Bool(json, "active"), clock.UtcNow)));
                break;
            case "utterance":
                await OnUtteranceAsync(Str(json, "memberId") ?? string.Empty, Str(json, "text") ?? string.Empty, Int(json, "durationMs"), fromCast: true);
                break;
            case "agentAudioState" when Str(json, "state") == "idle":
                _audioIdle = true;
                await MaybeFinishSpeechAsync();
                break;
            case "callState":
                ScrumTelemetry.Count("join", _session.TeamId, Str(json, "state") == "Connected" ? "success" : "state_change");
                await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Bridge, null, $"{Str(json, "state")}: {Str(json, "message")}"), _run);
                break;
            case "telemetry":
                if (Str(json, "hop") is "bridge.audio" or "bridge.playback" && Int(json, "durationMs") is >= 0 and <= 60000)
                {
                    var name = Str(json, "hop")!;
                    using var bridgeHop = ScrumTelemetry.StartHop(name, _session.TeamId, _sessionId, _session.CorrelationId);
                    ScrumTelemetry.RecordHop(name, _session.TeamId, Int(json, "durationMs"));
                }
                break;
            case "command":
                await OnCommandAsync(Str(json, "action") ?? string.Empty, Str(json, "memberId"));
                break;
        }
    }

    private async Task OnParticipantsAsync(JsonElement json)
    {
        if (!json.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in items.EnumerateArray())
        {
            var id = Str(item, "memberId");
            if (id is null || item.TryGetProperty("present", out var p) && p.ValueKind != JsonValueKind.False)
            {
                continue;
            }

            if (_absent.Add(id))
            {
                await sender.Send(new MarkAbsentCommand(_sessionId, id), _run);
            }
        }
    }

    private async Task OnCommandAsync(string action, string? memberId)
    {
        switch (action)
        {
            case "begin" when !_begun:
                _begun = true;
                _beganAt = clock.UtcNow;
                _presenter = memberId ?? _presenter;
                await ExecuteAsync(_floor.Handle(new StartFloor(clock.UtcNow)));
                break;
            case "takeover":
                await ExecuteAsync(_floor.Handle(new HumanOverride(true)));
                break;
            case "release":
                await ExecuteAsync(_floor.Handle(new HumanOverride(false)));
                break;
            case "mute":
                await ExecuteAsync(_floor.Handle(new PauseFloor(true)));
                break;
            case "unmute":
                await ExecuteAsync(_floor.Handle(new PauseFloor(false)));
                break;
            case "skip":
                await ExecuteAsync(_floor.Handle(new SkipSpeaker()));
                break;
            case "end":
                await ExecuteAsync([new EndSession()]);
                break;
        }

        await PushUiAsync(force: true);
    }

    private async Task OnVoiceAsync(VoiceEvent ev)
    {
        switch (ev)
        {
            case UserSpeechStarted:
                _speechStartedAt = clock.UtcNow;
                _speechSpeaker = ResolveSpeaker();
                _bargeSpeaker = _speechSpeaker;
                _bargeCheckAt = _floor.AgentHasFloor && _audioStarted ? clock.UtcNow.AddMilliseconds(options.Value.BargeInMs) : null;
                _bargeSustainedAt = _bargeCheckAt is null ? null : clock.UtcNow.AddMilliseconds(options.Value.SustainedBargeInMs);
                await ExecuteAsync(_floor.Handle(new SpeechActive(Attribute(_speechSpeaker), true, clock.UtcNow)));
                break;
            case UserSpeechStopped:
                _bargeCheckAt = null;
                _bargeSustainedAt = null;
                _lastHumanEnd = clock.UtcNow;
                _pendingTranscripts++;
                _lastStoppedAt = clock.UtcNow;
                await ExecuteAsync(_floor.Handle(new SpeechActive(Attribute(_speechSpeaker ?? _presenter), false, clock.UtcNow)));
                break;
            case UserTranscript t:
                _pendingTranscripts = Math.Max(0, _pendingTranscripts - 1);
                _turnTranscriptMs = (int)Math.Max(0, (clock.UtcNow - _lastStoppedAt).TotalMilliseconds);
                if (!string.IsNullOrWhiteSpace(t.Text))
                {
                    var started = _speechStartedAt ?? clock.UtcNow.AddSeconds(-2);
                    await OnUtteranceAsync(_speechSpeaker ?? ResolveSpeaker(), t.Text.Trim(), (int)(clock.UtcNow - started).TotalMilliseconds, fromCast: false);
                }

                break;
            case AgentTranscript a when !string.IsNullOrWhiteSpace(a.Text):
                await _bridge.SendAsync(new { type = "caption", role = "agent", memberId = (string?)null, text = a.Text }, _run);
                await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Transcript, null, a.Text), _run);
                break;
            case ToolCallRequested call:
                await OnToolCallAsync(call);
                break;
            case ResponseDone d:
                await OnResponseDoneAsync(d);
                break;
            case VoiceFault f when _truncatedAt is { } at && clock.UtcNow - at < TimeSpan.FromSeconds(3):
                // A refused truncate only costs the model some context about what was heard; it is not a failure the room needs to see.
                logger.LogWarning("Voice Live refused a truncate or cancel after a barge-in: {Message}", f.Message);
                break;
            case VoiceFault f:
                await _bridge.SendAsync(new { type = "error", code = "VOICE_FAULT", message = f.Message }, _run);
                break;
        }
    }

    // Someone talked over the agent. FloorControl decides whether they may cut in; this stops the voice at once, tells the model how much
    // was heard, and remembers what was cut off so the next turn can acknowledge it instead of carrying on as if nothing happened.
    private async Task BargeInAsync(BargeIn barge)
    {
        var actions = _floor.Handle(barge);
        if (actions.OfType<CancelResponse>().Any() && _lastSpeak is { } last)
        {
            _interrupted = (barge.MemberId, Saying(last.Speak, last.Question));
            logger.LogInformation(
                "Barge-in session={SessionId} by={MemberId} holder={HolderId} sustained={Sustained}",
                _sessionId,
                barge.MemberId,
                _floor.CurrentMemberId,
                barge.Sustained);
            ScrumTelemetry.Count("barge_in", _session.TeamId, "cancelled");
            await RecordTalkOverAsync(barge.MemberId);
        }

        await ExecuteAsync(actions);
    }

    private async Task StopSpeakingAsync()
    {
        // Set first: audio already in flight must not reach the room after the flush.
        Interlocked.Exchange(ref _discardAgentAudio, 1);
        await _voice.CancelResponseAsync(_run);
        await _bridge.SendAsync(new { type = "control", action = "flush" }, _run);
        if (_agentItemId is { } item && _firstAudioAt is { } started)
        {
            _agentItemId = null;
            _truncatedAt = clock.UtcNow;
            await _voice.TruncateAsync(item, (int)(clock.UtcNow - started).TotalMilliseconds, _run);
        }
    }

    private string Saying(AgentSpeak speak, string? question) => speak.Reason switch
    {
        SpeakReason.Clarify or SpeakReason.Unblocker when question is not null => $"asking: {question}",
        SpeakReason.CallOn or SpeakReason.CircleBack => $"calling on {Short(NameOf(speak.MemberId ?? string.Empty))}",
        SpeakReason.Recap => "giving the recap",
        SpeakReason.StepAway or SpeakReason.NoResponse => "moving on from a member who was away",
        _ => "speaking",
    };

    // Consumed by the next turn the agent speaks, so it acknowledges the interruption once and does not repeat what it was saying.
    private string? WithInterruption(string? notes)
    {
        if (_interrupted is not { } i)
        {
            return notes;
        }

        _interrupted = null;
        var note = $"You were just interrupted by {Short(NameOf(i.ByMemberId))} while you were {i.Saying}, and stopped. Do not repeat or finish what you were saying. If {Short(NameOf(i.ByMemberId))} asked or said something to you, answer or acknowledge it first, in a few words; if they simply carried on with their update, just continue.";
        return string.IsNullOrEmpty(notes) ? note : $"{note} {notes}";
    }

    private async Task OnToolCallAsync(ToolCallRequested call)
    {
        var output = await tools.InvokeAsync(_sessionId, call.Name, call.ArgumentsJson, _run);
        await _voice.SubmitToolOutputAsync(call.CallId, output, _run);
        await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.ToolCall, null, $"{call.Name} {call.ArgumentsJson}"), _run);
    }

    private async Task OnResponseDoneAsync(ResponseDone done)
    {
        if (done.HadToolCalls)
        {
            await _voice.CreateResponseAsync(null, _run);
            return;
        }

        _responseDone = true;
        await MaybeFinishSpeechAsync();
    }

    private async Task OnFirstAudioAsync()
    {
        _audioStarted = true;
        _firstAudioAt = clock.UtcNow;
        await _bridge.SendAsync(new { type = "agentAudioState", state = "speaking" }, _run);
        if (_latencyBaseline is not { } t)
        {
            return;
        }

        var ms = (int)(clock.UtcNow - t).TotalMilliseconds;
        _latencyBaseline = null;
        _latencies.Add(ms);
        await sender.Send(new RecordAgentLatencyCommand(_sessionId, ms), _run);
        logger.LogInformation(
            "Agent turn session={SessionId} member={MemberId} state={State} latencyMs={LatencyMs} decisionMs={DecisionMs} voiceMs={VoiceMs}",
            _sessionId,
            _floor.CurrentMemberId,
            _floor.State,
            ms,
            (int)(_createSentAt - t).TotalMilliseconds,
            (int)(clock.UtcNow - _createSentAt).TotalMilliseconds);
        await RecordTimingAsync(ms, (int)(_createSentAt - t).TotalMilliseconds, (int)(clock.UtcNow - _createSentAt).TotalMilliseconds);
    }

    // One diagnostic event per reactive turn so the gate runner can split the delay by hop. The orchestration hop is what is left of the time to
    // CreateResponse after the transcript and the analysis: floor control, the wait for silence, and the reconcile.
    private async Task RecordTimingAsync(int totalMs, int toCreateMs, int voiceMs)
    {
        var orchestrationMs = Math.Max(0, toCreateMs - _turnTranscriptMs - _turnAnalysisMs);
        using var turnHop = ScrumTelemetry.StartHop("voice.turn", _session.TeamId, _sessionId, _session.CorrelationId);
        ScrumTelemetry.Count("turn", _session.TeamId, "first_audio");
        ScrumTelemetry.RecordHop("voice.total", _session.TeamId, totalMs);
        ScrumTelemetry.RecordHop("voice.transcription", _session.TeamId, _turnTranscriptMs);
        ScrumTelemetry.RecordHop("decision.grounding", _session.TeamId, Math.Max(0, _turnAnalysisMs - _turnModelMs));
        ScrumTelemetry.RecordHop("voice.orchestration", _session.TeamId, orchestrationMs);
        ScrumTelemetry.RecordHop("voice.first_audio", _session.TeamId, voiceMs);
        var json = JsonSerializer.Serialize(new
        {
            traceId = turnHop?.TraceId.ToHexString() ?? _session.CorrelationId,
            spanId = turnHop?.SpanId.ToHexString(),
            reason = _lastSpeak?.Speak.Reason.ToString() ?? "unknown",
            totalMs,
            speechEndToTranscriptMs = _turnTranscriptMs,
            analyserMs = Math.Max(0, _turnAnalysisMs - _turnModelMs),
            decisionModelMs = _turnModelMs,
            orchestrationMs,
            voiceFirstAudioMs = voiceMs,
            source = _turnSource,
        });
        await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Timing, _floor.CurrentMemberId, json), _run);
    }

    // One event each time the agent is cut off by someone speaking. In a scripted run nobody talks over the agent on purpose, so each one
    // is the agent speaking over a person: the voice service ended their turn too early (ADR 0019). The gate runner counts them.
    private async Task RecordTalkOverAsync(string? memberId)
    {
        var json = JsonSerializer.Serialize(new { reason = "TalkOver", totalMs = 0, speechEndToTranscriptMs = 0, analyserMs = 0, decisionModelMs = 0, orchestrationMs = 0, voiceFirstAudioMs = 0, source = "none" });
        await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Timing, memberId, json), _run);
    }

    private async Task MaybeFinishSpeechAsync()
    {
        if (!_responseDone || !_floor.AgentHasFloor || (_audioStarted && !_audioIdle))
        {
            return;
        }

        _responseDone = false;
        await ExecuteAsync(_floor.Handle(new AgentSpeechEnded(clock.UtcNow)));
    }

    private async Task OnUtteranceAsync(string memberId, string text, int durationMs, bool fromCast)
    {
        if (string.IsNullOrWhiteSpace(memberId) || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var now = clock.UtcNow;
        if (fromCast)
        {
            _lastHumanEnd = now;
            await _voice.AddUserTextAsync($"{NameOf(memberId)}: {text}", _run);
        }

        await _bridge.SendAsync(new { type = "caption", role = "participant", memberId, text }, _run);

        // Someone speaking for a teammate counts as that teammate for the floor and the record.
        var owner = Attribute(memberId);
        await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Transcript, owner, owner == memberId ? text : $"({NameOf(memberId)} for {NameOf(owner)}) {text}"), _run);

        var analysis = await AnalyseAsync(memberId, owner, text);
        var interpretation = owner != memberId
            ? new Interpretation(ItemMentionsCorrection(text) ? UtteranceIntent.Correction : UtteranceIntent.Update, owner)
            : analysis.Interpretation;
        if (owner == memberId && memberId == _floor.CurrentMemberId && _awaitingUnblocker.Remove(memberId))
        {
            // M9-R2: the answer to "Who can help unblock this?" is matched to a roster member in code, never by the model.
            await sender.Send(new ResolveUnblockerCommand(_sessionId, memberId, text), _run);
        }

        if (await ReactAsync(interpretation, memberId, text))
        {
            return;
        }

        if (owner == _floor.CurrentMemberId)
        {
            Remember(owner, text, analysis);
        }
        else
        {
            _offTopic.Add(text);
            _offTopicSpeakers.Add(NameOf(memberId));
        }

        var actions = _floor.Handle(new MemberSpoke(owner, Math.Max(durationMs, 500), now));
        if (text.Contains("scrum agent", StringComparison.OrdinalIgnoreCase) || interpretation.Intent == UtteranceIntent.AskItemInfo)
        {
            actions = [.. actions, .. RespondActions(null)];
        }

        await ExecuteAsync(actions);
    }

    // The decision model reads the utterance (intent, then items, then comment kind) and code checks it; any failure falls back to the rules.
    private async Task<UtteranceAnalysis> AnalyseAsync(string speakerId, string owner, string text)
    {
        var started = clock.UtcNow;
        int? focus;
        string? asked;
        lock (_gate)
        {
            focus = _focus.TryGetValue(owner, out var f) ? f : null;
            asked = _asked.GetValueOrDefault(owner);
        }

        var result = await sender.Send(new AnalyseUtteranceQuery(_sessionId, speakerId, _floor.CurrentMemberId, text, _roster, asked, focus), _run);
        var analysis = result.IsSuccess
            ? result.Value!
            : new UtteranceAnalysis(UtteranceClassifier.Classify(text, speakerId, _floor.CurrentMemberId, _roster), [], [], AnalysisSource.Rules);
        _turnAnalysisMs = (int)(clock.UtcNow - started).TotalMilliseconds;
        _turnModelMs = analysis.ModelMs;
        _turnSource = analysis.Source.ToString();
        logger.LogInformation(
            "Utterance session={SessionId} member={MemberId} intent={Intent} source={Source} items={Items} comments={Comments} analysisMs={AnalysisMs}",
            _sessionId,
            speakerId,
            analysis.Interpretation.Intent,
            analysis.Source,
            string.Join(',', analysis.WorkItemIds),
            string.Join(',', analysis.Comments.Select(c => c.Kind)),
            (int)(clock.UtcNow - started).TotalMilliseconds);
        return analysis;
    }

    private static bool ItemMentionsCorrection(string text) =>
        UtteranceClassifier.Classify(text, "x", "x", []).Intent == UtteranceIntent.Correction;

    // ---- goal-driven reactions -----------------------------------------------------------------------------------
    // The standup has one goal: every member's items covered. Whatever happens in the room (a chime-in, someone stepping away,
    // a proxy, a correction) is handled by changing who is asked next or what the agent says, never by losing the goal.

    // Returns true when the utterance was fully handled and must not be treated as part of the holder's update.
    private async Task<bool> ReactAsync(Interpretation i, string speakerId, string text)
    {
        var holder = _floor.CurrentMemberId;
        switch (i.Intent)
        {
            case UtteranceIntent.ProxyUpdate when i.TargetMemberId is { } target && i.ByMemberId is { } by:
                _proxy[target] = by;
                _status[target] = $"update by {Short(NameOf(by))}";
                await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Proxy, target, $"{NameOf(by)} gives {NameOf(target)}'s update"), _run);
                await RespondAsync(target == holder
                    ? $"{Short(NameOf(by))} will give {Short(NameOf(target))}'s update. Acknowledge in one short sentence, say that anything captured still needs {Short(NameOf(target))} to approve it, and invite {Short(NameOf(by))} to go ahead. Do not move on."
                    : $"{Short(NameOf(by))} will give {Short(NameOf(target))}'s update when it is {Short(NameOf(target))}'s turn. Acknowledge in one short sentence and say that anything captured still needs {Short(NameOf(target))} to approve it. Then carry on with {Short(NameOf(holder ?? string.Empty))}.");
                return true;
            case UtteranceIntent.StepAway when i.TargetMemberId is { } away:
                return await OnStepAwayAsync(away, holder, speakerId);
            case UtteranceIntent.AskWhatToReport when MemberOf(i.TargetMemberId ?? speakerId) is { } brief:
                lock (_gate)
                {
                    _directive = _session.Kind == CeremonyKind.Retrospective
                        ? $"{Short(brief.DisplayName)} asked what to share. Tell them, briefly: {RetroFormats.Prompt(retro.Format)}."
                        : $"{Short(brief.DisplayName)} asked what to report on. Tell them, briefly and in this order: {ReportingPriorities.For(brief, _covered)}";
                }

                await RespondAsync(_directive);
                return true;
            case UtteranceIntent.OutOfTurnUpdate when holder is not null && _redirected.Add($"{speakerId}>{holder}"):
                await RespondAsync($"{Short(NameOf(speakerId))} started giving an update, but it is {Short(NameOf(holder))}'s turn. In one friendly sentence say it is {Short(NameOf(holder))}'s turn now, that {Short(NameOf(speakerId))} will be asked in turn, and if it is urgent they can say so.");
                return true;
            case UtteranceIntent.BackNow when _status.ContainsKey(speakerId) && holder != speakerId:
                _status.Remove(speakerId);
                await RespondAsync($"{Short(NameOf(speakerId))} is back. Welcome them in a few words and say you will get to them after {Short(NameOf(holder ?? string.Empty))}'s update.");
                return true;
            case UtteranceIntent.Correction:
                _correcting.Add(i.TargetMemberId ?? speakerId);
                return false;
            default:
                return false;
        }
    }

    private async Task<bool> OnStepAwayAsync(string away, string? holder, string speakerId)
    {
        var by = speakerId == away ? string.Empty : $" {Short(NameOf(speakerId))} said so.";
        if (away == holder)
        {
            _deferNotes = $"{Short(NameOf(away))} has stepped away.{by}";
            await ExecuteAsync(_floor.Handle(new DeferCurrent(DeferReason.StepAway)));
            return true;
        }

        if (MemberOf(away) is null || _absent.Contains(away))
        {
            return false;
        }

        // Not their turn yet: they are skipped in advance and circled back to at the end.
        await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Deferred, away, $"{NameOf(away)} stepped away; will circle back"), _run);
        _status[away] = "away";
        await RespondAsync($"{Short(NameOf(away))} is away for a few minutes.{by} Say in one sentence that you will skip them for now and come back to them at the end, then carry on with {Short(NameOf(holder ?? string.Empty))}.");
        return true;
    }

    private async Task RespondAsync(string directive)
    {
        lock (_gate)
        {
            _directive = directive;
        }

        var actions = RespondActions(directive);
        if (actions.Count == 0)
        {
            lock (_gate)
            {
                _directive = null;
            }
        }

        await ExecuteAsync(actions);
    }

    private IReadOnlyList<FloorAction> RespondActions(string? directive)
    {
        _ = directive;
        return _floor.Handle(new AgentAddressed());
    }

    private string Attribute(string memberId) =>
        _proxy.FirstOrDefault(p => p.Value == memberId && p.Key == _floor.CurrentMemberId).Key ?? memberId;

    // In Teams the mixed audio has no speaker, so the member whose "speaking" event is live (or was a moment ago) is the speaker.
    // Without one, the person whose turn it is is the best guess; the Simulated Room has an explicit presenter.
    private string ResolveSpeaker()
    {
        if (_lastActiveId is not null && clock.UtcNow - _lastActiveAt < TimeSpan.FromSeconds(2))
        {
            return _lastActiveId;
        }

        return _session.Mode == "acs" ? _floor.CurrentMemberId ?? _presenter : _presenter;
    }

    private MemberBrief? MemberOf(string memberId) => _session.PreBrief?.Members.FirstOrDefault(m => m.MemberId == memberId);

    private static string Short(string displayName) => Names.First(displayName);
    private void Remember(string memberId, string text, UtteranceAnalysis analysis)
    {
        lock (_gate)
        {
            var so = _analysis.TryGetValue(memberId, out var s) ? s : (Items: [], Comments: [], AllModel: true);
            so.Items.AddRange(analysis.WorkItemIds.Where(i => !so.Items.Contains(i)));
            so.Comments.AddRange(analysis.Comments);
            _analysis[memberId] = (so.Items, so.Comments, so.AllModel && analysis.Source == AnalysisSource.Model);
            if (!_utterance.TryGetValue(memberId, out var sb))
            {
                _utterance[memberId] = sb = new System.Text.StringBuilder();
            }

            sb.Append(sb.Length == 0 ? string.Empty : " ").Append(text);

            // Precompute (SPEC 10.2): reconcile while the member may still be talking so the result is ready when silence is detected.
            var snapshot = sb.ToString();
            var analysed = Snapshot(_analysis[memberId]);
            _speculative[memberId] = (snapshot, Task.Run(() => ReconcileAsync(memberId, snapshot, analysed), _run));
        }
    }

    private async Task OnClockAsync()
    {
        var now = clock.UtcNow;
        if (_begun)
        {
            if (_bargeCheckAt is { } at && now >= at)
            {
                _bargeCheckAt = null;
                await BargeInAsync(new BargeIn(_bargeSpeaker ?? _presenter, IsHuman: false));
            }

            if (_bargeSustainedAt is { } sustained && now >= sustained)
            {
                _bargeSustainedAt = null;
                await BargeInAsync(new BargeIn(_bargeSpeaker ?? _presenter, IsHuman: false, Sustained: true));
            }

            await ExecuteAsync(_floor.Handle(new Tick(now)));
        }

        await PushUiAsync(force: false);
    }

    private async Task OnReconcileDoneAsync(ReconcileDone done)
    {
        var hasQuestion = false;
        if (done.Result is { } r)
        {
            foreach (var e in r.Events)
            {
                await sender.Send(new RecordStandupEventCommand(_sessionId, e.Type, done.MemberId, e.Text, e.WorkItemIds, e.Category, e.OwnerMemberId), _run);
                lock (_gate)
                {
                    _covered.UnionWith(e.WorkItemIds);
                }
            }

            lock (_gate)
            {
                _covered.UnionWith(r.MentionedWorkItemIds);
            }

            var retracted = r.RetractedEventIds ?? [];
            foreach (var id in retracted)
            {
                await sender.Send(new RetractStandupEventCommand(_sessionId, id), _run);
            }

            _correcting.Remove(done.MemberId);
            if (r.Events.Any(e => e.Type == StandupEventType.Blocker) && _unblockerAsked.Add(done.MemberId))
            {
                _askUnblocker.Add(done.MemberId);
            }

            var corrected = retracted.Count > 0 ? "you corrected an earlier statement, so I have removed it; " : string.Empty;
            _noted[done.MemberId] = corrected + CoverageNotes.Noted(r.Events.Select(e => (e.Type, e.Text)));

            _pendingQuestion[done.MemberId] = r.SuggestedQuestion;
            _pendingItem[done.MemberId] = r.QuestionItemId;
            hasQuestion = !string.IsNullOrWhiteSpace(r.SuggestedQuestion) || _noted[done.MemberId].Length > 0 || _askUnblocker.Contains(done.MemberId);
        }

        if (!done.Background)
        {
            await ExecuteAsync(_floor.Handle(new ReconcileFinished(done.Version, hasQuestion)));
        }
    }

    // ---- actions -------------------------------------------------------------------------------------------------

    private async Task ExecuteAsync(IReadOnlyList<FloorAction> actions)
    {
        foreach (var action in actions)
        {
            switch (action)
            {
                case AgentSpeak speak:
                    await SpeakAsync(speak);
                    break;
                case CancelResponse:
                    await StopSpeakingAsync();
                    break;
                case RunReconcile r:
                    StartReconcile(r);
                    break;
                case AdvanceSpeaker:
                    await AdvanceAsync();
                    break;
                case DeferSpeaker d:
                    await DeferAsync(d);
                    break;
                case ParkTopic:
                    await ParkAsync();
                    break;
                case EndSession:
                    await EndAsync();
                    break;
            }
        }

        await PushUiAsync(force: true);
    }

    private async Task SpeakAsync(AgentSpeak speak)
    {
        var reason = speak.Reason;
        var question = reason == SpeakReason.Clarify && speak.MemberId is not null ? _pendingQuestion.GetValueOrDefault(speak.MemberId) : null;
        if (reason == SpeakReason.Clarify && speak.MemberId is not null && _askUnblocker.Remove(speak.MemberId))
        {
            // M9-R2: a recorded blocker gets one turn that asks who can help, before any other clarifying question.
            reason = SpeakReason.Unblocker;
            question = UnblockerQuestion;
            _awaitingUnblocker.Add(speak.MemberId);
        }

        var notes = WithInterruption(NotesFor(speak));
        var instruction = await sender.Send(new GetTurnInstructionQuery(_sessionId, reason, speak.MemberId, question, notes), _run);
        if (!instruction.IsSuccess)
        {
            await _bridge.SendAsync(new { type = "error", code = "TURN_INSTRUCTION", message = instruction.Error!.Message }, _run);
            await ExecuteAsync(_floor.Handle(new AgentSpeechEnded(clock.UtcNow)));
            return;
        }

        _responseDone = false;
        _audioIdle = false;
        _audioStarted = false;
        _agentItemId = null;
        _firstAudioAt = null;
        _lastSpeak = (speak, question);

        // Only a turn that reacts to someone finishing speaking has a latency; timebox and time-warning turns do not.
        _latencyBaseline = _lastHumanEnd is { } end && clock.UtcNow - end < TimeSpan.FromSeconds(10) ? end : null;
        _lastHumanEnd = null;
        Interlocked.Exchange(ref _firstAudioPending, 1);
        if (question is not null)
        {
            await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Question, speak.MemberId, question), _run);
            _asked[speak.MemberId!] = question;
        }

        if (speak.MemberId is not null && reason is SpeakReason.Clarify or SpeakReason.Unblocker)
        {
            // Only an answer to a question about a specific item is credited to that item. "Who can help?" and read-backs are not, or the
            // next uncovered item would be marked covered although nobody mentioned it.
            lock (_gate)
            {
                if (reason == SpeakReason.Clarify && question is not null && _pendingItem.GetValueOrDefault(speak.MemberId) is { } item)
                {
                    _focus[speak.MemberId] = item;
                }
                else
                {
                    _focus.Remove(speak.MemberId);
                }
            }
        }

        await _voice.CreateResponseAsync(instruction.Value, _run);
        _createSentAt = clock.UtcNow;
    }

    // A retrospective is sorted by rules in code (RetroInterpreter); the standup also asks the model for phrasing.
    // Comments are only authoritative when the model read every segment of the update; one segment read by rules means the reconcile model proposes the events.
    private static (IReadOnlyList<int> Items, IReadOnlyList<ItemComment>? Comments) Snapshot((List<int> Items, List<ItemComment> Comments, bool AllModel) a) =>
        ([.. a.Items], a.AllModel ? [.. a.Comments] : null);

    private Task<Result<ReconcileResult>> ReconcileAsync(string memberId, string text, (IReadOnlyList<int> Items, IReadOnlyList<ItemComment>? Comments) analysed)
    {
        lock (_gate)
        {
            if (_session.Kind == CeremonyKind.Retrospective)
            {
                return sender.Send(new ReconcileRetroUpdateQuery(_sessionId, memberId, text, _asked.ContainsKey(memberId), _correcting.Contains(memberId)), _run);
            }

            return sender.Send(new ReconcileUpdateQuery(_sessionId, memberId, text, _asked.GetValueOrDefault(memberId), [.. _covered], _focus.TryGetValue(memberId, out var f) ? f : null, _correcting.Contains(memberId), analysed.Items, analysed.Comments), _run);
        }
    }

    // Context for "is that one overdue?": the item being discussed and the speaker's other open items, so "that one" resolves in code, not by guess.
    private string ItemContext(string memberId)
    {
        var items = _session.PreBrief?.Members.FirstOrDefault(m => m.MemberId == memberId)?.Items ?? [];
        int? focus;
        lock (_gate)
        {
            focus = _focus.TryGetValue(memberId, out var f) ? f : null;
        }

        var current = items.FirstOrDefault(i => i.Id.Value == focus);
        var head = current is null ? "No single item is being discussed." : $"The item being discussed now is {CoverageNotes.Label(current)}, \"{current.Title}\".";
        var list = string.Join("; ", items.Select(i => $"{CoverageNotes.Label(i)} \"{i.Title}\" ({i.State}{(i.Flags.Count > 0 ? ", " + string.Join("/", i.Flags) : string.Empty)})"));
        return $"{head} {NameOf(memberId)}'s open items: {list}.";
    }

    private const string UnblockerQuestion = "Who can help unblock this?";

    private string? NotesFor(AgentSpeak speak)
    {
        if (_session.Kind == CeremonyKind.Retrospective && speak.Reason is not (SpeakReason.StepAway or SpeakReason.NoResponse or SpeakReason.Addressed))
        {
            // The retrospective's turn templates carry their own facts (themes, actions); there are no flagged items to carry over.
            return null;
        }

        if (speak.Reason == SpeakReason.Clarify && speak.MemberId is not null)
        {
            return _noted.GetValueOrDefault(speak.MemberId);
        }

        if (speak.Reason is SpeakReason.StepAway or SpeakReason.NoResponse)
        {
            return _deferNotes;
        }

        if (speak.Reason is SpeakReason.CallOn or SpeakReason.CircleBack && MemberOf(speak.MemberId ?? string.Empty) is { } caller)
        {
            // M9-R2, M9-R3: ask first about last time's commitments, and whether they helped the teammate they were named to help.
            var carried = CoverageNotes.CarryOverNotes(caller);
            return carried.Length == 0 ? null : carried;
        }

        if (speak.Reason == SpeakReason.Addressed && speak.MemberId is not null)
        {
            lock (_gate)
            {
                var directive = _directive;
                _directive = null;
                return directive ?? ItemContext(speak.MemberId);
            }
        }

        if (speak.Reason != SpeakReason.Recap || _session.PreBrief is not { } brief)
        {
            return null;
        }

        lock (_gate)
        {
            return CoverageNotes.Describe(CoverageNotes.UncoveredFlagged(brief, _covered));
        }
    }

    private void StartReconcile(RunReconcile r)
    {
        var task = Task.Run(() => ReconcileWhenTranscriptsLandAsync(r.MemberId), _run);
        _ = task.ContinueWith(
            t => _inbox.Writer.TryWrite(new ReconcileDone(r.MemberId, r.Version, t.IsCompletedSuccessfully && t.Result.IsSuccess ? t.Result.Value : null, r.Background)),
            TaskScheduler.Default);
    }

    // Speech-to-text finishes a moment after the speaker stops, so wait (briefly) for outstanding transcripts before reconciling.
    private async Task<Result<ReconcileResult>> ReconcileWhenTranscriptsLandAsync(string memberId)
    {
        while (_pendingTranscripts > 0 && clock.UtcNow - _lastStoppedAt < TimeSpan.FromMilliseconds(2500))
        {
            await Task.Delay(50, _run);
        }

        string text;
        Task<Result<ReconcileResult>>? reused = null;
        (IReadOnlyList<int> Items, IReadOnlyList<ItemComment>? Comments) analysed;
        lock (_gate)
        {
            text = _utterance.TryGetValue(memberId, out var sb) ? sb.ToString() : string.Empty;
            analysed = _analysis.TryGetValue(memberId, out var a) ? Snapshot(a) : ([], null);
            _analysis.Remove(memberId);
            if (_speculative.TryGetValue(memberId, out var s) && s.Text == text)
            {
                reused = s.Task;
            }

            _utterance.Remove(memberId);
            _speculative.Remove(memberId);
        }

        return await (reused ?? ReconcileAsync(memberId, text, analysed));
    }

    private async Task AdvanceAsync()
    {
        _interrupted = null;
        if (_floor.CurrentMemberId is { } previous)
        {
            lock (_gate)
            {
                _focus.Remove(previous);
                _asked.Remove(previous);
            }

            _proxy.Remove(previous);
            _status.Remove(previous);
            if (_noResponses.GetValueOrDefault(previous) >= 2 && !_deferredNow.Contains(previous))
            {
                await sender.Send(new CompleteSpeakerTurnCommand(_sessionId, previous), _run);
                await AdvanceToNextAsync();
                return;
            }

            if (_deferredNow.Remove(previous))
            {
                // Skipped for now, not finished: do not complete the turn.
                await AdvanceToNextAsync();
                return;
            }

            await sender.Send(new CompleteSpeakerTurnCommand(_sessionId, previous), _run);
        }

        await AdvanceToNextAsync();
    }

    private async Task AdvanceToNextAsync()
    {
        var next = await sender.Send(new GetNextSpeakerQuery(_sessionId), _run);
        if (next.IsSuccess && next.Value.Announcement is { } announce)
        {
            // The retrospective's themes are named in code-grouped order, then read back to everyone.
            if (announce == SpeakReason.RetroThemes)
            {
                await sender.Send(new NameRetroThemesCommand(_sessionId), _run);
            }

            await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Phase, null, next.Value.Phase), _run);
            await ExecuteAsync(_floor.Handle(new AnnounceStep(announce)));
            return;
        }

        if (next.IsSuccess && next.Value.Phase == "Actions" && next.Value.MemberId is { } asked)
        {
            await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Phase, asked, RetroAgenda.ActionTurnPrefix + asked), _run);
        }

        if (next.IsSuccess && next.Value.CircleBack && next.Value.MemberId is { } back)
        {
            _status.Remove(back);
            await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.CircledBack, back, $"Circling back to {NameOf(back)}"), _run);
        }

        var more = _floor.Handle(next.IsSuccess
            ? new SpeakerSelected(next.Value.MemberId, next.Value.Absent, true, next.Value.CircleBack)
            : new SpeakerSelected(null, false));
        await ExecuteAsync(more);
    }

    // A deferred member is not "handled": NextSpeaker skips them now and returns them last. Someone who stays silent twice is
    // finished (recorded as present but silent, which is different from absent) so the standup cannot loop.
    private async Task DeferAsync(DeferSpeaker d)
    {
        var name = Short(NameOf(d.MemberId));
        if (d.Reason == DeferReason.NoResponse)
        {
            var count = _noResponses[d.MemberId] = _noResponses.GetValueOrDefault(d.MemberId) + 1;
            await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.NoResponse, d.MemberId, $"{NameOf(d.MemberId)} was in the meeting but did not respond"), _run);
            if (count >= 2)
            {
                _status[d.MemberId] = "no response";
                _deferNotes = $"{name} still did not respond when you came back. Say in one sentence that you could not get an update from {name}, that they were here, and that a follow-up will be sent.";
                return;
            }

            _deferNotes = $"{name} did not answer within the wait time.";
        }

        _status[d.MemberId] = d.Reason == DeferReason.NoResponse ? "no response yet" : "away";
        _deferredNow.Add(d.MemberId);
        await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Deferred, d.MemberId, d.Reason == DeferReason.NoResponse ? $"{NameOf(d.MemberId)} did not respond; will circle back" : $"{NameOf(d.MemberId)} stepped away; will circle back"), _run);
    }

    private async Task ParkAsync()
    {
        var who = _offTopicSpeakers.Count == 0 ? string.Empty : $" ({string.Join(" and ", _offTopicSpeakers)})";
        var gist = _offTopic.Count == 0 ? string.Empty : ": " + Gist(_offTopic[0]);
        var topic = "Side discussion" + who + gist;
        _offTopic.Clear();
        _offTopicSpeakers.Clear();
        _parking.Add(topic);
        await sender.Send(new ParkTopicCommand(_sessionId, topic), _run);
    }

    private static string Gist(string text)
    {
        const int max = 70;
        if (text.Length <= max)
        {
            return text;
        }

        var cut = text.LastIndexOf(' ', max);
        return text[..(cut > 20 ? cut : max)] + "...";
    }

    private async Task EndAsync()
    {
        await sender.Send(new EndStandupCommand(_sessionId), _run);
        await PushUiAsync(force: true, ended: true);
        await _bridge.SendAsync(new { type = "control", action = "leave" }, _run);
        _finished = true;
    }

    // ---- helpers -------------------------------------------------------------------------------------------------

    private async Task PushUiAsync(bool force, bool ended = false)
    {
        var now = clock.UtcNow;
        if (!force && now - _lastUi < TimeSpan.FromSeconds(1))
        {
            return;
        }

        _lastUi = now;
        var total = _session.StandupMinutes * 60;
        var remaining = _begun ? Math.Max(0, total - (int)(now - _beganAt).TotalSeconds) : total;
        var state = new
        {
            floor = ended ? "Closed" : _floor.State.ToString(),
            currentSpeaker = _floor.CurrentMemberId,
            agentSpeaking = _floor.AgentHasFloor,
            remainingSec = remaining,
            parkingLot = _parking,
            absent = _absent,
            p50 = LatencyStats.Percentile(_latencies, 50),
            p95 = LatencyStats.Percentile(_latencies, 95),
            running = _begun,
        };
        live.Update(_sessionId, JsonSerializer.Serialize(state));
        await _bridge.SendAsync(new { type = "ui", state }, _run);
        await PushBoardAsync(ended);
    }

    // The Teams page renders this as a shared screen so everyone sees the current speaker's items and what is still uncovered.
    private async Task PushBoardAsync(bool ended)
    {
        if (_session.PreBrief is not { } brief)
        {
            return;
        }

        if (_session.Kind == CeremonyKind.Retrospective)
        {
            await PushRetroBoardAsync(brief, ended);
            return;
        }

        HashSet<int> covered;
        lock (_gate)
        {
            covered = [.. _covered];
        }

        var speaker = _floor.CurrentMemberId is { } id ? brief.Members.FirstOrDefault(m => m.MemberId == id) : null;
        var board = new
        {
            type = "board",
            ended,
            sprint = brief.IterationPath,
            goal = brief.SprintGoal,
            daysLeft = brief.DaysLeft,
            speaker = speaker?.DisplayName,
            focus = speaker is not null && _focus.TryGetValue(speaker.MemberId, out var fo) ? fo : (int?)null,
            items = (speaker?.Items ?? brief.Members.SelectMany(m => m.Items).Where(i => i.Flags.Count > 0).ToList()).Select(i => new
            {
                id = i.Id.Value,
                owner = brief.Members.FirstOrDefault(m => m.Items.Any(x => x.Id == i.Id))?.DisplayName,
                title = i.Title,
                type = i.Type,
                state = i.State,
                flags = i.Flags.Select(f => f.ToString()),
                covered = covered.Contains(i.Id.Value),
            }),
            team = brief.Members.Select(m => new
            {
                name = m.DisplayName,
                total = m.Items.Count,
                covered = m.Items.Count(i => covered.Contains(i.Id.Value)),
                status = _status.GetValueOrDefault(m.MemberId),
            }),
        };
        var json = JsonSerializer.Serialize(board);
        if (json == _lastBoard)
        {
            return;
        }

        _lastBoard = json;
        await _bridge.SendAsync(board, _run);
    }

    // The retrospective board: three columns that fill as members speak, then the agreed actions.
    private async Task PushRetroBoardAsync(PreBrief brief, bool ended)
    {
        var read = await sender.Send(new GetRetroBoardQuery(_sessionId), _run);
        if (!read.IsSuccess)
        {
            return;
        }

        var b = read.Value;
        var board = new
        {
            type = "board",
            kind = "retro",
            ended,
            sprint = brief.IterationPath,
            phase = b.Phase,
            speaker = _floor.CurrentMemberId is { } id ? Short(NameOf(id)) : null,
            columns = b.Columns.Select(c => new { key = c.Category.ToString(), label = c.Label, notes = c.Notes.Select(n => new { text = n.Text, member = n.Member }) }),
            themes = b.Themes.Select(t => new { name = t.Name, count = t.Count }),
            actions = b.Actions.Select(a => new { text = a.Text, owner = Short(a.OwnerName) }),
            team = brief.Members.Select(m => new { name = m.DisplayName, status = _status.GetValueOrDefault(m.MemberId) }),
        };
        var json = JsonSerializer.Serialize(board);
        if (json == _lastBoard)
        {
            return;
        }

        _lastBoard = json;
        await _bridge.SendAsync(board, _run);
    }

    private string NameOf(string memberId) =>
        _session.PreBrief?.Members.FirstOrDefault(m => m.MemberId == memberId)?.DisplayName ?? memberId;

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Bool(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : 0;
}
