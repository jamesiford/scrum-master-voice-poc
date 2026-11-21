using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Application.Features.FollowUps.GetFollowUp;

/// <summary>Reads one follow-up.</summary>
internal sealed class GetFollowUpHandler(IFollowUpRepository followUps) : IRequestHandler<GetFollowUpQuery, Result<FollowUpSnapshot>>
{
    public async Task<Result<FollowUpSnapshot>> Handle(GetFollowUpQuery request, CancellationToken ct)
    {
        var f = await followUps.GetAsync(request.FollowUpId, ct);
        return f is null ? Result<FollowUpSnapshot>.Failure(FollowUpErrors.NotFound(request.FollowUpId)) : Result<FollowUpSnapshot>.Success(f.ToSnapshot());
    }
}
