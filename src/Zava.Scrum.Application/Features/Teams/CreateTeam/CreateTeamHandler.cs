using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.CreateTeam;

/// <summary>Validates the connection, then saves a draft team with the default behaviours.</summary>
internal sealed class CreateTeamHandler(ITeamRepository teams, TeamRegistration registration) : IRequestHandler<CreateTeamCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateTeamCommand request, CancellationToken ct)
    {
        if (!AdoOrganisation.IsValidTeamId(request.TeamId))
        {
            return Result<string>.Failure(Error.Invalid("team.id_invalid", "A team id is 2 to 40 lower-case letters, digits or hyphens, starting with a letter."));
        }

        if (await teams.GetAsync(request.TeamId, ct) is not null)
        {
            return Result<string>.Failure(TeamErrors.Exists(request.TeamId));
        }

        var ado = await registration.ValidateAsync(request.Input.Ado, ct);
        if (!ado.IsSuccess)
        {
            return Result<string>.Failure(ado.Error!);
        }

        var i = request.Input;
        await teams.SaveAsync(
            new Team(request.TeamId, i.Name.Trim(), i.TimeZone, i.StandupMinutes, ado.Value, new TeamBehaviours(), i.SprintGoal,
                Status: TeamStatus.Draft, TeamsBindings: i.TeamsBindings ?? [], Ceremonies: i.Ceremonies ?? new CeremoniesEnabled(), EntraGroupId: i.EntraGroupId),
            ct);
        return Result<string>.Success(request.TeamId);
    }
}
