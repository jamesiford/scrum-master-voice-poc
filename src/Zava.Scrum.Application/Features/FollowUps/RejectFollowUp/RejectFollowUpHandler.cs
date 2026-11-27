using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Application.Behaviors;

namespace Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;

/// <summary>Authorises, rejects, saves and appends the rejection record.</summary>
internal sealed class RejectFollowUpHandler(IFollowUpRepository followUps, ApprovalGate gate, IApprovalRecordStore records, IPublisher publisher, IClock clock) : IRequestHandler<RejectFollowUpCommand, Result>
{
    public async Task<Result> Handle(RejectFollowUpCommand request, CancellationToken ct)
    {
        var followUp = await followUps.GetAsync(request.FollowUpId, ct);
        if (followUp is null)
        {
            return Result.Failure(FollowUpErrors.NotFound(request.FollowUpId));
        }

        var decision = request.Decision;
        using var hop = ScrumTelemetry.StartHop("approval.decision", followUp.TeamId, followUp.SessionId, followUp.CorrelationId);
        var allowed = await gate.AuthoriseAsync(decision, followUp.TeamId, followUp.MemberId, followUp.ForScrumMaster, ct);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        var now = clock.UtcNow;
        var rejected = followUp.Reject(decision.Actor, now);
        if (!rejected.IsSuccess)
        {
            return rejected;
        }

        await followUps.SaveAsync(followUp, ct);
        await records.AppendAsync(
            new ApprovalRecord(Guid.NewGuid(), followUp.CorrelationId, followUp.Id, followUp.TeamId, ApprovalRecordKind.Rejected, followUp.ComputeProposalHash(), decision.Actor, decision.Channel, now, decision.Authentication, null, []),
            ct);
        ScrumTelemetry.Count("approval", followUp.TeamId, "rejected");
        hop?.SetTag("approval.outcome", "rejected");
        await followUp.PublishEventsAsync(publisher, ct);
        return Result.Success();
    }
}
