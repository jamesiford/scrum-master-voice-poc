using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.RecordAgentLatency;

/// <summary>Adds the sample to the session.</summary>
internal sealed class RecordAgentLatencyHandler(ISessionRepository sessions) : IRequestHandler<RecordAgentLatencyCommand, Result>
{
    public async Task<Result> Handle(RecordAgentLatencyCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        session.AddLatency(request.Milliseconds);
        await sessions.SaveAsync(session, ct);
        return Result.Success();
    }
}
