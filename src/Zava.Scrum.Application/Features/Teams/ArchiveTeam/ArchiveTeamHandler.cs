using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.ArchiveTeam;

/// <summary>Sets the status to archived. Archiving twice is a no-op.</summary>
internal sealed class ArchiveTeamHandler(ITeamRepository teams) : IRequestHandler<ArchiveTeamCommand, Result>
{
    public async Task<Result> Handle(ArchiveTeamCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound(request.TeamId));
        }

        if (team.Status != TeamStatus.Archived)
        {
            await teams.SaveAsync(team with { Status = TeamStatus.Archived }, ct);
        }

        return Result.Success();
    }
}
