using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.GetFollowUp;

/// <summary>Reads one follow-up.</summary>
internal sealed class GetFollowUpHandler(IFollowUpRepository followUps) : IRequestHandler<GetFollowUpQuery, Result<FollowUpSnapshot>>
{
    public async Task<Result<FollowUpSnapshot>> Handle(GetFollowUpQuery request, CancellationToken ct)
    {
        var f = await followUps.GetAsync(request.FollowUpId, ct);
        return f is null ? Result<FollowUpSnapshot>.Failure(FollowUpErrors.NotFound(request.FollowUpId)) : Result<FollowUpSnapshot>.Success(f.ToReadModel());
    }
}
