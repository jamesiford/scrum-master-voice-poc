using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Demo.ResetDemoAdo;

/// <summary>Removes the agent's own Azure DevOps comments so the next run starts from a clean board.</summary>
internal sealed class ResetDemoAdoHandler(ITeamRepository teams, IDemoAdoReset reset) : IRequestHandler<ResetDemoAdoCommand, Result<int>>
{
    public async Task<Result<int>> Handle(ResetDemoAdoCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result<int>.Failure(Error.NotFound("team.not_found", $"Team {request.TeamId} was not found."));
        }

        try
        {
            return Result<int>.Success(await reset.RemoveAgentCommentsAsync(team.Ado, ct));
        }
        catch (HttpRequestException)
        {
            return Result<int>.Failure(Error.ExternalRejected("ado.unreachable", "Azure DevOps could not be reached."));
        }
    }
}
