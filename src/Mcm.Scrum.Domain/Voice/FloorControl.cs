namespace Mcm.Scrum.Domain.Voice;

/// <summary>
/// The pure floor-control state machine (SPEC 11.3): who may speak and when. No I/O and no clock; time arrives on inputs.
/// The gateway feeds it events and executes the actions it returns. The agent speaks only when an action says so.
/// </summary>
public sealed class FloorControl(FloorOptions options)
{
    private static readonly FloorAction[] None = [];
    private readonly Dictionary<string, int> _clarified = [];
    private DateTimeOffset? _startedAt;
    private DateTimeOffset _listeningSince;
    private DateTimeOffset _lastHolderEnd;
    private DateTimeOffset _lastAnyEnd;
    private bool _heard;
    private bool _turnHeard;
    private readonly HashSet<string> _active = [];
    private bool _canClarify = true;
    private int _offHolderMs;
    private bool _absentCurrent;
    private bool _warned;
    private bool _warnPending;
    private bool _timeUp;
    private int _version;
    private FloorState _resumeState = FloorState.Idle;

    /// <summary>Gets the current state.</summary>
    public FloorState State { get; private set; } = FloorState.Idle;

    /// <summary>Gets the member who holds the floor, if any.</summary>
    public string? CurrentMemberId { get; private set; }

    /// <summary>Gets a value indicating whether the agent is meant to be speaking right now.</summary>
    public bool AgentHasFloor => State is FloorState.Opening or FloorState.CallingOn or FloorState.Clarifying or FloorState.Interjecting or FloorState.Recap or FloorState.Deferring;

    /// <summary>Applies an input and returns what the gateway must do.</summary>
    /// <param name="input">The input.</param>
    /// <returns>Actions in order; empty when nothing to do.</returns>
    public IReadOnlyList<FloorAction> Handle(FloorInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input switch
        {
            StartFloor s => OnStart(s),
            AgentSpeechEnded e => OnAgentSpeechEnded(e),
            DeferCurrent d => OnDefer(d.Reason),
            SpeakerSelected s => OnSpeakerSelected(s),
            MemberSpoke m => OnMemberSpoke(m),
            Tick t => OnTick(t.Now),
            ReconcileFinished r => OnReconcileFinished(r),
            HumanOverride h => OnSuspend(h.On, FloorState.HumanOverride),
            PauseFloor p => OnSuspend(p.On, FloorState.Paused),
            AgentAddressed => OnAddressed(),
            SkipSpeaker => OnSkip(),
            BargeIn b => OnBargeIn(b),
            SpeechActive s => OnSpeechActive(s),
            _ => None,
        };
    }

    private FloorAction[] OnStart(StartFloor s)
    {
        if (State != FloorState.Idle)
        {
            return None;
        }

        _startedAt = s.Now;
        State = FloorState.Opening;
        return [new AgentSpeak(SpeakReason.Open, null)];
    }

    private FloorAction[] OnAgentSpeechEnded(AgentSpeechEnded e)
    {
        if (!AgentHasFloor)
        {
            return None;
        }

        if (_timeUp && State != FloorState.Recap)
        {
            return StartRecap();
        }

        switch (State)
        {
            case FloorState.Opening:
                State = FloorState.Advancing;
                return [new AdvanceSpeaker()];
            case FloorState.CallingOn when _absentCurrent:
                State = FloorState.Advancing;
                return [new AdvanceSpeaker()];
            case FloorState.CallingOn:
                BeginListening(e.Now, resetHeard: true);
                return None;
            case FloorState.Clarifying:
                BeginListening(e.Now, resetHeard: true);
                return None;
            case FloorState.Deferring:
                State = FloorState.Advancing;
                return [new AdvanceSpeaker()];
            case FloorState.Interjecting:
                State = FloorState.Listening;
                _offHolderMs = 0;
                _lastAnyEnd = e.Now;
                return None;
            default:
                State = FloorState.Closed;
                return [new EndSession()];
        }
    }

    private FloorAction[] OnSpeakerSelected(SpeakerSelected s)
    {
        if (State != FloorState.Advancing)
        {
            return None;
        }

        if (s.MemberId is null)
        {
            return StartRecap();
        }

        CurrentMemberId = s.MemberId;
        _absentCurrent = s.Absent;
        _canClarify = s.CanClarify;
        _offHolderMs = 0;
        _heard = false;
        _turnHeard = false;
        State = FloorState.CallingOn;
        return [new AgentSpeak(s.Absent ? SpeakReason.Absent : s.CircleBack ? SpeakReason.CircleBack : SpeakReason.CallOn, s.MemberId)];
    }

