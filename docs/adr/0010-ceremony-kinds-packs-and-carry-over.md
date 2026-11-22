# 0010. Ceremony kinds, ceremony packs and carry-over (M9)

## Context
The PoC ran one ceremony, the daily scrum. Zava also wants planning, review and retrospective support, and the standup should remember what happened yesterday (commitments, blockers and who can help) instead of starting cold. The rules that make the demo safe must not move: no Azure DevOps write without an approved follow-up (F7), numbers come from code, ids come from the sprint.

## Decision
- **Reuse, do not fork.** A ceremony pack is a `FollowUp` that belongs to the Scrum Master (`ForScrumMaster`) and to a `CeremonyKind`. It goes through the same repository, card renderer, outbox, approval commands, `FollowUp.GetApprovedChanges` guard and `IWorkItemWriter` as a standup follow-up. No new deployable, no new writer.
- **`CeremonyKind` on the session aggregate** (`DailyScrum` default, so stored sessions load unchanged). `StandupSession` is not renamed in this milestone; the naming debt is recorded in ADR 0009.
- **Three more change kinds** (`SetIteration`, `Assign`, `CreateWorkItem`), each written only from an `ApprovedChange`. The attribution carries the approval channel (Teams or the portal) and the ceremony ("standup" for the daily scrum, keeping the existing text).
- **Packs are computed in Domain** (`PlanningPackBuilder`, `ReviewPackBuilder`, `RetroPackBuilder`) and unit tested. The model rephrases only the summary; the text is rejected if it names a work item outside the sprint (`WorkItemIdGuard`) or a number the pack did not compute (`CeremonyPack.IsGrounded`). The prompt is `agents/instructions/ceremony-pack.md`.
- **Carry-over is derived, not stored twice.** `CommitmentCarryOver` reads earlier started daily standups at prepare time; the only new stored fields are `StandupEvent.UnblockerMemberId`, `Team.RecapChannelKey` and the follow-up's `Ceremony`, `ForScrumMaster` and `ApprovedVia`, all with defaults so old data loads.
- **The unblocker is chosen in code.** The model never picks a roster member; `UnblockerResolver` matches exactly one other member or nobody.
- **A small port, `IIterationReader`,** returns the next iteration path. It is separate from `IWorkItemReader` (interface segregation) and implemented by the same Azure DevOps client.
- **Email.** `Email:Mode=Graph` selects `GraphEmailSender` (Microsoft Graph `sendMail`); `OutboxEmailSender` stays the default and the record of every email.
- **Recap routing** is a pure function (`CardRouting`) over stored conversation keys; the team's recap channel is captured when the bot is added to a channel.

## Consequences
- A pack's approval is all or nothing: the existing card approves a whole follow-up, so there are no per-item checkboxes.
- A planning pack moves carry-over only when Azure DevOps has a future iteration for the team.
- The unblock question and carry-over notes live in `VoiceGateway` and the turn prompts; they are tested at the use-case and Domain level but have not run in a live Voice Live session.
- Graph email needs `Mail.Send` for the app identity and an application access policy; it was not run against a live mailbox. `// VERIFY: https://learn.microsoft.com/graph/api/user-sendmail`.
- PROD: route recap and cards by Entra object id; persist carry-over instead of re-reading sessions; a delivery log for email.
