# MCM Scrum Master voice PoC: as built

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
2. **Join:** the agent joins the Teams meeting, waits in the lobby if needed, shares the standup board as its screen, and begins shortly after the first roster member appears. Members are matched by name; anyone not present is marked absent.
3. **Floor control:** a pure Domain state machine owns who speaks. The agent only speaks when it is allowed to, and stops when a human barges in.
4. **Each turn:** call on a member, listen, then reconcile what they said against their items. Item links come from code (`ItemMentions`: "story 5", "task fourteen", or a distinctive title word), never from a model guess. The agent briefly says what it noted (blockers, commitments) and asks about the most urgent flagged item not yet covered (blocked, then overdue, then stale, then no recent update), up to three questions per person.
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
2. Work item ids are never invented: every id reaching speech, a card or the board is checked against the sprint.
3. Numbers and flags come from code (flag rules, burndown, KPIs, coverage, priorities); models only phrase.
4. No secrets in the repo; `DefaultAzureCredential`, user secrets locally, Container Apps secrets in Azure.
5. Prompts live in `agents/instructions/*.md`; edits made in the portal are stored in Cosmos and survive restarts.

## 5. Features by area
- **Standups:** prepare, start (Simulated Room or Teams), live console (floor, speaker, timebox, parking lot, latency p50/p95, Bridge events), control (begin, mute, take over, skip, end), scheduler (prepare T-10 min, start T-1 min), demo reset.
- **Follow-ups:** per-member follow-up with proposed Azure DevOps comments, escalation for long-blocked items, "missed standup" and "no response" messages. Adaptive cards in Teams with approve and reject; the card updates with the outcome. Outbox view in the portal when Teams is unavailable. Optional email copy (`Features:EmailFollowUps`), recorded to an outbox in the demo.
- **Reports (M6):** KPIs (say/do, velocity, carry-over, blocker age, attendance, follow-up closure), sprint and monthly narrative, and Q&A shared with the Teams chat. KPI history is an illustrative fixture because the sandbox project has no past sprints.
- **Planning (M8):** load per person, carry-over, Definition-of-Ready gaps, suggested owners (advice only).
- **Retrospective (M8):** start/stop/continue built in code from the sprint and standups; a model writes the opening summary. Text, not voice-led.
- **Story Writer (M8):** drafts a user story and acceptance criteria from a parked topic. Draft only; never written to Azure DevOps.
- **Models page (M8):** estimated cost per standup and measured latency for realtime models. Prices are placeholders to verify.
- **Foundry agent mode (M8):** `VoiceLive:AgentName` and `AgentProjectName` bind the session to a Foundry voice agent instead of a model. Implemented, not exercised against a live agent; parameter names need verifying.
- **Not built:** Work IQ real-time transcripts (needs delegated sign-in, a Work IQ MCP connection and a tenant setting; delivery contract not documented enough); Graph media bot; API Management, Service Bus, private networking, on-behalf-of, evaluation pipelines.

## 6. Portal
Overview, Sessions (with pre-brief and timeline), Live console, Follow-ups, Reports, Planning, Retrospective, Story Writer, Models and email, Schedule, Prompts, Team, Roster. Light and dark themes, responsive.

## 7. Demo environment
- Azure DevOps org `fordjames-demos`, project `MCM-Scrum-Demo`, seeded idempotently by `tools/SeedAdo` from `demo/seed-ado.json` (18 items; roster Daniel Park, Maya Lin, Admin, matching the tenant users "Daniel Park", "Maya Lin", "System Administrator").
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
- No automated Cosmos contract test.
- The shared board in a live meeting is still being validated.

## 9. Tests and quality gates
142 Domain, 46 Application, 20 Infrastructure and 7 architecture tests; `dotnet format` clean; lint and typecheck for bridge and portal; an end-to-end Simulated Room standup (6 checks) via `npm run e2e --prefix src/meeting-bridge`. A scripted "messy standup" exercised chime-ins, step-away, circle-back, silence, what-to-report, correction and proxy against the real gateway.

## 10. Decision records
0001 domain events; 0002 to 0006 deviations by milestone (M1 to M7, Teams bridge, scheduler, reports, Cosmos); 0007 M8 stretch features, shared board, Teams audio; 0008 goal-driven orchestration.
