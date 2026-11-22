using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.EndStandup;

/// <summary>Ends the session.</summary>
internal sealed class EndStandupHandler(ISessionRepository sessions, IPublisher publisher, IClock clock) : IRequestHandler<EndStandupCommand, Result>
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
        await session.PublishEventsAsync(publisher, ct);
        return Result.Success();
    }
}
