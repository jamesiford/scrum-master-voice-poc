using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Standups.GetSessionEvents;

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
