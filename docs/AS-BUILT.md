# Zava Scrum Master voice PoC: as built

This document describes what the proof of concept actually does today. It is the companion to the build spec (`SPEC.md`, what was planned) and the ADRs (`adr/`, where the build deviated and why). Where they differ, this document and the ADRs are right.

## 1. What it is
A voice-led "junior Scrum Master" for the Payments team. It joins a real Microsoft Teams meeting, runs the daily standup by voice, grounds every question in Azure DevOps, shares a live board on screen, posts a recap and per-person follow-ups, and writes changes back to Azure DevOps only after a person approves them.

## 2. Architecture
```
Teams meeting <-> ACS calling SDK (headless Chromium on the bridge) <-> API voice gateway <-> Azure Voice Live
                                                                     |-> Application (hand-rolled mediator, commands/queries)
                                                                     |-> Domain (flag rules, floor control, classifiers, KPIs)
                                                                     |-> Infrastructure: Azure DevOps REST, Foundry agents, Cosmos, Bot
Admin portal (React, Tailwind) -> API (served by the same container)
```
- **Solution:** Clean Architecture, four projects (`Domain`, `Application`, `Infrastructure`, `Api`), enforced by architecture tests. Hand-rolled mediator with logging, validation and exception-mapping behaviours. No MediatR.
- **Bridge (`src/meeting-bridge`):** Node 22 + TypeScript. Serves the Simulated Room and the Teams call page, launches Chromium, and holds the ACS identity. Fallback for any live demo: the Simulated Room (scripted cast).
- **Portal (`src/admin-portal`):** React 19, Vite, Tailwind CSS v4, lucide icons, hash routing, Entra sign-in (Easy Auth).
- **Azure (West US 3, `azd up`):** two Container Apps (`api` serves the portal too; `bridge`), ACR, user-assigned managed identity, Cosmos DB (Entra-only), Azure Communication Services, Azure Bot (Teams channel), Foundry (chat and Voice Live), Entra apps for the bot and the portal.
- **Audio:** PCM16, 24 kHz, mono. The bridge feeds meeting audio to the gateway and plays agent audio into the call through a virtual microphone. Chromium runs with `--use-fake-device-for-media-stream`; without a microphone device the ACS SDK silently drops custom audio (azure-sdk-for-js#29174).

## 3. The standup, as it actually runs
1. **Prepare:** facts are computed in code from Azure DevOps: sprint, burndown, per-person open items and flags (stale, blocked, overdue, unassigned, no recent update). A model only phrases a summary and questions; ids outside the sprint are dropped.
2. **Join:** the agent joins the Teams meeting, waits in the lobby if needed, shares the standup board as its screen, and begins 8 seconds after the first roster member is matched (the portal's **Begin** button starts it sooner). Members are matched by any word of their Teams name; anyone not present is marked absent.
3. **Floor control:** a pure Domain state machine owns who speaks. The agent only speaks when it is allowed to, and stops when a human barges in.
4. **Each turn:** call on a member, listen, then reconcile what they said against their items. Item links come from code (`ItemMentions`: "story 5", "task fourteen", or a distinctive title word), never from a model guess. The agent briefly says what it noted (blockers, commitments) and asks about the most urgent flagged item not yet covered (blocked, then overdue, then at risk, then stale, then no recent update), up to three questions per person.
5. **Questions from the team:** "is that one overdue, what was the due date?" is answered from live Azure DevOps through the `get_work_item_details` tool, with the item being discussed supplied as context so "that one" resolves in code.
6. **Recap:** blockers, parked topics, absent members, members who were present but silent, and flagged items nobody covered. Follow-ups are then sent.

### 3.1 Reactive, goal-driven behaviour (ADR 0008)
The goal is fixed (everything covered, nobody forgotten); the room changes the path.
| Situation | What the agent does |
|---|---|
| A teammate starts their own update out of turn | Says once, politely, whose turn it is and that they will be asked in turn |
| Someone says a member stepped away (or the member says so) | Acknowledges, skips them (now or in advance) and circles back at the end |
| A called member is silent | Records "in the meeting but did not respond" (not absent), moves on, asks once more at the end; silent twice is final and their follow-up says they were present |
| "What am I supposed to report on?" | Lists their most urgent items first (blocked, overdue, stale), top three, then asks about blockers |
| A member corrects themselves ("scratch that, I meant...") | The earlier statement is retracted (kept in the audit trail, excluded from follow-ups, recap and reports) and the new one recorded |
| One member gives another's update | Attributes it to the owner; anything captured still needs the owner's approval |
| Side conversation | Waits, then parks the topic after a threshold |
| Time warning / timebox | Warns at 80 percent; recaps and ends at the limit |
Classification is rule based (`UtteranceClassifier`), so it adds no latency and is table-tested. In Teams the mixed audio has no speaker, so the speaker is the member whose live "speaking" event overlaps the speech, else the floor holder.

### 3.2 Shared board
The agent shares a 1280x720 canvas as its screen: the agenda of flagged items at the start, then the current speaker's items (state, flags, owner, a highlight on the item being asked about, a tick when covered) and per-member coverage with a status (away, no response, update by someone). Frames are forced from a canvas attached to the page. Sharing is best effort and supervised: it retries until Teams confirms, and every status change appears as a Bridge event on the Live tab and in the bridge log (`[share]`). **Status:** live behaviour was still being validated at the time of writing.

## 4. Safety rules that are enforced in code
1. No Azure DevOps write without an approved follow-up (`FollowUp.GetApprovedChanges`, `ApplyApprovedChangesHandler`, unit tested). Approval is a card button in Teams or the portal.
2. Work item ids are never invented: ids in recorded events, follow-ups, cards, generated questions, summaries and the board are checked against the sprint in code (`WorkItemIdGuard`, `RecordStandupEventHandler`). The facilitator model's free speech is constrained by its prompt and checked only by the end-to-end test "never mentions a work item id outside the sprint".
3. Numbers and flags come from code (flag rules, burndown, KPIs, coverage, priorities); models only phrase.
4. No secrets in the repo; `DefaultAzureCredential`, user secrets locally, Container Apps secrets in Azure.
5. Prompts live in `agents/instructions/*.md`; edits made in the portal are stored in Cosmos and survive restarts.

## 5. Features by area
- **Standups:** prepare, start (Simulated Room or Teams), live console (floor, speaker, timebox, parking lot, latency p50/p95, Bridge events), control (begin, mute, unmute, skip, take over, hand back, end), scheduler (prepare T-10 min, start T-1 min), demo reset.
- **Follow-ups:** per-member follow-up with proposed Azure DevOps comments, escalation for long-blocked items, "missed standup" and "no response" messages. Adaptive cards in Teams with Approve and Reject (there is no Edit action); the card updates with the outcome. The planner proposes **comments only**; State and Tag changes exist in the model and the writer but are reachable only through `POST /api/followups/propose`. Pre-brief, follow-up, absent and escalation cards are sent to **every stored conversation** (everyone who has messaged or added the bot), not to the meeting chat, a channel or the Scrum Master specifically; the recap goes to the team's recap channel and the meeting chat, with every stored conversation as the fallback (M9, section 11). Outbox view in the portal when Teams is unavailable. Optional email copy (`Features:EmailFollowUps`): recorded to an outbox, and sent through Microsoft Graph when `Email:Mode=Graph` (M9, section 11).
- **Reports (M6):** KPIs (say/do, velocity, carry-over, blocker age, attendance, follow-up closure), sprint and monthly narrative, and Q&A shared with the Teams chat. KPI history is an illustrative fixture because the sandbox project has no past sprints.
- **Planning (M8):** load per person, carry-over, Definition-of-Ready gaps, suggested owners (advice only).
- **Retrospective (M8):** start/stop/continue built in code from the sprint and standups; a model writes the opening summary. Text, not voice-led.
- **Story Writer (M8):** drafts a user story and acceptance criteria from a parked topic. Draft only; never written to Azure DevOps.
- **Models page (M8):** estimated cost per standup and measured latency for realtime models. Prices are placeholders to verify.
- **Teams chat Q&A:** any message to the bot is answered by `AnswerTeamQuestionQuery`. Microsoft 365 Copilot is **not built**: the app manifest declares a bot only.
- **Foundry agent mode (M8):** `VoiceLive:AgentName` and `AgentProjectName` bind the session to a Foundry voice agent instead of a model. Implemented, not exercised against a live agent; parameter names need verifying.
- **Not built:** Work IQ real-time transcripts (needs delegated sign-in, a Work IQ MCP connection and a tenant setting; delivery contract not documented enough); Graph media bot; API Management, Service Bus, private networking, on-behalf-of, evaluation pipelines.

## 6. Portal
Overview, Sessions (with pre-brief and timeline), Live console, Follow-ups, Reports, Planning, Retrospective, Story Writer, Models and email, Schedule, Prompts, Team, Roster. Light and dark themes, responsive.

## 7. Demo environment
- Azure DevOps organisation `<your-organisation>`, project `Zava-Scrum-Demo`, seeded idempotently by `tools/SeedAdo` from `demo/seed-ado.json` (29 items: 19 in the sprint and 10 backlog items, plus the next iteration "Sprint 43"; each assigned to the signed-in user and tagged `demo-owner-{key}`, ADR 0002; roster Daniel Park, Maya Lin, Admin, matching the tenant users "Daniel Park", "Maya Lin", "System Administrator").
- Demo cast (`demo/cast/payments-standup.yaml`) lets one presenter run a full three-person standup with a scripted side conversation.
- Deploy with `azd up` / `azd deploy`. Run the runbook in `DEMO.md`.

## 8. Known limits and open items
- Teams app installation from the admin center failed repeatedly with a tenant/admin-portal error; sideloading per user is the workaround. Until installed, cards fall back to the portal outbox.
- Voice latency p50 is about 2 to 3 seconds against a 1 second target; each speaker now waits briefly while the agent checks their update.
- The utterance classifier recognises English phrasing; unusual wording can be missed. PROD: model-assisted classification for ambiguous speech.
- Only one API replica is supported (live state is in memory).
- Cosmos has public network access (Entra-only) because the tenant policy blocked private defaults; PROD: private endpoint.
- ACS uses a connection string in a secret; PROD: Entra with role assignment.
- `JsonFile*` class names are historical; they sit on `ICollectionStore` (file or Cosmos).
- Repository contract test: `SessionRepositoryContract` exists for `ISessionRepository` only and runs against the JSON-file store only; there is no Cosmos run.
- OpenTelemetry to Application Insights is basic (requests, outgoing HTTP, one span per use case, Agent Framework model calls) and is on only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set; it has not been checked in a live Application Insights resource and has no custom metrics, sampling or bridge traces. The gateway logs one structured line per agent turn through `ILogger`.
- Agents are Microsoft Agent Framework chat-client agents (`AIProjectClient.AsAIAgent`, Azure.AI.Projects 2.x) behind the same ports (ADR 0009b). Foundry hosted agents are not used. `// PROD: Foundry hosted agents`.
- `knowledge/working-agreements.md` is not read by any code.
- The sprint calendar's Graph app (`Calendars.ReadWrite`, application) is not yet scoped to one mailbox with an Exchange application access policy; the `.ics` download is the fallback when `Calendar:Organizer` is empty.
- Email follow-ups are recorded to an outbox; `Email:Mode=Graph` sends them through Microsoft Graph but has not been run against a live mailbox.
- Escalation cards have no Acknowledge or Reassign buttons.
- The shared board in a live meeting is still being validated.

## 9. Tests and quality gates
227 Domain, 80 Application, 39 Infrastructure and 7 architecture tests; `dotnet format` clean; lint and typecheck for bridge and portal; an end-to-end Simulated Room standup (6 checks) via `npm run e2e --prefix src/meeting-bridge`. A scripted "messy standup" exercised chime-ins, step-away, circle-back, silence, what-to-report, correction and proxy against the real gateway.

## 10. Decision records
0001 domain events; 0002 to 0006 deviations by milestone (M1 to M7, Teams bridge, scheduler, reports, Cosmos); 0007 M8 stretch features, shared board, Teams audio; 0008 goal-driven orchestration; 0009 documentation baseline (which document wins, and the `StandupSession` naming debt); 0009b Agent Framework and OpenTelemetry; 0010 ceremony kinds, packs and carry-over (M9); 0011 sprint planning pack (M9a). `DOC-DRIFT.md` lists the differences found between the documents and the code.

## 11. M9: ceremony kinds, packs and carry-over
Built after the reconciliation (ADR 0010). Behaviour is in code and covered by unit tests; none of it has been run in a live Teams meeting.

- **Ceremony kind.** `CeremonyKind` on the session (default `DailyScrum`, so saved sessions load unchanged). The class is still `StandupSession`.
- **Change kinds.** `ChangeOp` is `Comment`, `State`, `Tag`, `SetIteration`, `Assign`, `CreateWorkItem`. The attribution is "Approved by {name} in {Teams|the portal}, proposed by Zava Scrum Agent from the {date} {ceremony}."; the standup text is unchanged. `ApprovalGuardKindsTests` refuses every new kind unless approved.
- **Ceremony packs.** `POST /api/ceremonies/{SprintReview|Retrospective}/pack` creates a session of that kind and one Scrum Master follow-up with **Approve all** and **Reject** (no per-item checkboxes). Figures come from `ReviewPackBuilder` and `RetroPackBuilder`; the model only rephrases the summary and is ignored if it names an unknown item or a number the pack did not compute. There is no portal button for these two packs. Sprint planning is separate (next section).
- **R1 Graph email.** `Email:Mode=Graph` and `Email:Sender` select `GraphEmailSender`; the email is also recorded in the outbox and a Graph failure is logged, not thrown.
- **R2 unblock loop.** A recorded blocker makes the next turn ask who can help (`turn-unblocker.md`); `UnblockerResolver` matches the answer to one roster member in code; the member gets an `Unblock` card and email; the next pre-brief lists still-blocked items with the unblocker and the agent asks them in their turn. Blocked items escalate to the Scrum Master after `Flags:EscalateBlockerAfterDays` whether or not they were raised again.
- **R3 commitment carry-over.** `PrepareStandup` carries each member's open commitments from the last three started standups into `MemberBrief.PreviousCommitments`; the call-on turn asks about them first; a commitment still open two standups later is flagged `MissedCommitment` and escalated.
- **R4 `AtRiskRule`.** Flags items due within `Flags:AtRiskDays` that have more than `Flags:AtRiskRemainingPct` of the original estimate left, or are still `New`. Seeded by the task "Prepare reconciliation report for audit" (re-run `SeedAdo` to add it).
- **R5 recap routing.** The recap goes to `Team.RecapChannelKey` (captured when the bot is added in a channel, or `PUT /api/teams/{id}/recap-channel`) and to any `19:meeting_` chat; every stored conversation is the fallback.
- **Gaps.** The new voice turns (unblocker question, carry-over notes) are built into `VoiceGateway` but exercised only through use-case tests, not a live Voice Live session. A deployed environment needs a new image and, for Graph email, `Mail.Send` plus an application access policy.

## 12. M9a: sprint planning
Built after M9 (ADR 0011); `SPEC.md` section 10.6 has the rules.

- **What it does.** **Prepare pack** (portal, API or the chat command "prepare sprint planning" for names in `Team:ScrumMasters`) computes a plan: capacity from the velocity range (the illustrative fixture) split equally by member and scaled by each member's availability; started stories and bugs carry over first; ready backlog items follow in backlog order until the top of the range; not-ready items are listed with why (no points, over 8 points, no acceptance criteria). Unowned selected items get a suggested owner (least loaded). A model phrases the draft goal, with a code fallback. Over- and under-commitment and overloaded members are flagged.
- **Decision.** A named person approves in the portal (**Approve plan**, with a checkbox per item) or in Teams (the card's **Approve plan**, approver = `Activity.From.Name`). Approval creates a Scrum Master follow-up with `SetIteration` and `Assign` changes and writes through the same guard as every follow-up. **Reject** writes nothing.
- **DEMO.** With owner tags on (the default) an `Assign` sets the `demo-owner-{key}` tag, because the sandbox has one real user.
- **Checked live.** Against the sandbox: prepare, reject (nothing written), approve (items in Sprint 43, owner tags, "Approved by" comment). **Not checked live:** the Teams card actions and the chat command.
- **Limits.** Child tasks are not moved with their story. Per-member capacity warns but does not constrain the selection. Capacity ignores calendars. The history is illustrative. A team with no future iteration can prepare a pack but not approve it. A re-seed resets everything a plan moved.

## 13. M9b: retrospective by voice
Built after M9a (ADR 0012); `SPEC.md` section 10.7 has the rules.

- **What it does.** **Prepare retro** (Retrospective page or `POST /api/retrospective/prepare`) stores a session with `Kind=Retrospective`. Started in the Simulated Room (or a Teams meeting) it runs on the same gateway and floor control as the standup: the agent opens with sprint facts, asks each person in turn for what went well, what to do better and what is missing (up to three notes each), asks "Is that something to start, stop or keep doing?" when a note is unclear, reads the themes back, asks up to three people for one action each and confirms the owner, then closes. Step-away, silence, circle-back and correction work as in the standup.
- **In code, not by the model.** Sorting (`RetroItemClassifier`), the order (`RetroAgenda`), grouping (`ThemeGrouper`), owners (roster names matched in code) and every count. The model only phrases turns and names the theme groups (falling back to the shared keyword).
- **Decision.** When the retrospective ends, a Scrum Master follow-up with one `CreateWorkItem` change per action (Task, `retro-action`, next sprint, owner) is sent as a card and a summary goes to the recap channel. Approving creates the tasks through the same guard as every follow-up; rejecting writes nothing.
- **Standup is unchanged.** `StandupSession.NextSpeaker` delegates to `StandupAgenda`; the standup e2e still passes (6 of 6).
- **Checked live.** In the Simulated Room: `npm run e2e:retro` (notes in all three categories, the clarifying question, a step-away, a self-correction, three actions with owners, a pending follow-up; about 5 minutes against the 15-minute timebox). Approving the follow-up through the API created three Tasks in Sprint 43 with `retro-action`, `demo-owner-*` tags and the "Approved by" first comment; they were deleted afterwards. **Not checked live:** a Teams meeting, the Teams card actions, and **Reject** against the sandbox.
- **Limits.** One format change only (titles); no 4Ls or sailboat. Themes use shared keywords, so unrelated wording is not grouped. The agent must hear the category keywords to sort a note; otherwise it asks once, and a non-answer leaves the note unsorted. The retro board is drawn on the shared screen and the Live console shows the floor only (the portal has no board view). The cast script is the only tested path through the Actions phase.

## 14. M9d: sprint calendar
Built after M9b (ADR 0013); `SPEC.md` section 10.8 has the rules.

- **What it does.** **Propose calendar** (Schedule page or `POST /api/calendar/propose`) plans the next sprint's meetings in code from the iteration dates and the schedule: a daily standup, planning after the day-one standup, and the retrospective (and a review when `Team:HasSprintReview`) after the last-day standup. The Scrum Master gets a **Proposed sprint calendar** card. **Approve** creates each meeting as a Teams meeting in the agent's calendar through Microsoft Graph and invites the roster; **Reject** creates nothing.
- **Scheduler.** The standup join URLs are stored on the schedule, so the scheduler joins without a pasted link. Approving also turns the schedule on in `acs` mode.
- **Fallback.** `GET /api/calendar/{iteration}.ics?teamId=` when `Calendar:Organizer` is not set. (The route was named in the brief as existing; it was not, so it is new.)
- **Checked live.** Checked live against the demo tenant: approving created 12 Teams meetings (one per planned meeting) in `admin@`'s calendar, each with a Teams join URL and the three roster members invited, at the right UTC time for 09:30 Pacific; approving again was refused (`calendar.already_decided`); the join URLs were stored on the schedule; and with the schedule armed the scheduler prepared the session, started it with the calendar's join URL (no pasted link), and the agent reached the real meeting and waited in the Teams lobby. **Not checked:** the invites' appearance in each recipient's Outlook UI, the Teams card actions, and an Exchange application access policy (creating it needs an interactive Exchange Online sign-in, so the app is currently not scoped to one mailbox).
- **Limits.** One event per meeting, not a series. No update or cancel when dates change. Join URLs are stored at creation, not read back from the calendar. Holidays and out-of-office are ignored. Roster members without an email are not invited.
