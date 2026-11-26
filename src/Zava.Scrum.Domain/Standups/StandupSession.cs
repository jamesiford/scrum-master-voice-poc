using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Standups;

/// <summary>One daily standup: pre-brief, attendance, speaking progress, events and latency.</summary>
public sealed class StandupSession : AggregateRoot
{
    private readonly Dictionary<string, Attendance> _attendance;
    private readonly List<string> _handled;
    private readonly List<StandupEvent> _events;
    private readonly List<int> _latency;

    private StandupSession(StandupSessionSnapshot s)
    {
        Id = s.Id;
        TeamId = s.TeamId;
        CreatedAt = s.CreatedAt;
        Status = s.Status;
        PreBrief = s.PreBrief;
        Mode = s.Mode;
        JoinUrl = s.JoinUrl;
        StandupMinutes = s.StandupMinutes;
        StartedAt = s.StartedAt;
        EndedAt = s.EndedAt;
        _attendance = new Dictionary<string, Attendance>(s.Attendance);
        _handled = [.. s.HandledMemberIds];
        _events = [.. s.Events];
        _latency = [.. s.LatencySamplesMs];
        Kind = s.Kind;
        CorrelationId = s.CorrelationId ?? Correlation.ForLegacySession(s.Id);
        HostWarmUp = s.HostWarmUp;
        MeetingLifecycle = s.MeetingLifecycle;
    }

    /// <summary>Gets the session id.</summary>
    public Guid Id { get; }

    /// <summary>Gets the owning team.</summary>
    public string TeamId { get; }

    /// <summary>Gets the correlation id (a W3C trace id) created with the session and carried through the join, each turn, the proposal, the approval, the Azure DevOps write and the outbox.</summary>
    public string CorrelationId { get; }

    /// <summary>Gets the result of warming the agent host for this standup, or <see langword="null"/> when none was recorded.</summary>
    public HostWarmUp? HostWarmUp { get; private set; }

    /// <summary>Records the result of warming the agent host (ADR 0021).</summary>
    /// <param name="warmUp">What happened.</param>
    public void RecordHostWarmUp(HostWarmUp warmUp)
    {
        ArgumentNullException.ThrowIfNull(warmUp);
        HostWarmUp = warmUp;
    }

    /// <summary>Gets the ceremony this session runs.</summary>
    public CeremonyKind Kind { get; }

    /// <summary>Gets when the session was prepared.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the lifecycle status.</summary>
    public StandupStatus Status { get; private set; }

    /// <summary>Gets the pre-brief built when the session was prepared.</summary>
    public PreBrief? PreBrief { get; }

    /// <summary>Gets the bridge mode, once started.</summary>
    public string? Mode { get; private set; }

    /// <summary>Gets the Teams join URL, if any.</summary>
    public string? JoinUrl { get; private set; }

    /// <summary>Gets correlated Teams lifecycle metadata. This contains no participant or meeting content.</summary>
    public MeetingLifecycle? MeetingLifecycle { get; private set; }

    /// <summary>Checks an event against this session's stored meeting and any previously correlated identity.</summary>
    /// <param name="meetingId">The id from channel data, never the payload's id.</param>
    /// <param name="conversationId">The unambiguous bound conversation.</param>
    /// <param name="joinUrl">The join URL in the typed meeting details.</param>
    /// <returns>Whether the event belongs to this session.</returns>
    public bool MatchesMeeting(string meetingId, string conversationId, string joinUrl) =>
        !string.IsNullOrWhiteSpace(meetingId) &&
        !string.IsNullOrWhiteSpace(conversationId) &&
        Uri.TryCreate(JoinUrl, UriKind.Absolute, out var stored) &&
        Uri.TryCreate(joinUrl, UriKind.Absolute, out var received) &&
        stored.Scheme == Uri.UriSchemeHttps &&
        stored.Equals(received) &&
        MatchesMeetingConversation(stored, conversationId) &&
        (MeetingLifecycle is null ||
            MeetingLifecycle.MeetingId == meetingId && MeetingLifecycle.ConversationId == conversationId);

