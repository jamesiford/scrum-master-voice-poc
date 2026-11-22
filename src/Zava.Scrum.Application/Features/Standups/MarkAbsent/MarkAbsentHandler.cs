using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Standups.MarkAbsent;

/// <summary>Marks a briefed member absent.</summary>
internal sealed class MarkAbsentHandler(ISessionRepository sessions, IClock clock) : IRequestHandler<MarkAbsentCommand, Result>
{
    public async Task<Result> Handle(MarkAbsentCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        if (session.PreBrief?.Members.All(m => m.MemberId != request.MemberId) != false)
        {
            return Result.Failure(StandupErrors.MemberNotInBrief(request.MemberId));
        }

        session.MarkAbsent(request.MemberId, clock.UtcNow);
        await sessions.SaveAsync(session, ct);
        return Result.Success();
    }
}
