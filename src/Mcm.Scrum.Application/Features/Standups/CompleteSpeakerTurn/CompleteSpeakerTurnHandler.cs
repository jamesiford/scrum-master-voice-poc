using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.CompleteSpeakerTurn;

/// <summary>Completes a turn.</summary>
internal sealed class CompleteSpeakerTurnHandler(ISessionRepository sessions) : IRequestHandler<CompleteSpeakerTurnCommand, Result>
{
    public async Task<Result> Handle(CompleteSpeakerTurnCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        session.CompleteTurn(request.MemberId);
        await sessions.SaveAsync(session, ct);
        return Result.Success();
    }
}
