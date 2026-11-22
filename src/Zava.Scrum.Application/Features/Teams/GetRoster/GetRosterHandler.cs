using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Members;

namespace Mcm.Scrum.Application.Features.Teams.GetRoster;

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
