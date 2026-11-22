using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Domain.Planning;

/// <summary>
/// A prepared sprint plan (CEREMONIES C2). It holds the proposal and decides which selection a person may approve; the approved
/// selection becomes a Scrum Master follow-up, so the writes still go through <see cref="FollowUp.GetApprovedChanges"/> (F7).
/// </summary>
public sealed class PlanningPack : AggregateRoot
{
    private PlanningPack(PlanningPackData data) => Data = data;

    /// <summary>Gets the persisted state.</summary>
    public PlanningPackData Data { get; private set; }

    /// <summary>Gets the pack id.</summary>
    public Guid Id => Data.Id;

    /// <summary>Prepares a pack and raises <see cref="PlanningPrepared"/>.</summary>
    /// <param name="id">Pack id.</param>
    /// <param name="teamId">Team id.</param>
    /// <param name="now">Current time.</param>
    /// <param name="iterationPath">The current iteration.</param>
    /// <param name="nextIterationPath">The next iteration, if Azure DevOps has one.</param>
    /// <param name="velocityIllustrative">Whether the velocity history is the illustrative fixture.</param>
    /// <param name="proposal">The proposal.</param>
    /// <param name="draftGoal">The draft goal.</param>
    /// <param name="goalFromModel">Whether a model phrased the goal.</param>
    /// <returns>The pack.</returns>
    public static PlanningPack Prepare(Guid id, string teamId, DateTimeOffset now, string iterationPath, string? nextIterationPath, bool velocityIllustrative, PlanningProposal proposal, string draftGoal, bool goalFromModel)
    {
        var pack = new PlanningPack(new PlanningPackData(id, teamId, now, iterationPath, nextIterationPath, velocityIllustrative, proposal, draftGoal, goalFromModel, PlanningPackStatus.Prepared, null, null, [], null));
        pack.Raise(new PlanningPrepared(id, now));
        return pack;
    }

    /// <summary>Rehydrates a persisted pack.</summary>
    /// <param name="data">Persisted state.</param>
    /// <returns>The pack.</returns>
    public static PlanningPack Restore(PlanningPackData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new PlanningPack(data);
    }

    /// <summary>
    /// Accepts an approver's selection and returns the proposed changes. Only items that are in the proposal can be selected, so an id from
    /// anywhere else is refused (rule 2). The caller turns the changes into a follow-up and approves it; nothing is written here.
    /// </summary>
    /// <param name="approver">Who approves (display name).</param>
    /// <param name="selectedIds">The proposed items the approver keeps; empty means all of them.</param>
    /// <param name="roster">The roster, to resolve suggested owners.</param>
    /// <param name="now">Current time.</param>
    /// <returns>The changes, or a validation or conflict error.</returns>
    public Result<IReadOnlyList<ProposedChange>> Approve(string approver, IReadOnlyList<int> selectedIds, IReadOnlyList<Member> roster, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(selectedIds);
        ArgumentNullException.ThrowIfNull(roster);
        if (string.IsNullOrWhiteSpace(approver))
        {
            return Result<IReadOnlyList<ProposedChange>>.Failure(PlanningErrors.ApproverRequired);
        }

        if (Data.Status != PlanningPackStatus.Prepared)
        {
            return Result<IReadOnlyList<ProposedChange>>.Failure(PlanningErrors.AlreadyDecided(Data.Status));
        }

        if (Data.NextIterationPath is null)
        {
            return Result<IReadOnlyList<ProposedChange>>.Failure(PlanningErrors.NoNextIteration);
        }

        var wanted = selectedIds.Count == 0 ? Data.Proposal.Selected.Select(s => s.WorkItemId).ToList() : selectedIds.Distinct().ToList();
        var known = Data.Proposal.Selected.ToDictionary(s => s.WorkItemId);
        foreach (var id in wanted.Where(id => !known.ContainsKey(id)))
        {
            return Result<IReadOnlyList<ProposedChange>>.Failure(PlanningErrors.UnknownItem(id));
        }

        if (wanted.Count == 0)
        {
            return Result<IReadOnlyList<ProposedChange>>.Failure(PlanningErrors.NothingSelected);
        }

        var chosen = Data.Proposal.Selected.Where(s => wanted.Contains(s.WorkItemId)).ToList();
        Data = Data with { Status = PlanningPackStatus.Approved, DecidedBy = approver.Trim(), DecidedAt = now, ApprovedItemIds = [.. chosen.Select(c => c.WorkItemId)] };
        return Result<IReadOnlyList<ProposedChange>>.Success(ChangesFor(chosen, roster));
    }

    /// <summary>Records the follow-up that carries the approved changes.</summary>
    /// <param name="followUpId">The follow-up.</param>
    public void AttachFollowUp(Guid followUpId) => Data = Data with { FollowUpId = followUpId };

    /// <summary>Rejects the plan. Nothing is written.</summary>
    /// <param name="rejecter">Who rejects (display name).</param>
    /// <param name="now">Current time.</param>
    /// <returns>Success, or a validation or conflict error.</returns>
    public Result Reject(string rejecter, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(rejecter))
        {
            return Result.Failure(PlanningErrors.ApproverRequired);
        }

        if (Data.Status != PlanningPackStatus.Prepared)
        {
            return Result.Failure(PlanningErrors.AlreadyDecided(Data.Status));
        }

        Data = Data with { Status = PlanningPackStatus.Rejected, DecidedBy = rejecter.Trim(), DecidedAt = now };
        return Result.Success();
    }

    // Every chosen item moves to the next iteration; an item that had no owner also gets the suggested one.
    private List<ProposedChange> ChangesFor(List<PlannedItem> chosen, IReadOnlyList<Member> roster)
    {
        var changes = new List<ProposedChange>();
        foreach (var item in chosen)
        {
            changes.Add(ProposedChange.SetIteration(item.WorkItemId, Data.NextIterationPath!, item.Source == PlanningSource.CarryOver ? "Carry-over: started and not finished." : "Ready and within capacity."));
            if (item.OwnerSuggested && roster.FirstOrDefault(m => m.Id == item.OwnerMemberId) is { } owner)
            {
                changes.Add(ProposedChange.Assign(item.WorkItemId, owner.AdoUniqueName ?? owner.DisplayName, $"{owner.DisplayName} has the most room in the plan.", owner.Key));
            }
        }

        return changes;
    }
}
