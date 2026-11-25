using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.UpdateTeam;

/// <summary>Applies the edit. A changed organisation is checked for the tenant again; archiving is a separate command.</summary>
internal sealed class UpdateTeamHandler(ITeamRepository teams, TeamRegistration registration) : IRequestHandler<UpdateTeamCommand, Result>
{
    public async Task<Result> Handle(UpdateTeamCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound(request.TeamId));
        }

        if (!team.IsOpen || request.Input.Status == TeamStatus.Archived)
        {
            return Result.Failure(team.IsOpen ? Error.Invalid("team.use_archive", "Archive a team with DELETE /api/teams/{id}.") : TeamErrors.Archived(team.Id));
        }

        var i = request.Input;
        var ado = i.Ado == team.Ado ? Result<AdoSettings>.Success(team.Ado) : await registration.ValidateAsync(i.Ado, ct);
        if (!ado.IsSuccess)
        {
            return Result.Failure(ado.Error!);
        }

        await teams.SaveAsync(
            team with
            {
                Name = i.Name.Trim(),
                TimeZone = i.TimeZone,
                StandupMinutes = i.StandupMinutes,
                Ado = ado.Value,
                SprintGoal = i.SprintGoal,
                Ceremonies = i.Ceremonies ?? team.Ceremonies,
                Status = i.Status ?? team.Status,
                TeamsBindings = i.TeamsBindings ?? team.TeamsBindings,
                EntraGroupId = i.EntraGroupId ?? team.EntraGroupId,
            },
            ct);
        return Result.Success();
    }
}
