using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Standups.GetSession;

/// <summary>Loads a session or reports it missing.</summary>
internal sealed class GetSessionHandler(ISessionRepository sessions) : IRequestHandler<GetSessionQuery, Result<SessionDto>>
{
    public async Task<Result<SessionDto>> Handle(GetSessionQuery request, CancellationToken ct)
    {
        var s = await sessions.GetAsync(request.SessionId, ct);
        if (s is null)
        {
            return Result<SessionDto>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        return Result<SessionDto>.Success(new SessionDto(
            s.Id,
            s.TeamId,
            s.Status,
            s.CreatedAt,
            s.PreBrief,
            s.Mode,
            s.StartedAt,
            s.EndedAt,
            s.Events,
            s.Attendance,
            LatencyStats.Percentile(s.LatencySamplesMs.ToList(), 50),
            LatencyStats.Percentile(s.LatencySamplesMs.ToList(), 95),
            s.Events.Count(e => e.Type == StandupEventType.Question),
            s.StandupMinutes,
            s.Kind));
    }
}
