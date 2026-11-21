namespace Mcm.Scrum.Domain.Standups;

/// <summary>Everything persisted about a <see cref="StandupSession"/>. Used to save and restore the aggregate.</summary>
/// <param name="Id">Session id.</param>
/// <param name="TeamId">Team id.</param>
/// <param name="CreatedAt">When prepared.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="PreBrief">The pre-brief.</param>
/// <param name="Mode">Bridge mode: <c>simulated</c> or <c>acs</c>.</param>
/// <param name="JoinUrl">Teams join URL (acs mode).</param>
/// <param name="StandupMinutes">Timebox used for this run.</param>
/// <param name="StartedAt">When it started.</param>
/// <param name="EndedAt">When it ended.</param>
/// <param name="Attendance">Attendance by member id.</param>
/// <param name="HandledMemberIds">Members whose turn is over (spoke, or absent and announced).</param>
/// <param name="Events">Recorded events in order.</param>
/// <param name="LatencySamplesMs">End-of-speech to first-audio samples.</param>
public sealed record StandupSessionSnapshot(
    Guid Id,
    string TeamId,
    DateTimeOffset CreatedAt,
    StandupStatus Status,
    PreBrief? PreBrief,
    string? Mode,
    string? JoinUrl,
    int StandupMinutes,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyDictionary<string, Attendance> Attendance,
    IReadOnlyList<string> HandledMemberIds,
    IReadOnlyList<StandupEvent> Events,
    IReadOnlyList<int> LatencySamplesMs);