    private static bool MatchesMeetingConversation(Uri joinUrl, string conversationId)
    {
        // Scheduled Teams join links name their meeting chat. A different bound chat on the same team cannot end this ceremony.
        var segments = joinUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is ["l", "meetup-join", var thread, ..] &&
            string.Equals(Uri.UnescapeDataString(thread), conversationId.Split(";messageid=", 2)[0], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Records a matched meeting event without starting an unprepared ceremony. Duplicate events change nothing.</summary>
    /// <param name="meetingId">The id from Teams channel data.</param>
    /// <param name="conversationId">The bound conversation.</param>
    /// <param name="joinUrl">The meeting's join URL.</param>
    /// <param name="ended">Whether this is an end event.</param>
    /// <param name="now">When the event was received.</param>
    /// <returns>Whether metadata changed.</returns>
    public bool RecordMeetingEvent(string meetingId, string conversationId, string joinUrl, bool ended, DateTimeOffset now)
    {
        if (Status != StandupStatus.Live || !MatchesMeeting(meetingId, conversationId, joinUrl))
        {
            return false;
        }

        var current = MeetingLifecycle ?? new MeetingLifecycle(meetingId, conversationId, null, null);
        if (ended ? current.EndedAt is not null : current.StartedAt is not null)
        {
            return false;
        }

        MeetingLifecycle = ended ? current with { EndedAt = now } : current with { StartedAt = now };
        return true;
    }

    /// <summary>Gets the timebox in minutes for this run.</summary>
    public int StandupMinutes { get; private set; }

    /// <summary>Gets when the standup started.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Gets when the standup ended.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>Gets attendance by member id.</summary>
    public IReadOnlyDictionary<string, Attendance> Attendance => _attendance;

    /// <summary>Gets members whose turn is over.</summary>
    public IReadOnlyList<string> HandledMemberIds => _handled;

    /// <summary>Gets the recorded events.</summary>
    public IReadOnlyList<StandupEvent> Events => _events;

    /// <summary>Gets latency samples in milliseconds.</summary>
    public IReadOnlyList<int> LatencySamplesMs => _latency;

    /// <summary>Prepares a new session with its pre-brief.</summary>
    /// <param name="id">The session id.</param>
    /// <param name="teamId">The owning team.</param>
    /// <param name="now">Current time, supplied by the caller (no ambient clock in Domain).</param>
    /// <param name="preBrief">The pre-brief.</param>
    /// <param name="kind">The ceremony.</param>
    /// <returns>A prepared session.</returns>
    public static StandupSession Prepare(Guid id, string teamId, DateTimeOffset now, PreBrief? preBrief, CeremonyKind kind = CeremonyKind.DailyScrum)
    {
        var session = new StandupSession(new StandupSessionSnapshot(id, teamId, now, StandupStatus.Prepared, preBrief, null, null, 15, null, null, new Dictionary<string, Attendance>(), [], [], [], kind, Correlation.New()));
        session.Raise(new StandupPrepared(id, now));
        return session;
    }

    /// <summary>Rehydrates a persisted session.</summary>
    /// <param name="snapshot">Persisted state.</param>
    /// <returns>The session.</returns>
    public static StandupSession Restore(StandupSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new StandupSession(snapshot);
    }

    /// <summary>Captures the state for persistence.</summary>
    /// <returns>The snapshot.</returns>
    public StandupSessionSnapshot ToSnapshot() =>
        new(Id, TeamId, CreatedAt, Status, PreBrief, Mode, JoinUrl, StandupMinutes, StartedAt, EndedAt, new Dictionary<string, Attendance>(_attendance), [.. _handled], [.. _events], [.. _latency], Kind, CorrelationId, HostWarmUp, MeetingLifecycle);

    /// <summary>Starts the standup. A session can start once, and only after it was prepared.</summary>
    /// <param name="mode">Bridge mode.</param>
    /// <param name="joinUrl">Teams join URL, if any.</param>
    /// <param name="standupMinutes">Timebox.</param>
    /// <param name="now">Current time.</param>
    /// <returns>Success, or a conflict when it already started.</returns>
    public Result Start(string mode, string? joinUrl, int standupMinutes, DateTimeOffset now)
    {
        if (Status != StandupStatus.Prepared)
        {
            return Result.Failure(Error.Conflict("standup.already_started", $"Session is {Status}, not Prepared."));
        }

        Status = StandupStatus.Live;
        Mode = mode;
        JoinUrl = joinUrl;
        StandupMinutes = standupMinutes;
        StartedAt = now;
        return Result.Success();
    }

    /// <summary>Ends the standup. Ending twice is not an error so a disconnect after "End" is harmless.</summary>
    /// <param name="now">Current time.</param>
    public void End(DateTimeOffset now)
    {
        if (Status == StandupStatus.Ended)
        {
            return;
        }

        Status = StandupStatus.Ended;
        EndedAt = now;
        // A retrospective has its own handlers: the standup recap and per-member follow-ups must not fire for it.
        if (Kind == CeremonyKind.Retrospective)
        {
            Raise(new RetrospectiveEnded(Id, now));
            return;
        }

        Raise(new StandupEnded(Id, now));
    }

    /// <summary>Records an event. Work item ids must already be grounded by the caller (rule 2).</summary>
    /// <param name="type">Kind.</param>
    /// <param name="memberId">Member concerned.</param>
    /// <param name="workItemIds">Work items referred to.</param>
    /// <param name="text">What happened.</param>
    /// <param name="now">Current time.</param>
    /// <param name="category">For a retrospective note, its column.</param>
    /// <param name="ownerMemberId">For a retrospective action item, its owner.</param>
    public void RecordEvent(
        StandupEventType type,
        string? memberId,
        IReadOnlyList<int> workItemIds,
        string text,
        DateTimeOffset now,
        RetroCategory? category = null,
        string? ownerMemberId = null) =>
        _events.Add(new StandupEvent(Guid.NewGuid(), now, type, memberId, workItemIds, text, Category: category, OwnerMemberId: ownerMemberId));

    /// <summary>Records who can help unblock a blocker (M9-R2). The id must come from the roster; the caller resolves the spoken name in code.</summary>
    /// <param name="eventId">The blocker event.</param>
    /// <param name="unblockerMemberId">The roster member who can help.</param>
    /// <returns><see langword="true"/> when a blocker event was updated.</returns>
    public bool SetUnblocker(Guid eventId, string unblockerMemberId)
    {
        var i = _events.FindIndex(e => e.Id == eventId && e.Type == StandupEventType.Blocker && !e.Retracted);
        if (i < 0)
        {
            return false;
        }

        _events[i] = _events[i] with { UnblockerMemberId = unblockerMemberId };
        return true;
    }

    /// <summary>Gets the member's latest active blocker that nobody has been named to unblock yet.</summary>
    /// <param name="memberId">The member.</param>
    /// <returns>The event, or <see langword="null"/>.</returns>
    public StandupEvent? OpenBlockerAwaitingUnblocker(string memberId) =>
        ActiveEvents.LastOrDefault(e => e.Type == StandupEventType.Blocker && e.MemberId == memberId && e.UnblockerMemberId is null);

    /// <summary>Marks a member absent today and records it once.</summary>
    /// <param name="memberId">The member.</param>
    /// <param name="now">Current time.</param>
    public void MarkAbsent(string memberId, DateTimeOffset now)
    {
        if (_attendance.TryGetValue(memberId, out var current) && current == Standups.Attendance.Absent)
        {
            return;
        }

        _attendance[memberId] = Standups.Attendance.Absent;
        RecordEvent(StandupEventType.Absent, memberId, [], "Absent today", now);
    }

    /// <summary>Marks a member's turn as over.</summary>
    /// <param name="memberId">The member.</param>
    public void CompleteTurn(string memberId)
    {
        if (!_handled.Contains(memberId))
        {
            _handled.Add(memberId);
        }
    }

    /// <summary>Gets the events that still count: everything the speakers have not retracted.</summary>
    public IEnumerable<StandupEvent> ActiveEvents => _events.Where(e => !e.Retracted);

    /// <summary>Marks an earlier statement as corrected by its speaker. The event stays in the audit trail but no rule uses it.</summary>
    /// <param name="eventId">The event.</param>
    /// <returns><see langword="true"/> when an event was retracted.</returns>
    public bool Retract(Guid eventId)
    {
        var i = _events.FindIndex(e => e.Id == eventId && !e.Retracted);
        if (i < 0)
        {
            return false;
        }

        _events[i] = _events[i] with { Retracted = true };
        return true;
    }

    /// <summary>Checks whether a member was skipped for now and has not been circled back to yet.</summary>
    /// <param name="memberId">The member.</param>
    /// <returns><see langword="true"/> when they are waiting to be asked again.</returns>
    public bool IsDeferred(string memberId)
    {
        var deferred = _events.FindLastIndex(e => e.Type == StandupEventType.Deferred && e.MemberId == memberId);
        return deferred >= 0 && _events.FindLastIndex(e => e.Type == StandupEventType.CircledBack && e.MemberId == memberId) < deferred;
    }

    /// <summary>Counts how many times a member was called and did not respond.</summary>
    /// <param name="memberId">The member.</param>
    /// <returns>The count.</returns>
    public int NoResponseCount(string memberId) => _events.Count(e => e.Type == StandupEventType.NoResponse && e.MemberId == memberId);

    /// <summary>Checks whether a member said anything in the standup, in their own voice or through someone speaking for them.</summary>
    /// <param name="memberId">The member.</param>
    /// <returns><see langword="true"/> when at least one statement is attributed to them.</returns>
    public bool HasSpoken(string memberId) =>
        _events.Any(e => e.MemberId == memberId && e.Type is StandupEventType.Transcript or StandupEventType.Commitment or StandupEventType.Blocker or StandupEventType.Decision);

    /// <summary>
    /// Gets the next member to ask: members in speaking order first, skipping anyone deferred, then the deferred members so
    /// nobody is forgotten (the agent circles back at the end).
    /// </summary>
    /// <returns>The member, whether they are absent and whether this is a circle-back, or <see langword="null"/> when everyone is done.</returns>
    public (MemberBrief Member, bool Absent, bool CircleBack)? NextSpeaker() =>
        StandupAgenda.Instance.Next(this) is MemberTurn m ? (m.Member, m.Absent, m.CircleBack) : null;

    /// <summary>Adds an end-of-speech to first-audio latency sample.</summary>
    /// <param name="milliseconds">The sample.</param>
    public void AddLatency(int milliseconds) => _latency.Add(Math.Max(0, milliseconds));
}
