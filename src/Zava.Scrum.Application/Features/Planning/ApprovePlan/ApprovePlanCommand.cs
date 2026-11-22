using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.Planning.ApprovePlan;

/// <summary>
/// Approves a sprint plan. The kept selection becomes a Scrum Master follow-up (move to the next iteration, assign suggested owners),
/// approved by the named person and handed to the approval-gated writer (F7). Returns the follow-up id.
/// </summary>
/// <param name="PackId">The pack.</param>
/// <param name="Approver">Display name of the person approving.</param>
/// <param name="SelectedItemIds">The proposed items to keep; empty keeps them all. An id that is not in the proposal is refused (rule 2).</param>
/// <param name="Channel">Where they approved; written into the Azure DevOps attribution.</param>
public sealed record ApprovePlanCommand(Guid PackId, string Approver, IReadOnlyList<int> SelectedItemIds, ApprovalChannel Channel = ApprovalChannel.Teams) : ICommand<Guid>;
