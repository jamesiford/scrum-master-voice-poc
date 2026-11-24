using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Standups.EndStandup;

/// <summary>Ends the session.</summary>
internal sealed class EndStandupHandler(ISessionRepository sessions, IPublisher publisher, IClock clock, ILogger<EndStandupHandler> logger) : IRequestHandler<EndStandupCommand, Result>
{
    public async Task<Result> Handle(EndStandupCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        session.End(clock.UtcNow);
        await sessions.SaveAsync(session, ct);
        logger.LogInformation("StandupEnded raised for session {SessionId}; {Count} event(s) to publish", session.Id, session.DomainEvents.Count);

        // The state change is saved, so its consequences (follow-ups, recap, leaving the call) must finish even when the caller's token is
        // cancelled. The gateway's token dies with the bridge socket, which the leave handler itself closes (ADR 0016).
        await session.PublishEventsAsync(publisher, CancellationToken.None);
        return Result.Success();
    }
}
