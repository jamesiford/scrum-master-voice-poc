using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Teams.UpdateMemberAvailability;

/// <summary>Replaces the member's availability in the stored roster.</summary>
internal sealed class UpdateMemberAvailabilityHandler(ITeamRepository teams, IMemberRepository members) : IRequestHandler<UpdateMemberAvailabilityCommand, Result>
{
    public async Task<Result> Handle(UpdateMemberAvailabilityCommand request, CancellationToken ct)
    {
        if (await teams.GetAsync(request.TeamId, ct) is null)
        {
            return Result.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var roster = await members.ListAsync(request.TeamId, ct);
        if (roster.All(m => m.Id != request.MemberId))
        {
            return Result.Failure(Error.NotFound("member.not_found", $"Member '{request.MemberId}' is not on the roster."));
        }

        await members.ReplaceAsync(request.TeamId, [.. roster.Select(m => m.Id == request.MemberId ? m with { AvailabilityPct = request.Percent } : m)], ct);
        return Result.Success();
    }
}
