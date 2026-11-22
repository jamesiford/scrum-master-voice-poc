using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Standups.ResolveUnblocker;

/// <summary>Matches the answer to a roster member in code, then stores the match on the blocker event.</summary>
internal sealed class ResolveUnblockerHandler(ISessionRepository sessions) : IRequestHandler<ResolveUnblockerCommand, Result<string?>>
{
    public async Task<Result<string?>> Handle(ResolveUnblockerCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<string?>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var blocker = session.OpenBlockerAwaitingUnblocker(request.MemberId);
        var named = blocker is null ? null : UnblockerResolver.Resolve(request.Answer, session.PreBrief?.Members ?? [], request.MemberId);
        if (blocker is null || named is null)
        {
            return Result<string?>.Success(null);
        }

        session.SetUnblocker(blocker.Id, named);
        await sessions.SaveAsync(session, ct);
        return Result<string?>.Success(named);
    }
}
