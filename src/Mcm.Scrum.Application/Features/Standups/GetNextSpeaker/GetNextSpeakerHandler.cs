using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.GetNextSpeaker;

/// <summary>Reads the next speaker from the session.</summary>
internal sealed class GetNextSpeakerHandler(ISessionRepository sessions) : IRequestHandler<GetNextSpeakerQuery, Result<NextSpeakerDto>>
{
    public async Task<Result<NextSpeakerDto>> Handle(GetNextSpeakerQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<NextSpeakerDto>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var next = session.NextSpeaker();
        return Result<NextSpeakerDto>.Success(next is null
            ? new NextSpeakerDto(null, null, null, false, false)
            : new NextSpeakerDto(next.Value.Member.MemberId, next.Value.Member.DisplayName, Names.First(next.Value.Member.DisplayName), next.Value.Absent, next.Value.Member.Items.Any(i => i.Flags.Count > 0)));
    }
}
