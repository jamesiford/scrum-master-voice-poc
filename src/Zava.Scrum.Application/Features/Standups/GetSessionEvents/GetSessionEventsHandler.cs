using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Standups.GetSessionEvents;

/// <summary>Loads events.</summary>
internal sealed class GetSessionEventsHandler(ISessionRepository sessions)
    : IRequestHandler<GetSessionEventsQuery, Result<IReadOnlyList<StandupEvent>>>
{
    public async Task<Result<IReadOnlyList<StandupEvent>>> Handle(GetSessionEventsQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        return session is null
            ? Result<IReadOnlyList<StandupEvent>>.Failure(StandupErrors.SessionNotFound(request.SessionId))
            : Result<IReadOnlyList<StandupEvent>>.Success(session.Events);
    }
}
