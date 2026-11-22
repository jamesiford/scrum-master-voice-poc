using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.GetTimeRemaining;

/// <summary>Computes the time left from the start time and the timebox.</summary>
internal sealed class GetTimeRemainingHandler(ISessionRepository sessions, IClock clock)
    : IRequestHandler<GetTimeRemainingQuery, Result<TimeRemainingDto>>
{
    public async Task<Result<TimeRemainingDto>> Handle(GetTimeRemainingQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<TimeRemainingDto>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var total = session.StandupMinutes * 60;
        var elapsed = session.StartedAt is { } started ? (int)(clock.UtcNow - started).TotalSeconds : 0;
        return Result<TimeRemainingDto>.Success(new TimeRemainingDto(Math.Max(0, total - elapsed), total, elapsed >= total * 0.8));
    }
}
