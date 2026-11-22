using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.ListFollowUps;

/// <summary>Reads follow-ups.</summary>
internal sealed class ListFollowUpsHandler(IFollowUpRepository followUps) : IRequestHandler<ListFollowUpsQuery, Result<IReadOnlyList<FollowUpSnapshot>>>
{
    public async Task<Result<IReadOnlyList<FollowUpSnapshot>>> Handle(ListFollowUpsQuery request, CancellationToken ct)
    {
        var list = await followUps.ListAsync(request.TeamId, request.Status, ct);
        return Result<IReadOnlyList<FollowUpSnapshot>>.Success(list.Select(f => f.ToSnapshot()).ToList());
    }
}
