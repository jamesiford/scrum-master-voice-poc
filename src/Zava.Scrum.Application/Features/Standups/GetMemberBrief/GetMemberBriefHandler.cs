using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Standups.GetMemberBrief;

/// <summary>Finds the member's brief.</summary>
internal sealed class GetMemberBriefHandler(ISessionRepository sessions) : IRequestHandler<GetMemberBriefQuery, Result<MemberBrief>>
{
    public async Task<Result<MemberBrief>> Handle(GetMemberBriefQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<MemberBrief>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var brief = session.PreBrief?.Members.FirstOrDefault(m => m.MemberId == request.MemberId);
        return brief is null
            ? Result<MemberBrief>.Failure(StandupErrors.MemberNotInBrief(request.MemberId))
            : Result<MemberBrief>.Success(brief);
    }
}
