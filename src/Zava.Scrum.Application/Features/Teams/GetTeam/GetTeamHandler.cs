using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.GetTeam;

/// <summary>Loads a team or reports it missing.</summary>
internal sealed class GetTeamHandler(ITeamRepository teams) : IRequestHandler<GetTeamQuery, Result<Team>>
{
    public async Task<Result<Team>> Handle(GetTeamQuery request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        return team is null ? Result<Team>.Failure(TeamErrors.NotFound(request.TeamId)) : Result<Team>.Success(team);
    }
}
