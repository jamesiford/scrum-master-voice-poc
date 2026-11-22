using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Application.Features.Teams.GetRoster;

/// <summary>Lists the roster after checking the team exists.</summary>
internal sealed class GetRosterHandler(ITeamRepository teams, IMemberRepository members)
    : IRequestHandler<GetRosterQuery, Result<IReadOnlyList<Member>>>
{
    public async Task<Result<IReadOnlyList<Member>>> Handle(GetRosterQuery request, CancellationToken ct)
    {
        if (await teams.GetAsync(request.TeamId, ct) is null)
        {
            return Result<IReadOnlyList<Member>>.Failure(TeamErrors.NotFound(request.TeamId));
        }

        return Result<IReadOnlyList<Member>>.Success(await members.ListAsync(request.TeamId, ct));
    }
}
