# 0011. Sprint planning pack (M9a)

## Context
M9 built ceremony packs as one follow-up that carries its changes, created by `CreateCeremonyPackCommand`, with Approve all and Reject on the card. Sprint planning needs more: a capacity model, a selection the Scrum Master can edit before approving, reasons for what was left out, a draft goal, and a record of the proposal separate from what was finally written. The approval rules must not change: no Azure DevOps write without an approved follow-up (F7), numbers come from code, ids come from Azure DevOps.

## Decision
- **A `PlanningPack` aggregate** (Domain, stored in `planning-packs.json`) holds the proposal, the draft goal and the decision. It is not a follow-up: the proposal can be approved in part, and a rejected or ignored plan must leave no follow-up behind. `PlanningPrepared` is raised on creation and a handler sends the card.
- **Approval creates the follow-up.** `ApprovePlanCommand` asks the pack for the changes of the kept items (`SetIteration` for each, `Assign` where the owner was a suggestion), creates a Scrum Master follow-up (`PlanningPack` kind, ceremony `SprintPlanning`), approves it with the approver's name and channel, and publishes `FollowUpApproved`. `ApplyApprovedChangesCommand` then writes through `FollowUp.GetApprovedChanges` and `IWorkItemWriter`, unchanged. The follow-up never gets its own card; the planning card is replaced with the outcome.
- **The planner is pure Domain** (`PlanningPlanner.Propose`): velocity range from the last six sprints; each active member's equal share times availability; carry-over first; ready backlog items in order, stopping at the first that does not fit; not-ready items skipped with reasons; the same least-loaded rule for owners as the advice page; over- and under-commitment flags. `PlanningProposal` is data, serialisable and unit tested.
- **The model only phrases the goal.** `agents/instructions/planning-goal.md`; the text is dropped for `PlanningGoal.Fallback` when it names an unknown work item (`WorkItemIdGuard`) or a number the proposal does not contain (`PlanningGoal.IsGrounded`).
- **The existing planning ceremony pack route is removed.** `CreateCeremonyPackCommand` now refuses `SprintPlanning`; `PlanningPackBuilder` is deleted. Review and retrospective packs are unchanged.
- **Ports.** `IBacklogReader` (backlog outside the sprint, in backlog order) and `IPlanningPackRepository`, both small and role-specific. The Azure DevOps client implements `IBacklogReader`; `IIterationReader` now lists the team's iterations and takes the earliest one that has not started (the endpoint's `$timeframe` filter only supports `current`).
- **Availability** is `Member.AvailabilityPct` (default 100), edited through `UpdateMemberAvailabilityCommand` and kept by roster sync.
- **Scrum Master chat command.** "prepare sprint planning" runs `PreparePlanningCommand` only for display names in `Team:ScrumMasters`. `// PROD: an Entra group or app role.`
- **DEMO owners are tags.** The sandbox has one real user, so with `Demo:HonorOwnerTags` an `Assign` change replaces the `demo-owner-{key}` tag instead of setting `System.AssignedTo` (`ProposedChange.OwnerKey` carries the roster key). `// PROD: AssignedTo only.`

## Consequences
- The kept selection is chosen by the approver, but the pack only accepts ids it proposed; an id from anywhere else is refused.
- Per-member capacity warns and does not constrain: a plan can load one person above their capacity and says so.
- A plan moves stories and bugs, not their child tasks. Capacity ignores calendars, and the velocity history is the illustrative fixture until real sprints exist.
- A team with no future iteration can prepare a pack but not approve it.
- Checked live against the sandbox (reject writes nothing; approve moves the items to Sprint 43 with owners and an "Approved by" comment). The Teams card actions and the chat command are not checked in Teams; the commands behind them are unit tested.