    private FloorAction[] OnMemberSpoke(MemberSpoke m)
    {
        if (State != FloorState.Listening && State != FloorState.Reconciling)
        {
            return None;
        }

        _lastAnyEnd = m.At;
        if (m.MemberId == CurrentMemberId)
        {
            _heard = true;
            _turnHeard = true;
            _lastHolderEnd = m.At;
            if (State == FloorState.Reconciling)
            {
                _version++;
                State = FloorState.Listening;
            }
        }
        else
        {
            _offHolderMs += m.DurationMs;
        }

        return None;
    }

    private FloorAction[] OnTick(DateTimeOffset now)
    {
        if (_startedAt is null || State is FloorState.Idle or FloorState.Recap or FloorState.Closed)
        {
            return None;
        }

        var timebox = CheckTimebox(now);
        if (timebox is not null)
        {
            return timebox;
        }

        return State == FloorState.Listening ? OnListeningTick(now) : None;
    }

    private FloorAction[]? CheckTimebox(DateTimeOffset now)
    {
        var elapsed = now - _startedAt!.Value;
        if (!_timeUp && elapsed >= options.StandupLength)
        {
            _timeUp = true;
            return State is FloorState.Listening or FloorState.Reconciling or FloorState.Advancing ? StartRecap() : None;
        }

        if (!_warned && elapsed >= options.StandupLength * options.WarnFraction)
        {
            _warned = true;
            _warnPending = true;
        }

        return null;
    }

    private FloorAction[] OnSpeechActive(SpeechActive s)
    {
        if (State != FloorState.Listening && State != FloorState.Reconciling)
        {
            return None;
        }

        // Anyone talking holds back the silence timer, so a side discussion cannot be mistaken for the holder finishing.
        _ = s.Active ? _active.Add(s.MemberId) : _active.Remove(s.MemberId);
        _lastAnyEnd = s.At;
        if (s.MemberId != CurrentMemberId)
        {
            if (s.Active && State == FloorState.Reconciling)
            {
                // A teammate started talking while the agent prepared its question: the stale result is dropped and the agent waits.
                _version++;
                State = FloorState.Listening;
            }

            return None;
        }

        if (s.Active)
        {
            _heard = true;
            _turnHeard = true;
            if (State == FloorState.Reconciling)
            {
                _version++;
                State = FloorState.Listening;
            }
        }
        else
        {
            _lastHolderEnd = s.At;
        }

        return None;
    }

    private FloorAction[] OnListeningTick(DateTimeOffset now)
    {
        if (_active.Count > 0)
        {
            // Safety net: a lost "speech stopped" must not freeze the standup.
            if (now - _lastAnyEnd < TimeSpan.FromSeconds(30))
            {
                return None;
            }

            _active.Clear();
        }

        var pause = now - (_lastAnyEnd == default ? _listeningSince : _lastAnyEnd) >= TimeSpan.FromMilliseconds(options.PauseMs);
        if (_warnPending && pause)
        {
            _warnPending = false;
            return Interject(SpeakReason.TimeWarning, null);
        }

        if (_offHolderMs >= options.ParkAfterOffTopicMs && pause)
        {
            _offHolderMs = 0;
            return Interject(SpeakReason.Park, new ParkTopic());
        }

        if (_heard && now - _lastAnyEnd >= TimeSpan.FromMilliseconds(options.SilenceMs))
        {
            return FinishUpdate();
        }

        if (!_heard && now - _listeningSince >= TimeSpan.FromMilliseconds(options.NoResponseMs))
        {
            // Silence after a clarifying question is not "no response": the member already spoke this turn.
            if (_turnHeard)
            {
                State = FloorState.Advancing;
                return [new AdvanceSpeaker()];
            }

            return OnDefer(DeferReason.NoResponse);
        }

        return None;
    }

