using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;

/// <summary>Rejects and saves.</summary>
internal sealed class RejectFollowUpHandler(IFollowUpRepository followUps, IPublisher publisher, IClock clock) : IRequestHandler<RejectFollowUpCommand, Result>
{
    public async Task<Result> Handle(RejectFollowUpCommand request, CancellationToken ct)
    {
        var followUp = await followUps.GetAsync(request.FollowUpId, ct);
        if (followUp is null)
        {
            return Result.Failure(FollowUpErrors.NotFound(request.FollowUpId));
        }

        var rejected = followUp.Reject(request.Rejecter, clock.UtcNow);
        if (!rejected.IsSuccess)
        {
            return rejected;
        }

        await followUps.SaveAsync(followUp, ct);
        await followUp.PublishEventsAsync(publisher, ct);
        return Result.Success();
    }
}
