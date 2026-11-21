using System.Text.Json;
using System.Threading.Channels;
using Mcm.Scrum.Api.Endpoints;
using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Standups;
using Mcm.Scrum.Application.Features.Standups.CompleteSpeakerTurn;
using Mcm.Scrum.Application.Features.Standups.EndStandup;
using Mcm.Scrum.Application.Features.Standups.GetFacilitatorInstructions;
using Mcm.Scrum.Application.Features.Standups.GetNextSpeaker;
using Mcm.Scrum.Application.Features.Standups.GetSession;
using Mcm.Scrum.Application.Features.Standups.GetTurnInstruction;
using Mcm.Scrum.Application.Features.Standups.MarkAbsent;
using Mcm.Scrum.Application.Features.Standups.ParkTopic;
using Mcm.Scrum.Application.Features.Standups.RecordAgentLatency;
using Mcm.Scrum.Application.Features.Standups.RecordStandupEvent;
using Mcm.Scrum.Application.Features.Standups.ReconcileUpdate;
using Mcm.Scrum.Application.Tools;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Voice;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Api.Voice;

/// <summary>
/// The voice gateway (SPEC 11): one Voice Live session per standup, wired to a bridge websocket.
/// It owns no decisions. <see cref="FloorControl"/> decides when the agent speaks, queries decide what is said and who is next,
/// and this class only moves events in and actions out. Audio frames bypass the mediator for latency (SPEC 6.1.1).
/// </summary>
public sealed class VoiceGateway(
    ISender sender,
    IScrumTools tools,
    IVoiceSessionFactory voiceFactory,
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
    private DateTimeOffset _lastUi;
    private int _pendingTranscripts;
    private DateTimeOffset _lastStoppedAt;
    private readonly HashSet<string> _offTopicSpeakers = [];
    private readonly HashSet<int> _covered = [];
    private string? _lastBoard;

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
            _voice = await voiceFactory.CreateAsync(new VoiceSessionConfig(instructions.Value, tools.FacilitatorDefinitions, 600), ct);
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
                sessionId = _sessionId,
                mode = _session.Mode,
                version = 1,
                standupMinutes = _session.StandupMinutes,
                members = _session.PreBrief.Members.Select(m => new { memberId = m.MemberId, displayName = m.DisplayName }),
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
                        await _bridge.SendAudioAsync(audio.Pcm, _run);
                        if (Interlocked.Exchange(ref _firstAudioPending, 0) == 1)
                        {
                            _inbox.Writer.TryWrite(new FirstAudio());
                        }

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
            _voice = await voiceFactory.CreateAsync(new VoiceSessionConfig(_instructions, tools.FacilitatorDefinitions, 600), _run);
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
                await ExecuteAsync(_floor.Handle(new SpeechActive(Str(json, "memberId") ?? string.Empty, Bool(json, "active"), clock.UtcNow)));
                break;
            case "utterance":
                await OnUtteranceAsync(Str(json, "memberId") ?? string.Empty, Str(json, "text") ?? string.Empty, Int(json, "durationMs"), fromCast: true);
                break;
            case "agentAudioState" when Str(json, "state") == "idle":
                _audioIdle = true;
                await MaybeFinishSpeechAsync();
                break;
            case "callState":
                await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Bridge, null, $"{Str(json, "state")}: {Str(json, "message")}"), _run);
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
                _bargeCheckAt = _floor.AgentHasFloor && _audioStarted ? clock.UtcNow.AddMilliseconds(500) : null;
                await ExecuteAsync(_floor.Handle(new SpeechActive(_presenter, true, clock.UtcNow)));
                break;
            case UserSpeechStopped:
                _bargeCheckAt = null;
                _lastHumanEnd = clock.UtcNow;
                _pendingTranscripts++;
                _lastStoppedAt = clock.UtcNow;
                await ExecuteAsync(_floor.Handle(new SpeechActive(_presenter, false, clock.UtcNow)));
                break;
            case UserTranscript t:
                _pendingTranscripts = Math.Max(0, _pendingTranscripts - 1);
                if (!string.IsNullOrWhiteSpace(t.Text))
                {
                    var started = _speechStartedAt ?? clock.UtcNow.AddSeconds(-2);
                    await OnUtteranceAsync(_presenter, t.Text.Trim(), (int)(clock.UtcNow - started).TotalMilliseconds, fromCast: false);
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
            case VoiceFault f:
                await _bridge.SendAsync(new { type = "error", code = "VOICE_FAULT", message = f.Message }, _run);
                break;
        }
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
        await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Transcript, memberId, text), _run);

        if (memberId == _floor.CurrentMemberId)
        {
            Remember(memberId, text);
        }
        else
        {
            _offTopic.Add(text);
            _offTopicSpeakers.Add(NameOf(memberId));
        }

        var actions = _floor.Handle(new MemberSpoke(memberId, Math.Max(durationMs, 500), now));
        if (text.Contains("scrum agent", StringComparison.OrdinalIgnoreCase))
        {
            actions = [.. actions, .. _floor.Handle(new AgentAddressed())];
        }

        await ExecuteAsync(actions);
    }

    private void Remember(string memberId, string text)
    {
        lock (_gate)
        {
            if (!_utterance.TryGetValue(memberId, out var sb))
            {
                _utterance[memberId] = sb = new System.Text.StringBuilder();
            }

            sb.Append(sb.Length == 0 ? string.Empty : " ").Append(text);

            // Precompute (SPEC 10.2): reconcile while the member may still be talking so the result is ready when silence is detected.
            var snapshot = sb.ToString();
            _speculative[memberId] = (snapshot, Task.Run(() => sender.Send(new ReconcileUpdateQuery(_sessionId, memberId, snapshot), _run), _run));
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
                await ExecuteAsync(_floor.Handle(new BargeIn(_presenter, IsHuman: false)));
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
                await sender.Send(new RecordStandupEventCommand(_sessionId, e.Type, done.MemberId, e.Text, e.WorkItemIds), _run);
                lock (_gate)
                {
                    _covered.UnionWith(e.WorkItemIds);
                }
            }

            _pendingQuestion[done.MemberId] = r.SuggestedQuestion;
            hasQuestion = !string.IsNullOrWhiteSpace(r.SuggestedQuestion);
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
                    await _voice.CancelResponseAsync(_run);
                    await _bridge.SendAsync(new { type = "control", action = "flush" }, _run);
                    break;
                case RunReconcile r:
                    StartReconcile(r);
                    break;
                case AdvanceSpeaker:
                    await AdvanceAsync();
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
        var question = speak.Reason == SpeakReason.Clarify && speak.MemberId is not null ? _pendingQuestion.GetValueOrDefault(speak.MemberId) : null;
        var instruction = await sender.Send(new GetTurnInstructionQuery(_sessionId, speak.Reason, speak.MemberId, question), _run);
        if (!instruction.IsSuccess)
        {
            await _bridge.SendAsync(new { type = "error", code = "TURN_INSTRUCTION", message = instruction.Error!.Message }, _run);
            await ExecuteAsync(_floor.Handle(new AgentSpeechEnded(clock.UtcNow)));
            return;
        }

        _responseDone = false;
        _audioIdle = false;
        _audioStarted = false;

        // Only a turn that reacts to someone finishing speaking has a latency; timebox and time-warning turns do not.
        _latencyBaseline = _lastHumanEnd is { } end && clock.UtcNow - end < TimeSpan.FromSeconds(10) ? end : null;
        _lastHumanEnd = null;
        Interlocked.Exchange(ref _firstAudioPending, 1);
        if (question is not null)
        {
            await sender.Send(new RecordStandupEventCommand(_sessionId, StandupEventType.Question, speak.MemberId, question), _run);
        }

        await _voice.CreateResponseAsync(instruction.Value, _run);
        _createSentAt = clock.UtcNow;
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
        lock (_gate)
        {
            text = _utterance.TryGetValue(memberId, out var sb) ? sb.ToString() : string.Empty;
            if (_speculative.TryGetValue(memberId, out var s) && s.Text == text)
            {
                reused = s.Task;
            }

            _utterance.Remove(memberId);
            _speculative.Remove(memberId);
        }

        return await (reused ?? sender.Send(new ReconcileUpdateQuery(_sessionId, memberId, text), _run));
    }

    private async Task AdvanceAsync()
    {
        if (_floor.CurrentMemberId is { } previous)
        {
            await sender.Send(new CompleteSpeakerTurnCommand(_sessionId, previous), _run);
        }

        var next = await sender.Send(new GetNextSpeakerQuery(_sessionId), _run);
        var more = _floor.Handle(next.IsSuccess
            ? new SpeakerSelected(next.Value.MemberId, next.Value.Absent, next.Value.HasFlaggedItems)
            : new SpeakerSelected(null, false));
        await ExecuteAsync(more);
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
            items = (speaker?.Items ?? brief.UnassignedItems).Select(i => new
            {
                id = i.Id.Value,
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

    private string NameOf(string memberId) =>
        _session.PreBrief?.Members.FirstOrDefault(m => m.MemberId == memberId)?.DisplayName ?? memberId;

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Bool(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : 0;
}
