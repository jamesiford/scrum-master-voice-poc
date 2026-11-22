using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Teams.SetRecapChannel;

/// <summary>Stores the recap channel on the team.</summary>
internal sealed class SetRecapChannelHandler(ITeamRepository teams) : IRequestHandler<SetRecapChannelCommand, Result>
{
    public async Task<Result> Handle(SetRecapChannelCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound(request.TeamId));
        }

        await teams.SaveAsync(team with { RecapChannelKey = request.ConversationId }, ct);
        return Result.Success();
    }
}
