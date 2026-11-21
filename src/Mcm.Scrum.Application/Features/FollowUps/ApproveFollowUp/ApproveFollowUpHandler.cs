using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Application.Features.FollowUps.ApproveFollowUp;

/// <summary>Approves and saves; the raised <c>FollowUpApproved</c> event triggers the Azure DevOps write.</summary>
internal sealed class ApproveFollowUpHandler(IFollowUpRepository followUps, IPublisher publisher, IClock clock) : IRequestHandler<ApproveFollowUpCommand, Result>
{
    public async Task<Result> Handle(ApproveFollowUpCommand request, CancellationToken ct)
    {
        var followUp = await followUps.GetAsync(request.FollowUpId, ct);
        if (followUp is null)
        {
            return Result.Failure(FollowUpErrors.NotFound(request.FollowUpId));
        }

        var approved = followUp.Approve(request.Approver, clock.UtcNow);
        if (!approved.IsSuccess)
        {
            return approved;
        }

        await followUps.SaveAsync(followUp, ct);
        await followUp.PublishEventsAsync(publisher, ct);
        return Result.Success();
    }
}
