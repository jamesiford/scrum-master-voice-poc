using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Planning.ApprovePlan;

/// <summary>
/// Approves a sprint plan. The kept selection becomes a Scrum Master follow-up (move to the next iteration, assign suggested owners),
/// approved by the named person and handed to the approval-gated writer (F7). Returns the follow-up id.
/// </summary>
/// <param name="PackId">The pack.</param>
/// <param name="Decision">Who is approving and how that was proven. A plan belongs to the Scrum Master, so in Teams only a configured administrator can approve it.</param>
/// <param name="SelectedItemIds">The proposed items to keep; empty keeps them all. An id that is not in the proposal is refused (rule 2).</param>
public sealed record ApprovePlanCommand(Guid PackId, DecisionContext Decision, IReadOnlyList<int> SelectedItemIds) : ICommand<Guid>;
