using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Domain.Standups;

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
    }

    /// <summary>Gets the session id.</summary>
    public Guid Id { get; }

    /// <summary>Gets the owning team.</summary>
    public string TeamId { get; }

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
    /// <returns>A prepared session.</returns>
    public static StandupSession Prepare(Guid id, string teamId, DateTimeOffset now, PreBrief? preBrief)
    {
        var session = new StandupSession(new StandupSessionSnapshot(id, teamId, now, StandupStatus.Prepared, preBrief, null, null, 15, null, null, new Dictionary<string, Attendance>(), [], [], []));
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
        new(Id, TeamId, CreatedAt, Status, PreBrief, Mode, JoinUrl, StandupMinutes, StartedAt, EndedAt, new Dictionary<string, Attendance>(_attendance), [.. _handled], [.. _events], [.. _latency]);

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
        Raise(new StandupEnded(Id, now));
    }

    /// <summary>Records an event. Work item ids must already be grounded by the caller (rule 2).</summary>
    /// <param name="type">Kind.</param>
    /// <param name="memberId">Member concerned.</param>
    /// <param name="workItemIds">Work items referred to.</param>
    /// <param name="text">What happened.</param>
    /// <param name="now">Current time.</param>
    public void RecordEvent(StandupEventType type, string? memberId, IReadOnlyList<int> workItemIds, string text, DateTimeOffset now) =>
        _events.Add(new StandupEvent(Guid.NewGuid(), now, type, memberId, workItemIds, text));

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

    /// <summary>Gets the next member who has not had a turn, in speaking order.</summary>
    /// <returns>The member's brief and whether they are absent, or <see langword="null"/> when everyone is done.</returns>
    public (MemberBrief Member, bool Absent)? NextSpeaker()
    {
        var next = PreBrief?.Members.FirstOrDefault(m => !_handled.Contains(m.MemberId));
        return next is null ? null : (next, _attendance.TryGetValue(next.MemberId, out var a) && a == Standups.Attendance.Absent);
    }

    /// <summary>Adds an end-of-speech to first-audio latency sample.</summary>
    /// <param name="milliseconds">The sample.</param>
    public void AddLatency(int milliseconds) => _latency.Add(Math.Max(0, milliseconds));
}
