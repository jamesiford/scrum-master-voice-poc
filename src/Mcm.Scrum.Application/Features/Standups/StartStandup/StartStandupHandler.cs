using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.StartStandup;

/// <summary>Starts the session once; the team's standup length is the default timebox.</summary>
internal sealed class StartStandupHandler(ISessionRepository sessions, ITeamRepository teams, IRetroSettings retro, IClock clock)
    : IRequestHandler<StartStandupCommand, Result>
{
    public async Task<Result> Handle(StartStandupCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var minutes = request.StandupMinutes
            ?? (session.Kind == Domain.Standups.CeremonyKind.Retrospective ? retro.Minutes : (await teams.GetAsync(session.TeamId, ct))?.StandupMinutes)
            ?? 15;
        var started = session.Start(request.Mode, request.JoinUrl, minutes, clock.UtcNow);
        if (started.IsSuccess)
        {
            await sessions.SaveAsync(session, ct);
        }

        return started;
    }
}
