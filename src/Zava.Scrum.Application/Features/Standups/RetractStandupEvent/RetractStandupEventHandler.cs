using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Standups.RetractStandupEvent;

/// <summary>Retracts the event on the session.</summary>
internal sealed class RetractStandupEventHandler(ISessionRepository sessions) : IRequestHandler<RetractStandupEventCommand, Result>
{
    public async Task<Result> Handle(RetractStandupEventCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        if (!session.Retract(request.EventId))
        {
            return Result.Failure(Error.NotFound("event.not_found", "That event does not exist or was already retracted."));
        }

        await sessions.SaveAsync(session, ct);
        return Result.Success();
    }
}
