using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Teams;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Planning;

namespace Mcm.Scrum.Application.Features.Planning.GetPlanningPrep;

/// <summary>Reads the sprint and roster and applies <see cref="PlanningPlanner"/>.</summary>
internal sealed class GetPlanningPrepHandler(ITeamRepository teams, IMemberRepository members, IWorkItemReader reader) : IRequestHandler<GetPlanningPrepQuery, Result<PlanningPrep>>
{
    public async Task<Result<PlanningPrep>> Handle(GetPlanningPrepQuery request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result<PlanningPrep>.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var sprint = await reader.GetCurrentSprintAsync(team, ct);
        return sprint.IsSuccess
            ? Result<PlanningPrep>.Success(PlanningPlanner.Plan(sprint.Value, await members.ListAsync(team.Id, ct)))
            : Result<PlanningPrep>.Failure(sprint.Error!);
    }
}
