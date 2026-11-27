using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Application.Behaviors;

namespace Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;

/// <summary>
/// Authorises, approves, saves and appends the approval record; the raised <c>FollowUpApproved</c> event then triggers the Azure DevOps
/// write. The record is appended before the event is published, so a write can never be ahead of its audit trail.
/// </summary>
internal sealed class ApproveFollowUpHandler(IFollowUpRepository followUps, ApprovalGate gate, DelegatedAccessProvider delegated, IApprovalRecordStore records, IPublisher publisher, IClock clock) : IRequestHandler<ApproveFollowUpCommand, Result>
{
    public async Task<Result> Handle(ApproveFollowUpCommand request, CancellationToken ct)
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
            ScrumTelemetry.Count("refusal", followUp.TeamId, allowed.Error!.Code);
            return allowed;
        }

        // A decision that cannot be written as the approver is refused now, so nothing is left approved but unwritable (ADR 0020).
        if (followUp.Changes.Count > 0)
        {
            var access = await delegated.AcquireAsync(decision.Actor, ct);
            if (!access.IsSuccess)
            {
                ScrumTelemetry.Count("refusal", followUp.TeamId, access.Error!.Code);
                return Result.Failure(access.Error!);
            }
        }

        var now = clock.UtcNow;
        var approved = followUp.Approve(decision.Actor, now, decision.Channel);
        if (!approved.IsSuccess)
        {
            return approved;
        }

        await followUps.SaveAsync(followUp, ct);
        await records.AppendAsync(
            new ApprovalRecord(Guid.NewGuid(), followUp.CorrelationId, followUp.Id, followUp.TeamId, ApprovalRecordKind.Approved, followUp.ComputeProposalHash(), decision.Actor, decision.Channel, now, decision.Authentication, null, []),
            ct);
        ScrumTelemetry.Count("approval", followUp.TeamId, "approved");
        hop?.SetTag("approval.outcome", "approved");
        await followUp.PublishEventsAsync(publisher, ct);
        return Result.Success();
    }
}
