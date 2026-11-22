using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Teams;

namespace Mcm.Scrum.Application.Features.Teams.UpdateTeamSettings;

/// <summary>Applies edited settings to the stored team. The Azure DevOps org stays as configured.</summary>
internal sealed class UpdateTeamSettingsHandler(ITeamRepository teams) : IRequestHandler<UpdateTeamSettingsCommand, Result>
{
    public async Task<Result> Handle(UpdateTeamSettingsCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound(request.TeamId));
        }

        await teams.SaveAsync(
            team with
            {
                Name = request.Name,
                TimeZone = request.TimeZone,
                StandupMinutes = request.StandupMinutes,
                Ado = team.Ado with { Project = request.Project, Team = request.AdoTeam, AreaPath = request.AreaPath },
                Behaviours = team.Behaviours with { StaleDays = request.StaleDays, EscalateBlockerAfterDays = request.EscalateBlockerAfterDays },
                SprintGoal = request.SprintGoal,
            },
            ct);
        return Result.Success();
    }
}