    // Latency: when no question is possible (nothing flagged, or the one question was already asked) the next speaker is called
    // at once and the reconcile only records events in the background.
    private FloorAction[] FinishUpdate()
    {
        _version++;
        if (!_canClarify || _clarified.GetValueOrDefault(CurrentMemberId!) >= options.MaxClarifications)
        {
            State = FloorState.Advancing;
            return [new RunReconcile(CurrentMemberId!, _version, Background: true), new AdvanceSpeaker()];
        }

        State = FloorState.Reconciling;
        return [new RunReconcile(CurrentMemberId!, _version)];
    }

    private FloorAction[] OnReconcileFinished(ReconcileFinished r)
    {
        if (State != FloorState.Reconciling || r.Version != _version)
        {
            return None;
        }

        if (r.HasQuestion && _clarified.GetValueOrDefault(CurrentMemberId!) < options.MaxClarifications)
        {
            _clarified[CurrentMemberId!] = _clarified.GetValueOrDefault(CurrentMemberId!) + 1;
            State = FloorState.Clarifying;
            return [new AgentSpeak(SpeakReason.Clarify, CurrentMemberId)];
        }

        State = FloorState.Advancing;
        return [new AdvanceSpeaker()];
    }

    private FloorAction[] OnSuspend(bool on, FloorState target)
    {
        if (on)
        {
            if (State is FloorState.Idle or FloorState.Closed or FloorState.HumanOverride or FloorState.Paused)
            {
                State = State is FloorState.Idle or FloorState.Closed ? State : target;
                return None;
            }

            var cancel = AgentHasFloor;
            _resumeState = State;
            State = target;
            return cancel ? [new CancelResponse()] : None;
        }

        if (State != target)
        {
            return None;
        }

        if (_resumeState == FloorState.Advancing || CurrentMemberId is null)
        {
            State = FloorState.Advancing;
            return [new AdvanceSpeaker()];
        }

        BeginListening(_lastAnyEnd == default ? _listeningSince : _lastAnyEnd, resetHeard: false);
        return None;
    }

    // Goal-driven: a member who cannot speak now is skipped, not lost. The gateway records why and the agent circles back at the end.
    private FloorAction[] OnDefer(DeferReason reason)
    {
        if (CurrentMemberId is null || State is not (FloorState.Listening or FloorState.Reconciling or FloorState.CallingOn or FloorState.Clarifying))
        {
            return None;
        }

        var cancel = AgentHasFloor;
        State = FloorState.Deferring;
        _version++;
        FloorAction speak = new AgentSpeak(reason == DeferReason.NoResponse ? SpeakReason.NoResponse : SpeakReason.StepAway, CurrentMemberId);
        FloorAction defer = new DeferSpeaker(CurrentMemberId, reason);
        return cancel ? [new CancelResponse(), defer, speak] : [defer, speak];
    }

    private FloorAction[] OnAddressed() =>
        State == FloorState.Listening ? Interject(SpeakReason.Addressed, null) : None;

    private FloorAction[] OnSkip()
    {
        if (State is not (FloorState.Listening or FloorState.Reconciling or FloorState.Clarifying or FloorState.CallingOn))
        {
            return None;
        }

        var cancel = AgentHasFloor;
        State = FloorState.Advancing;
        return cancel ? [new CancelResponse(), new AdvanceSpeaker()] : [new AdvanceSpeaker()];
    }

    private FloorAction[] OnBargeIn(BargeIn b)
    {
        var holderOrHuman = b.IsHuman || b.MemberId == CurrentMemberId;
        if (!holderOrHuman || !AgentHasFloor || State is FloorState.Opening or FloorState.Recap || _absentCurrent)
        {
            return None;
        }

        State = FloorState.Listening;
        _listeningSince = _lastAnyEnd;
        return [new CancelResponse()];
    }

    private FloorAction[] Interject(SpeakReason reason, FloorAction? first)
    {
        State = FloorState.Interjecting;
        FloorAction speak = new AgentSpeak(reason, CurrentMemberId);
        return first is null ? [speak] : [first, speak];
    }

    private FloorAction[] StartRecap()
    {
        State = FloorState.Recap;
        _version++;
        return [new AgentSpeak(SpeakReason.Recap, null)];
    }

    private void BeginListening(DateTimeOffset now, bool resetHeard)
    {
        State = FloorState.Listening;
        _listeningSince = now;
        _lastAnyEnd = default;
        _offHolderMs = 0;
        _active.Clear();
        if (resetHeard)
        {
            _heard = false;
        }
    }
}
