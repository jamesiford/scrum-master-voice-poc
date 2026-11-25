using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Planning;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Planning.ApprovePlan;

/// <summary>
/// Turns the approved selection into a Scrum Master follow-up and approves it, so the write goes through the same guard as every other
/// change: <c>FollowUpApproved</c> triggers <c>ApplyApprovedChangesCommand</c>, which reads <see cref="FollowUp.GetApprovedChanges"/>.
/// </summary>
internal sealed class ApprovePlanHandler(
    IPlanningPackRepository packs,
    IMemberRepository members,
    IFollowUpRepository followUps,
    IApprovalRecordStore records,
    ApprovalGate gate,
    ITeamsMessenger messenger,
    IPublisher publisher,
    IClock clock) : IRequestHandler<ApprovePlanCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ApprovePlanCommand request, CancellationToken ct)
    {
        var pack = await packs.GetAsync(request.PackId, ct);
        if (pack is null)
        {
            return Result<Guid>.Failure(PlanningErrors.NotFound(request.PackId));
        }

        var decision = request.Decision;
        var allowed = await gate.AuthoriseAsync(decision, pack.Data.TeamId, null, forScrumMaster: true, ct);
        if (!allowed.IsSuccess)
        {
            return Result<Guid>.Failure(allowed.Error!);
        }

        var now = clock.UtcNow;
        var roster = await members.ListAsync(pack.Data.TeamId, ct);
        var changes = pack.Approve(decision.Actor.DisplayName, request.SelectedItemIds, roster, now);
        if (!changes.IsSuccess)
        {
            return Result<Guid>.Failure(changes.Error!);
        }

        var followUp = FollowUp.Create(
            Guid.NewGuid(), pack.Data.TeamId, pack.Id, null, FollowUpKind.PlanningPack, $"Sprint plan for {pack.Data.NextIterationPath}. Draft goal: {pack.Data.DraftGoal}",
            changes.Value, DateOnly.FromDateTime(now.UtcDateTime), now, CeremonyKind.SprintPlanning, forScrumMaster: true);
        followUp.MarkSent();
        var approved = followUp.Approve(decision.Actor, now, decision.Channel);
        if (!approved.IsSuccess)
        {
            return Result<Guid>.Failure(approved.Error!);
        }

        await followUps.SaveAsync(followUp, ct);
        await records.AppendAsync(
            new ApprovalRecord(Guid.NewGuid(), followUp.CorrelationId, followUp.Id, followUp.TeamId, ApprovalRecordKind.Approved, followUp.ComputeProposalHash(), decision.Actor, decision.Channel, now, decision.Authentication, null, []),
            ct);
        pack.AttachFollowUp(followUp.Id);
        await packs.SaveAsync(pack, ct);
        await followUp.PublishEventsAsync(publisher, ct);

        var written = await followUps.GetAsync(followUp.Id, ct) ?? followUp;
        await messenger.UpdateAsync(PlanningCards.Result(pack.Data, Describe(written)), ct);
        return Result<Guid>.Success(followUp.Id);
    }

    private static string Describe(FollowUp f) => f.Status == FollowUpStatus.Applied
        ? $"Approved by {f.ApprovedBy} and written to Azure DevOps."
        : f.ApplyError is null ? $"Approved by {f.ApprovedBy}. Writing to Azure DevOps." : $"Approved by {f.ApprovedBy}. Azure DevOps write failed: {f.ApplyError}";
}
