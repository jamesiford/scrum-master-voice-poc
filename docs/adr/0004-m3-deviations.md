# 0004. M3 decisions: domain event dispatch, outbox messenger, folded escalation

## Context
M3 adds follow-ups, Teams cards and approval-gated Azure DevOps writes. ADR 0001 left open how domain events raised on aggregates reach handlers. The Teams bot needs a customer bot registration, a dev tunnel and a Teams sideload that this environment does not have.

## Decision
- **Event dispatch:** handlers call `aggregate.PublishEventsAsync(publisher, ct)` after the repository save. It publishes then clears the events. This is used by `PrepareStandup`, `EndStandup` and `ApproveFollowUp`. `StandupEnded` is raised only on the first transition, so ending twice does not duplicate cards.
- **Approval guard (F7):** `FollowUp.GetApprovedChanges()` is the only way to obtain an `ApprovedChange`, and `IWorkItemWriter` accepts only that type. An unapproved follow-up cannot reach the writer, in addition to the handler tests.
- **Teams delivery:** `ITeamsMessenger` is implemented by `OutboxTeamsMessenger`, which renders Adaptive Cards from `Teams/Cards/*.json` and stores them in `cards.json`; the portal shows them. The Microsoft 365 Agents SDK bot (`/api/messages`), proactive messaging and card actions from Teams are **not built** (// PROD: a new adapter behind the same port).
- **Escalation:** `EscalateBlockerCommand` is folded into `SendFollowUpsCommand`. `FollowUpPlanner` plans follow-up, absent and escalation follow-ups in one pass, so there is no `BlockerEscalated` event.
- **Follow-up content:** the planner is pure code. It turns recorded commitment and blocker events into comment changes; no model decides what is written.

## Consequences
- Approving in the portal drives the same command a Teams card action will use later.
- `UpdateFollowUpCardHandler` is deferred until the bot exists.
- Events are not persisted or retried; a crash between save and publish loses the side effect. // PROD: outbox.
