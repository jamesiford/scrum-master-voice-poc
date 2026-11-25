using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.UpdateTeamSettings;

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
                // The Azure DevOps connection is registry data: only an Admin changes it (UpdateTeamCommand), so a team lead cannot repoint their team at another team's project or area path (ADR 0018).
                Behaviours = team.Behaviours with { StaleDays = request.StaleDays, EscalateBlockerAfterDays = request.EscalateBlockerAfterDays },
                SprintGoal = request.SprintGoal,
            },
            ct);
        return Result.Success();
    }
}
