using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.FollowUps.ApplyApprovedChanges;

/// <summary>
/// The single place Azure DevOps is written. The approval guard is <see cref="FollowUp.GetApprovedChanges"/>: it is the
/// only source of <see cref="ApprovedChange"/>, so the writer cannot be reached for an unapproved follow-up (F7). It also refuses
/// unless the approval record exists and its proposal hash matches the follow-up, so no write is ever ahead of or different from
/// its audit trail (ADR 0017); afterwards it appends the record of what was written.
/// </summary>
internal sealed class ApplyApprovedChangesHandler(IFollowUpRepository followUps, ITeamRepository teams, IWorkItemScopeReader scope, IWorkItemWriter writer, DelegatedAccessProvider delegated, IApprovalRecordStore records, IPublisher publisher, IClock clock) : IRequestHandler<ApplyApprovedChangesCommand, Result>
{
    public async Task<Result> Handle(ApplyApprovedChangesCommand request, CancellationToken ct)
    {
        var followUp = await followUps.GetAsync(request.FollowUpId, ct);
        if (followUp is null)
        {
            return Result.Failure(FollowUpErrors.NotFound(request.FollowUpId));
        }

        if (followUp.Status == FollowUpStatus.Applied)
        {
            return Result.Success();
        }

        var changes = followUp.GetApprovedChanges();
        if (!changes.IsSuccess)
        {
            return Result.Failure(changes.Error!);
        }

        var trail = await records.ListByFollowUpAsync(followUp.Id, ct);
        var decision = trail.LastOrDefault(r => r.Kind == ApprovalRecordKind.Approved);
        if (decision is null || decision.ProposalHash != followUp.ComputeProposalHash())
        {
            return Result.Failure(FollowUpErrors.ApprovalRecordMissing);
        }

        // The write is made as the approver, with their own token, and never with the service identity (ADR 0020). Refusing here leaves
        // the follow-up approved and unapplied, so the approver can retry after signing in.
        var access = await delegated.AcquireAsync(decision.Actor, ct);
        if (!access.IsSuccess)
        {
            followUp.MarkApplyFailed(access.Error!.Message);
            await followUps.SaveAsync(followUp, ct);
            return Result.Failure(access.Error);
        }

        var context = new DelegatedWriteContext(decision.Actor, _ => Task.FromResult(access));
        var team = await teams.GetAsync(followUp.TeamId, ct);
        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound(followUp.TeamId));
        }

        // Every change must be inside this team's project and area path before any is written; a write aimed at another team's item is refused
        // even though it was approved (ADR 0018).
        var inScope = await CheckScopeAsync(team.Ado, changes.Value, ct);
        if (!inScope.IsSuccess)
        {
            followUp.MarkApplyFailed(inScope.Error!.Message);
            await followUps.SaveAsync(followUp, ct);
            return inScope;
        }

        var revisions = new List<AdoRevision>();
        foreach (var change in changes.Value)
        {
            var written = await writer.ApplyAsync(change, team.Ado, context, ct);
            if (!written.IsSuccess)
            {
                followUp.MarkApplyFailed(written.Error!.Message);
                await followUps.SaveAsync(followUp, ct);
                return Result.Failure(written.Error);
            }

            if (written.Value.Written)
            {
                revisions.Add(written.Value.ToRecord());
            }
        }

        var now = clock.UtcNow;
        followUp.MarkApplied(now);
        await followUps.SaveAsync(followUp, ct);
        await records.AppendAsync(
            new ApprovalRecord(Guid.NewGuid(), followUp.CorrelationId, followUp.Id, followUp.TeamId, ApprovalRecordKind.Applied, decision.ProposalHash, decision.Actor, decision.Channel, now, decision.Authentication, decision.Id, revisions),
            ct);
        await followUp.PublishEventsAsync(publisher, ct);
        return Result.Success();
    }

    private async Task<Result> CheckScopeAsync(AdoSettings ado, IReadOnlyList<ApprovedChange> changes, CancellationToken ct)
    {
        foreach (var change in changes)
        {
            if (change.Op == ChangeOp.CreateWorkItem)
            {
                if (change.New is null || !WorkItemScope.ContainsNew(ado, change.New.IterationPath))
                {
                    return Result.Failure(WorkItemScope.OutOfScope(change.WorkItemId));
                }

                continue;
            }

            var location = await scope.GetLocationAsync(ado, change.WorkItemId, ct);
            if (!location.IsSuccess)
            {
                return Result.Failure(location.Error!);
            }

            if (!WorkItemScope.Contains(ado, location.Value.Project, location.Value.AreaPath))
            {
                return Result.Failure(WorkItemScope.OutOfScope(change.WorkItemId));
            }
        }

        return Result.Success();
    }
}
