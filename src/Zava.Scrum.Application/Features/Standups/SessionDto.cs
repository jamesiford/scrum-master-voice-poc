using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Standups;

/// <summary>Session detail including the pre-brief, events and latency.</summary>
/// <param name="Id">Session id.</param>
/// <param name="TeamId">Team id.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="CreatedAt">When it was prepared.</param>
/// <param name="PreBrief">The pre-brief.</param>
/// <param name="Mode">Bridge mode, once started.</param>
/// <param name="StartedAt">When it started.</param>
/// <param name="EndedAt">When it ended.</param>
/// <param name="Events">Recorded events in order.</param>
/// <param name="Attendance">Attendance by member id.</param>
/// <param name="P50LatencyMs">Median end-of-speech to first-audio latency.</param>
/// <param name="P95LatencyMs">95th percentile latency.</param>
/// <param name="QuestionsAsked">Clarifying questions the agent asked.</param>
/// <param name="StandupMinutes">Timebox for this run.</param>
/// <param name="Kind">The ceremony.</param>
public sealed record SessionDto(
    Guid Id,
    string TeamId,
    StandupStatus Status,
    DateTimeOffset CreatedAt,
    PreBrief? PreBrief,
    string? Mode,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<StandupEvent> Events,
    IReadOnlyDictionary<string, Attendance> Attendance,
    int P50LatencyMs,
    int P95LatencyMs,
    int QuestionsAsked,
    int StandupMinutes,
    CeremonyKind Kind = CeremonyKind.DailyScrum);
