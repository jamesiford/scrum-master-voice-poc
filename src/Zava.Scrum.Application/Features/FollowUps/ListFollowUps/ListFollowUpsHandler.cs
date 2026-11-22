using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Application.Features.FollowUps.ListFollowUps;

/// <summary>Reads follow-ups.</summary>
internal sealed class ListFollowUpsHandler(IFollowUpRepository followUps) : IRequestHandler<ListFollowUpsQuery, Result<IReadOnlyList<FollowUpSnapshot>>>
{
    public async Task<Result<IReadOnlyList<FollowUpSnapshot>>> Handle(ListFollowUpsQuery request, CancellationToken ct)
    {
        var list = await followUps.ListAsync(request.TeamId, request.Status, ct);
        return Result<IReadOnlyList<FollowUpSnapshot>>.Success(list.Select(f => f.ToSnapshot()).ToList());
    }
}
