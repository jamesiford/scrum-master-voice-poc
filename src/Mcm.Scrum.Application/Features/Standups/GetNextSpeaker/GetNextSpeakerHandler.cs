using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;

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

        ICeremonyAgenda agenda = session.Kind == CeremonyKind.Retrospective ? RetroAgenda.Instance : StandupAgenda.Instance;
        return Result<NextSpeakerDto>.Success(agenda.Next(session) switch
        {
            MemberTurn t => new NextSpeakerDto(t.Member.MemberId, t.Member.DisplayName, Names.First(t.Member.DisplayName), t.Absent, t.Member.Items.Any(i => i.Flags.Count > 0), t.CircleBack, null, t.Phase),
            Announcement a => new NextSpeakerDto(null, null, null, false, false, false, a.Reason, a.Phase),
            _ => new NextSpeakerDto(null, null, null, false, false, false),
        });
    }
}
