# Zava Scrum Master voice PoC: as built

**Date:** November 26, 2026 (M11 final validation).

**Acceptance is not implementation.** The current demo is Acs/Share/Direct
with Simulated Room fallback. Stable hosted relay 4 is retained; candidate 5
failed readiness/paired protocol (424, completed paired samples n=0).
Native Windows hosting/TLS/renderer are verified, not native Teams
attribution/join/board acceptance. Five delegated Copilot skills and package
1.1.0 are implemented, with real Copilot G5 still open. G7 revised dates fail
(75%, n=8); latest fallback p95 regression fails (+12.75%, n=12/12),
and historical p50 +6.19% remains failed. Final configured suites have
passing evidence after one unchanged retrospective retry; its initial
model deviation remains a reliability risk. G8 recovery has one complete
verified empty up/verify cycle of two; intervening purge passed1639.20s
with group/vault/Foundry absence readback. The second cycle is incomplete.
The attempted second deployment was reduced because teardown erased the
native opt-in's colliding output; it is not counted. The input-only fix and
compiled cleanup regression17/17 pass, and a full empty retry is required.
That invalid iteration also failed hosted version creation with the exact
Foundry409 workspace-identity conflict after1036.69s. AgentDeploy now reads
the current agent before at most two retries of that conflict only; other
errors remain explicit. Focused recovery tests6/6 and all three builds pass;
the isolated preview tool retains its existing dependency-resolution warning.
Invalid-footprint purge passes1742.02s; full native-enabled empty retry
started07:15:07Z after complete absence and13 documented input readbacks.
It failed 1229.5552 s later: three hosted-version creations returned the
same409 despite two successful reads. Independent native completion then
received403 Authorization_RequestDenied, request
07d95ae3-fd0a-4479-9442-9e7d14dd3abf. G8 is human-blocked at 1/2;
legal pages and messaging package generation independently passed.
Current24-resource native VM/vault provisioning is not certificate,
SYSTEM task or renderer readiness; the current host is unbootstrapped.
DEPLOY.md records exact administrator/support actions and continuing costs.
Final independent mandatory verify passes8/8 in361.4580s, approved audit/
revision58, Direct2821/4189ms p50/p95,n10,talkovers0. It explicitly prints
native MEDIA-TLS readiness not verified. First-to-final tail +13.77%
exceeds10% tolerance; these are different windows, not causal parity.
Failed full up still prevents cycle2 credit; inherited G1 remains failed.
See [gate evidence](gates/M11-gates.md), [deployment gates](DEPLOY.md) and
[ADR index](adr/README.md); knowledge and Work IQ are not demo dependencies.

**G8 recovery and operator update:** renewed Global Administrator and
unchanged zavagf privileged writes passed. Recovery uses the unrestricted
x64 Standard_D4als_v7 (4vCPUs, Gen2, Windows USD0.345/hour) in eastus2.
Image/purge parameters preserve existing defaults; the explicitly disposable
zavagf vault is purgeable with soft delete retained. Hook scope guards,
native-capacity validation, registry-role gate and optional permission merge
are implemented (ADR0031). First full empty up passes1828.77s; mandatory
verify8/8 passes344.83s with a real approver-authored write/auditrevision55.
The first complete cycle's D4als_v7 had127GB StandardSSD_LRS/V2 disk, trusted HTTPS,
no-recording/cap1/1280x720renderer and SYSTEM/AtStartup task readiness.
Known VM/disk/IP tariffUSD0.363150685/hour excludes variable charges.
The failed first vault iteration and conductor verification-layout failure
are retained in the gate evidence, not erased by the passing retry.
New zavagf catalogue publication was attempted once and refused403 for
missing OAuth catalogue scopes; exact Teams administrator action is in DEPLOY.md.
Second empty up/verify remains required; no native Teams acceptance is claimed.
Scout completed the zavaw3 Graph Files.Read.All/Sites.Read.All and Work IQ
WorkIQAgent.Ask delegated grants and both tenant catalogue uploads.
Real user/source/billing/Exchange/CI/organiserRSC gates remain open.

**M11 8B status (26 November 2026):** Work IQ consent and advisory evidence
contracts are built, but the live spike and integration are blocked.
The operator has now granted WorkIQAgent.Ask; a licensed, consenting team lead with an
approved spending-policy assignment and scoped channel/meeting fixture is
still required. Domain consent/revoke rules, typed Application ports and
the unregistered read boundary have focused tests through the real mediator
with port fakes. No live adapter, store, cache, portal connection page,
card enrichment, chase suppression, helper UI or review-to-backlog conversion
is shipped. M9c remains open; G11 live queries/latency/leakage have n=0.
Default-off signals and all deployed paths remain unchanged. See ADR 0027,
WORK-SIGNALS-PRIVACY.md and DEPLOY.md for the exact resume boundary.

This document describes what the proof of concept actually does today. It is the companion to the build spec (`SPEC.md`, what was planned) and the ADRs (`adr/`, where the build deviated and why). Where they differ, this document and the ADRs are right.

**M11 8A-r status (27 November 2026):** Foundry IQ is blocked before its
mandatory caller-OBO permission spike, not implemented or deployed.
Actual zavaw3 grant/declaration readbacks verify `Files.Read.All` and `Sites.Read.All`;
an approved standards corpus and two-user caller-OBO permission fixture
are still outstanding. Five actual teams have no knowledge bases or stored
memberships; no Search service exists in zavaw3. `KnowledgeBaseIds` remains reserved.
There is no `IKnowledge`,
Knowledge page, standards chat skill or knowledge enrichment in pre-briefs,
follow-ups, Story Writer or retrospective packs. Repo working agreements
are not a substitute. G10 retrieval and permission tests have n=0, not a
zero-leakage pass. Genuine admin portal-to-Graph OBO and matching `/me` metadata
succeed n1 each, using the existing confidential-client credential only in a
bounded memory-only probe; deployed federation/source permissions are unproved.
ADRs 0026/0033 and DEPLOY.md give the exact resume prerequisites.
The existing zavaw3 runtime and voice/approval boundaries are unchanged.

## 1. What it is
A team-scoped voice-led "junior Scrum Master", with Payments as the demo
team and Identity/Catalogue seed definitions. The ACS path joins Teams,
facilitates daily standups, shares a board and proposes follow-ups. Approved
changes use the recorded approver's delegated Azure DevOps access; rejection
writes nothing. Native non-recording facilitation deliberately cannot save
spoken updates or create meeting-derived proposals.

## 2. Architecture
```
Teams meeting <-> ACS calling SDK (headless Chromium on the bridge) <-> API voice gateway <-> Azure Voice Live
                                                                     |-> Application (hand-rolled mediator, commands/queries)
                                                                     |-> Domain (flag rules, floor control, classifiers, KPIs)
                                                                     |-> Infrastructure: Azure DevOps REST, Foundry agents, Cosmos, Bot
Admin portal (React, Tailwind) -> API (served by the same container)
Optional voice route (VoiceLive:RelayEndpoint): API voice gateway -> Foundry hosted agent (src/Zava.Scrum.AgentHost, invocations_ws relay) -> Azure Voice Live
```
- **Agent host (`src/Zava.Scrum.AgentHost`, prompt 4):** the Foundry hosted agent. Today it carries the stateless voice relay (`invocations_ws`, port 8088) and nothing else; the voice gateway, ceremony agents and Teams handlers are still in the API (section 20 lists what is not moved). Built as an image in the environment registry by `infra/hooks/agent-host.ps1`.
- **Native media host (`src/Zava.Scrum.MediaBot`):** separate public Windows
  four-vCPU Graph SDK host, trusted TLS, protected SYSTEM bootstrap and
  browser-free board renderer. GraphMedia/MediaShare are real alternatives,
  not the default saved-follow-up path. Hosted Agent ID, the media calling
  application, the API managed identity and portal OBO application are distinct.
- **Solution:** Clean Architecture, four projects (`Domain`, `Application`, `Infrastructure`, `Api`), enforced by architecture tests. Hand-rolled mediator with logging, validation and exception-mapping behaviours. No MediatR.
- **Bridge (`src/meeting-bridge`):** Node 22 + TypeScript. Serves the Simulated Room and the Teams call page, launches Chromium, and holds the ACS identity. Fallback for any live demo: the Simulated Room (scripted cast).
- **Portal (`src/admin-portal`):** React 19, Vite, Tailwind CSS v4, lucide icons, hash routing, Entra sign-in (Easy Auth).
- **Azure (West US 3, `azd up`):** two Container Apps (`api` serves the portal too; `bridge`), ACR, user-assigned managed identity, Cosmos DB (Entra-only), Azure Communication Services, Azure Bot (Teams channel), Foundry (chat and Voice Live), Entra apps for the bot and the portal.
- **Audio:** PCM16, 24 kHz, mono. The bridge feeds meeting audio to the gateway and plays agent audio into the call through a virtual microphone. Chromium runs with `--use-fake-device-for-media-stream`; without a microphone device the ACS SDK silently drops custom audio (azure-sdk-for-js#29174).

## 3. The standup, as it actually runs
1. **Prepare:** facts are computed in code from Azure DevOps: sprint, burndown, per-person open items and flags (stale, blocked, overdue, unassigned, no recent update). A model only phrases a summary and questions; ids outside the sprint are dropped.
2. **Join:** the agent joins the Teams meeting, waits in the lobby if needed, shares the standup board as its screen, and begins 8 seconds after the first roster member is matched (the portal's **Begin** button starts it sooner). Members are matched by any word of their Teams name; anyone not present is marked absent.
3. **Floor control:** a pure Domain state machine owns who speaks. The agent only speaks when it is allowed to, and stops when a human barges in.
4. **Each turn:** call on a member, listen, then reconcile what they said against their items. Item links come from code (`ItemMentions`: "story 5", "task fourteen", or a distinctive title word) or from the decision model's reading, which code accepts only when it names an id on the roster or sprint (ADR 0014); they are never taken from free model text. The agent briefly says what it noted (blockers, commitments) and asks about the most urgent flagged item not yet covered (blocked, then overdue, then at risk, then stale, then no recent update), up to three questions per person.
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
Each utterance is read by `AnalyseUtteranceQuery` (ADR 0014): a decision model (Microsoft-Decision-1.1 on the `zava-decision` deployment, or the chat model with `UtteranceAnalysis:Engine=Chat`) returns the intent, the work items and the comment kind (blocker, date change, commitment, decision, estimate, done) in one call, and `UtteranceAnalyser` checks every member and ticket id before anything acts on it. Rules (`UtteranceClassifier`, `ItemMentions`) are the fallback when the model is off, slower than `UtteranceAnalysis:TimeoutMs`, unsure or wrong; the analysis step takes about 0.3 s once the token is cached (`CachingTokenCredential`) and the analyser is warmed at session start; p50 agent latency in the Simulated Room is 3.4 to 4.0 s. Set `UtteranceAnalysis:Mode=Rules` to switch it off. A barge-in stops the voice at once, drops audio still in flight, truncates the model's message to what was heard, and the next turn acknowledges it; a member who is not holding the floor can cut in after `Voice:SustainedBargeInMs`. In Teams the mixed audio has no speaker, so the speaker is the member whose live "speaking" event overlaps the speech, else the floor holder.

**Timing.** Every agent reply that follows someone speaking records a `Timing` session event (JSON: reason, total, speech end to transcript, analyser, decision model, orchestration, Voice Live first audio, and whether the model or the rules read the utterance). It is diagnostic only. `tools/GateRunner` reads it to produce the p50 and p95 per hop (DEMO.md, "Gate runner"; baseline in `docs/gates/M11-gates.md`).

### 3.2 Shared board
The agent shares a 1280x720 canvas as its screen: the agenda of flagged items at the start, then the current speaker's items (state, flags, owner, a highlight on the item being asked about, a tick when covered) and per-member coverage with a status (away, no response, update by someone). Frames are forced from a canvas attached to the page. Sharing is best effort and supervised: it retries until Teams confirms, and every status change appears as a Bridge event on the Live tab and in the bridge log (`[share]`). **Status:** observed working in a real Teams meeting on 23 November 2026 (the recorded take).

**M11 prompt 7, ADR 0024:** `Board:Mode=Share` retains that ACS canvas.
`MediaShare` selects native VBSS on the GraphMedia call: a browser-free SkiaSharp
1280x720 renderer, draw on changed state and cached one-fps keep-alive, automatic
sharer/viewer lifecycle and reconnect renegotiation independent of voice.
Team/session-owned snapshots, coverage and retro notes/actions remain ephemeral;
native completion never promises saved follow-ups. The shared meeting view is
visible to its participants, not a guest query/approval grant. The portal shows
share failures and reports Sharing only after SDK frame submission; known
failures/timeouts remain recoverable on reconnect. No personal tab is built
because ADR 0015 S7 did not run.
Native visibility/encoder CPU/G1 and live meeting events remain n=0; ACS/Direct
remain defaults. Golden-image evidence and deployed checks are in the gate table.

The Azure-managed SSO bot handles typed Teams start/end events using scheduled
meeting `OnlineMeeting.ReadBasic.Chat` consent. It resolves tenant and bound team,
correlates channel-data meeting id with the stored join link, records lifecycle
metadata, and ends only the matching live ceremony idempotently through existing
end events. It does not start a new ceremony from an unbound event. Participant
join/leave is authoritative media call roster data, not Teams-managed subscriptions.
Catalogue upload/RSC and a real meeting are still required to verify delivery.

## 4. Safety rules that are enforced in code
1. No Azure DevOps write without an approved follow-up (`FollowUp.GetApprovedChanges`, `ApplyApprovedChangesHandler`, unit tested). Approval is a card button in Teams or the portal.
2. Work item ids are never invented: ids in recorded events, follow-ups, cards, generated questions, summaries and the board are checked against the sprint in code (`WorkItemIdGuard`, `RecordStandupEventHandler`). The facilitator model's free speech is constrained by its prompt and checked only by the end-to-end test "never mentions a work item id outside the sprint".
3. Numbers and flags come from code (flag rules, burndown, KPIs, coverage, priorities); models only phrase.
4. No secrets in the repo; `DefaultAzureCredential`, user secrets locally, Container Apps secrets in Azure.
5. Prompts live in `agents/instructions/*.md`; edits made in the portal are stored in Cosmos and survive restarts.

## 5. Features by area
- **Standups:** prepare, start (Simulated Room or Teams), live console (floor, speaker, timebox, parking lot, latency p50/p95, Bridge events), control (begin, mute, unmute, skip, take over, hand back, end), scheduler (prepare T-10 min, start T-1 min), demo reset.
- **Follow-ups:** per-member proposed comments, escalation, absent/no-response
  messages and Approve/Reject cards. The planner proposes comments only;
  explicit proposal APIs support other change kinds. Cards use owning-team
  bindings (Standup/Recap, or Planning/Recap for plans), never global broadcast;
  no binding means outbox only. Optional email uses Outbox/Graph settings.
- **Reports (M6):** KPIs (say/do, velocity, carry-over, blocker age, attendance, follow-up closure), sprint and monthly narrative, and Q&A shared with the Teams chat. KPI history is an illustrative fixture because the sandbox project has no past sprints.
- **Planning (M8):** load per person, carry-over, Definition-of-Ready gaps, suggested owners (advice only).
- **Retrospective (M8):** start/stop/continue built in code from the sprint and standups; a model writes the opening summary. Text, not voice-led.
- **Story Writer (M8):** drafts a user story and acceptance criteria from a parked topic. Draft only; never written to Azure DevOps.
- **Models page (M8):** estimated cost per standup and measured latency for realtime models. Prices are not set (they were placeholders): the page shows the Voice Live pricing tier from Learn and "Price unverified" until real rates are configured (ADR 0019).
- **Teams and Copilot Q&A (M11 8, ADR 0025):** five read-only skills use `AnswerCopilotQuestionQuery`, membership team resolution and the asker's OBO Azure DevOps token. Blockers, commitments, risk, today's standup status and personal item states are computed/filtered in Domain; a model phrases only the introduction. Responses include source cards and use the Agents SDK streaming channel when supported. Unknown Copilot requests never fall through to managed-identity reporting. Other Teams reporting and planning remain. Manifest 1.25/version 1.1.0 declares the existing bot as a custom engine agent; actual Copilot discovery, SSO/OBO and permission-negative G5 acceptance remain unrun pending tenant upload/user consent.
- **Foundry agent mode (M8):** `VoiceLive:AgentName` and `AgentProjectName` bind the session to a Foundry voice agent instead of a model. Implemented, not exercised against a live agent; parameter names need verifying.
- **Not built:** knowledge/Work IQ integrations, live transcript transport,
  hosted orchestration/channel migration, production infrastructure and
  employee-content quality evaluation. Synthetic decision evaluation and
  rules CI reporting are built; real-model CI is not enabled without its
  approved identity/variables. OBO is built for approved writes and the five
  Copilot reads, not scheduled voice reads. Native acceptance remains open.

## 6. Portal
Overview, Sessions (with pre-brief and timeline), Live console, Follow-ups, Reports, Planning, Retrospective, Story Writer, Models and email, Schedule, Prompts, Team, Roster. Light and dark themes, responsive.

M11 adds team switching, fleet/registry/onboarding and membership management.
Film-feature gaps: no dedicated correlation-id audit page, no Live per-hop
counter panel and no verified readable speaker names for every participant.
The API/workbook and aggregate latency are not those UI features. SeedAdo
resets seeded work items/relative sprint dates; app reset does not reset
Azure DevOps, external meeting state or the whole roster/cast configuration.

## 7. Demo environment
- Azure DevOps organisation `<your-organisation>`, project `Zava-Scrum-Demo`, seeded idempotently by `tools/SeedAdo` from `demo/seed-ado.json` (29 items: 19 in the sprint and 10 backlog items, plus the next iteration "Sprint 43"; each assigned to the signed-in user and tagged `demo-owner-{key}`, ADR 0002; roster Daniel Park, Maya Lin, Admin, matching the tenant users "Daniel Park", "Maya Lin", "System Administrator").
- Demo cast (`demo/cast/payments-standup.yaml`) lets one presenter run a full three-person standup with a scripted side conversation.
- Deploy with `azd up` / `azd deploy`. Run the runbook in `DEMO.md`.

## 8. Known limits and open items
- Teams app installation from the admin center failed repeatedly with a tenant/admin-portal error; sideloading per user is the workaround. Until installed, cards fall back to the portal outbox.
- Finding 1.20 (recap and follow-ups sometimes missing after the standup ends): root cause and fix in ADR 0016 (ending the standup now publishes its events on `CancellationToken.None`, because leaving the call cancelled the gateway's token). Covered by an Application test and **resolved**: verified in a real Teams meeting on 24 November 2026 (ADR 0016). Before (build 84498ad): recap recorded 130 ms after the end, no follow-ups; a manual send created 4. After (3c78835, three consecutive runs): 4 follow-ups, all Sent, 2.4 to 2.5 s after the end, delivered to the Admin's chat. Not covered: the agent ending the standup itself (same `EndAsync` path) and a run with spoken answers.
- Voice latency p50 is about 3.7 seconds (p95 5.0 s) in the 23 November Teams take, against a 1 second target; with the M10 decision model on, the Simulated Room measured 6.2 s p50 against 4.2 s with rules (ADR 0014). Each speaker now waits briefly while the agent checks their update.
- The utterance classifier recognises English phrasing; unusual wording can be missed by the rules. The M10 decision model (ADR 0014) reads ambiguous speech first and the rules remain the fallback. PROD: tune the model and the thresholds on real standups.
- Only one API replica is supported (live state is in memory).
- Cosmos has public network access (Entra-only) because the tenant policy blocked private defaults; PROD: private endpoint.
- ACS is reached with Entra only (a role on the resource); there is no connection string or key (ADR 0020).
- `JsonFile*` class names are historical; they sit on `ICollectionStore` (file or Cosmos).
- Repository contract test: `SessionRepositoryContract` exists for `ISessionRepository` only and runs against the JSON-file store only; there is no Cosmos run.
- OpenTelemetry now includes content-free session hops, counters/histograms and the bridge (M11 9, ADR 0028). Export needs `AzureMonitor:ConnectionString` or the ordinary `APPLICATIONINSIGHTS_CONNECTION_STRING`; hosted versions use the explicit SDK destination because Foundry reserves the ordinary variable. Actual live acceptance is recorded in G7. Native vendor spans and Foundry live quality evaluation are not established. The gateway retains its structured console turn log.
- API agents are Microsoft Agent Framework chat-client agents behind the
  existing ports (ADR 0009b). The optional hosted relay uses its own Agent ID,
  not Azure DevOps or channel handlers; stable 4 remains pinned.
- `knowledge/working-agreements.md` is not read by any code.
- The sprint calendar's Graph app (`Calendars.ReadWrite`, application) is not yet scoped to one mailbox with an Exchange application access policy; the `.ics` download is the fallback when `Calendar:Organizer` is empty.
- Email follow-ups are recorded to an outbox; `Email:Mode=Graph` sends them through Microsoft Graph but has not been run against a live mailbox.
- Escalation cards have no Acknowledge or Reassign buttons.
- The shared board was seen working in a live Teams meeting on 23 November 2026 (the recorded take); it stays best effort.

## 9. Tests and quality gates
The M11 final checkpoint (26 November 2026) passes947.NET tests:
424Domain,225Application,93Infrastructure,168Api,14architecture,
3AgentHost and20WindowsMediaBot in one aggregate run, no failures/skips.
Bridge unit tests pass13/13. All three
builds and both TypeScript lint/typecheck checks pass. The portal has no
configured test suite; build/lint is not reported as a test pass.
Full solution formatting passes without code changes. One real E2E aggregate
passes standup6/6 in371.16s and retrospective5/6 in351.44s. Voice Live
omits the requested circle-back question, so the cast never supplies Daniel's
correction; the effective default template is correct. One unchanged focused
retrospective retry passes6/6 in267.29s. No code/prompt/cast/assertion change
or aggregate rerun; all972unique configured checks have passing evidence,
but the initial failure remains a live model reliability risk.
Final zavaw3 verify passes8/8 with actual approver-authored revision53 and
complete audit chain; scripted12turn latency3262/5307ms fails the preceding
run's p95 tolerance. G7 real synthetic n72 still fails dates75%n8 and has
one timeout/missing probabilities9/1. Measured evidence is recorded in
`gates/M11-gates.md`. The existing Simulated Room standup has six checks via
`npm run e2e --prefix src/meeting-bridge`; the retrospective has six via
`npm run e2e:retro --prefix src/meeting-bridge`. A scripted "messy standup"
previously exercised chime-ins, step-away, circle-back, silence, what-to-report,
correction and proxy against the real gateway.

## 10. Decision records

The complete [ADR index](adr/README.md) covers Domain/mediator decisions,
M0-M10 deviations and M11 platform, identity, media, Copilot, blocked
knowledge/Work IQ and observability decisions. `DOC-DRIFT.md` records the
current corrections without overwriting historical measured failures.

## 11. M9: ceremony kinds, packs and carry-over
Built after the reconciliation (ADR 0010). Behaviour is in code and covered by unit tests; none of it has been run in a live Teams meeting.

- **Ceremony kind.** `CeremonyKind` on the session (default `DailyScrum`, so saved sessions load unchanged). The class is still `StandupSession`.
- **Change kinds.** `ChangeOp` is `Comment`, `State`, `Tag`, `SetIteration`, `Assign`, `CreateWorkItem`. The attribution is "Approved by {name} in {Teams|the portal}, proposed by Zava Scrum Agent from the {date} {ceremony}."; the standup text is unchanged. `ApprovalGuardKindsTests` refuses every new kind unless approved.
- **Ceremony packs.** `POST /api/ceremonies/{SprintReview|Retrospective}/pack` creates a session of that kind and one Scrum Master follow-up with **Approve all** and **Reject** (no per-item checkboxes). Figures come from `ReviewPackBuilder` and `RetroPackBuilder`; the model only rephrases the summary and is ignored if it names an unknown item or a number the pack did not compute. There is no portal button for these two packs. Sprint planning is separate (next section).
- **R1 Graph email.** `Email:Mode=Graph` and `Email:Sender` select `GraphEmailSender`; the email is also recorded in the outbox and a Graph failure is logged, not thrown.
- **R2 unblock loop.** A recorded blocker makes the next turn ask who can help (`turn-unblocker.md`); `UnblockerResolver` matches the answer to one roster member in code; the member gets an `Unblock` card and email; the next pre-brief lists still-blocked items with the unblocker and the agent asks them in their turn. Blocked items escalate to the Scrum Master after `Flags:EscalateBlockerAfterDays` whether or not they were raised again.
- **R3 commitment carry-over.** `PrepareStandup` carries each member's open commitments from the last three started standups into `MemberBrief.PreviousCommitments`; the call-on turn asks about them first; a commitment still open two standups later is flagged `MissedCommitment` and escalated.
- **R4 `AtRiskRule`.** Flags items due within `Flags:AtRiskDays` that have more than `Flags:AtRiskRemainingPct` of the original estimate left, or are still `New`. Seeded by the task "Prepare reconciliation report for audit" (re-run `SeedAdo` to add it).
- **R5 recap routing.** M9 added recap destinations; M11's `CardRouting`
  restricts them to the team's bindings/purposes. No global fallback.
- **Gaps.** The new voice turns (unblocker question, carry-over notes) are built into `VoiceGateway` but exercised only through use-case tests, not a live Voice Live session. A deployed environment needs a new image and, for Graph email, `Mail.Send` plus an application access policy.

## 12. M9a: sprint planning
Built after M9 (ADR 0011); `SPEC.md` section 10.6 has the rules.

- **What it does.** **Prepare pack** (portal, API or the chat command "prepare sprint planning" for an administrator's Entra object id in `Identity:AdminObjectIds`) computes a plan: capacity from the velocity range (the illustrative fixture) split equally by member and scaled by each member's availability; started stories and bugs carry over first; ready backlog items follow in backlog order until the top of the range; not-ready items are listed with why (no points, over 8 points, no acceptance criteria). Unowned selected items get a suggested owner (least loaded). A model phrases the draft goal, with a code fallback. Over- and under-commitment and overloaded members are flagged.
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

## 15. M11 greenfield deployment

The Bicep/hooks describe empty-environment deployment (ADR 0015a,
`docs/DEPLOY.md`); final-solution two-cycle G8 acceptance is not established.
Native hosting is opt-in, so a default deployment alone is not the full G8
proof. zavaw3 tenant package uploads are completed; real user/organiser
installation and consent remain gates. Another environment needs its own package.

- **Bicep:** resource group, telemetry/workbook, managed identity, Foundry
  account/project with three default model deployments, ACR/AcrPull,
  Cosmos/ACS (Entra), API/bridge Container Apps and messaging Bot. Opt-in
  native module adds the Windows host, vault/package storage and scoped roles.
  Hooks own hosted version/Agent ID, Entra/OBO, seed and package generation.
- **Hooks** (`infra/hooks/`): `preflight`, `entra-portal-app`, `entra-post` (Easy Auth), `ado-setup` (project, seed, managed identity access), `exchange` (human gate), `teams-app` (package build, upload is a human gate), `verify`.
- **Identity.** The bot id is the managed identity client id; Easy Auth holds one client secret as a Container Apps secret; the Azure CLI client is pre-authorised on the portal API scope so `verify` and GateRunner use a normal Entra token.
- **Not changed.** Application behaviour. The original `mcm-scrum-demo` environment keeps its names and is not managed by these templates.

## 16. M11: identity and correlation

Built in M11 prompt 3 (ADR 0017).

- **Identity.** `ActorIdentity` (object id, tenant, display name, source, authentication method) is a Domain value object; `FollowUp.Approve` and `Reject` take it. `ApprovedBy` is derived text; legacy decisions show `unverified (legacy)` and are never written. `ApprovalAuthority` (Domain) and `ApprovalGate` (Application) decide who may approve: tenant match, then the Approver role (portal) or a configured administrator or the follow-up's own roster member by `EntraObjectId` (Teams). Display names decide nothing.
- **Portal API.** In-app JwtBearer validation on every `/api` route (no Easy Auth headers); roles `Zava.Scrum.Admin`, `Zava.Scrum.Approver`, `Zava.Scrum.Reader` from `infra/entra/portal-app-roles.json`; reads need any role, changes need Admin, approve and reject need Approver. `Api.Tests` enumerates every route. The SPA sends the bearer token, shows "Deciding as", and reads the live state with `fetch`.
- **Teams.** `TeamsActor` builds the identity from `Activity.From.AadObjectId` and the tenant. `Identity:AdminObjectIds` replaces `Team:ScrumMasters`.
- **Roster.** `Member.EntraObjectId` is resolved from the mapped UPN by Graph (`GraphIdentityDirectory`), stored by `SyncRosterCommand` and re-verified when a standup is prepared.
- **Approval trail.** Append-only `ApprovalRecord`s (Cosmos `ApprovalRecords`, partition `/correlationId`, create and read only for the managed identity; a JSON-lines file locally): Approved or Rejected, then Applied with the Azure DevOps revisions. `ApplyApprovedChangesHandler` refuses without a matching record.
- **Correlation.** A W3C trace id per session, carried to follow-ups, approvals, the write, cards and the gateway log scope; every Azure DevOps attribution ends with `Correlation: zsa-<12 hex>`. `GET /api/audit/{correlationId}` returns the chain and what is missing.
- **Author attribution.** Built in prompt 5A, section 19. Identity summary: the approver is identified by Entra object id and tenant (this section), and the same identity is the Azure DevOps author through a delegated token (section 19). Team-scoped roles are section 17.

## 17. M11: multi-team and multi-organisation

Built in M11 prompt 3A (ADR 0018).

- **Registry.** `Team` has `Status`, `TeamsBindings`, ceremony flags, reserved `KnowledgeBaseIds` and `WorkSignalsEnabled`, and `EntraGroupId`; old teams load as Active. `GET/POST /api/teams`, `PUT/DELETE /api/teams/{id}` (archive), `POST /api/teams/{id}/test-connection` (organisation, project and team, current iteration, roster; names the step that failed), `GET /api/fleet`, `GET /api/ado/projects`, `GET /api/ado/teams`, `POST /api/ado/test-connection`.
- **Azure DevOps per organisation.** `IAdoClientFactory` (one client and credential per organisation, lazy, cached); `RoutedWorkItemReader`, `RoutedWorkItemWriter` and `RoutedDemoReset` pick the client from the team's settings. An organisation in another Entra tenant is refused (`X-VSS-ResourceTenant`, marked VERIFY). A write outside the team's project and area path is refused before anything is written (`WorkItemScope`, `IWorkItemScopeReader`).
- **Resolution.** `ITeamResolver` for the portal, the Teams bot (an unbound conversation gets an onboarding card and nothing else), Copilot (ask when more than one team), the scheduler and the media bot. `Team:Id` left the bot and `TEAM_ID` left the portal; `Team:*` now only seeds the default team.
- **Authorization.** Roles Admin (app role), TeamLead, Member, Reader. Memberships by Entra object id, managed by an Admin or a team lead, with an add-only Entra group sync (`GroupMember.Read.All`). `TeamAuthorizationFilter` on the `/api` group works out the team of every request and fails closed; a team the caller is not in is 404, a role that is too low is 403. Registry routes need the Admin role. A test enumerates every route.
- **Cards.** `CardRouting` sends only to the team's bound conversations; no binding, no Teams delivery (the outbox still has it).
- **Prompts.** Team override, then deployment override, then default file; the Prompts page edits the team's override and shows which is in effect.
- **Scheduler and capacity.** `RunScheduleTickHandler` runs per team in its time zone for Active teams with the daily scrum on, through `CapacityGovernor` (`Capacity:MaxConcurrentStandups` 3, `StaggerSeconds` 5, `GraceMinutes` 10). The waiting reason is on the schedule; a standup blocked past the grace notifies the recap channel.
- **Observability.** `TeamId` on the log scope, the `team.id` span tag and the `zava.scrum.usecase.duration` metric; the voice gateway scope; `GateRunner` records the team and `--only-team` filters.
- **Portal.** Team switcher (the caller's teams, remembered per person), Admin Teams page (status, next standup, last run, open follow-ups, connection health; edit, test, roster sync, schedule, bindings, ceremonies, pause, archive), onboarding wizard with project and team pickers, Admin fleet dashboard, members and roles card. A team with a broken connection shows the failing step.
- **Seed.** Payments, Identity and Catalogue (`demo/seed-ado*.json`, `tools/SeedAdo`; `--payments-only`), registered by `DemoTeamSeeder` from `Demo:Teams` (`Demo:PaymentsOnly` for the film).
- **Deployment.** The managed identity gets `GroupMember.Read.All` in `entra-post.ps1`. Both container apps have startup, readiness and liveness probes on `/healthz`.
- **Not built or not verified live.** Delegated writes (prompt 5); a second Azure DevOps organisation; `SeedAdo` against the sandbox for the two new projects; the onboarding card in a real channel; the Entra group sync; ten concurrent standups (three were run, see G9).

## 18. M11: realtime model and silence

Built in M11 prompt 3B (ADR 0019).

- **Voice default unchanged.** `VoiceLive:Model` stays `gpt-realtime-2.1` and `Voice:SilenceMs` stays 800. `gpt-realtime-2.1-mini` was measured and rejected (no latency gain, fewer flagged items raised, repeated turns); silence at 500, 650 and 800 ms could not be told apart by the test.
- **Infrastructure.** `realtimeModels` in `infra/main.bicep` is a parameterised list (default one entry) that feeds `foundry.bicep`; `requiredModels` in `infra/hooks/regions.json` is the matching preflight list. `gpt-live-1` is removed from both. The preflight adds back the quota the environment already holds, so a repeat `azd up` passes.
- **Models page.** Prices are `null` until set under `ModelComparison:Models`; the page shows the pricing tier and "Price unverified". `ModelCostEstimator.PerStandup` returns `null` when a price is unverified.
- **Measurement aids.** `[pause N]` markers in cast lines (`demo/cast/payments-standup-pauses.yaml`), a `TalkOver` timing event from the voice gateway, transcripts and talk-over counts in the gate harness results, `docs/gates/m11-p3b/bench.ps1`.
- **Known limit (fixed in prompt 4).** Sessions were one Cosmos item and passed the 2 MB limit; they are now one item each, partitioned by team (section 20, ADR 0021).

## 19. M11: delegated Azure DevOps writes

Built in M11 prompt 5A (ADR 0020). Code and hooks are built and tested; the live portal write and G3 (portal) are verified in `zavaw3`; Teams single sign-on is not.

- **Rule.** `ApproverAssertion` (Domain): the token presented at write time must be the recorded approver's (`oid` and `tid`). No token or someone else's token means no write.
- **Ports.** `IUserAssertionAccessor` (scoped, set by the Api), `IApproverTokenSource` (on-behalf-of exchange), `IWorkItemWriter.ApplyAsync(change, target, DelegatedAdoAccess, ct)`. `DelegatedAccessProvider` is used by the apply handler and the approve handlers (follow-up and plan), so a decision that cannot be written is refused before it is recorded.
- **Infrastructure.** `OboAdoTokenSource` (`OnBehalfOfCredential`, managed identity as the federated client assertion, no secret). `AdoClientFactory.DelegatedWriterFor` carries only the approver token; the managed-identity writer is gone. Reads, the scope check, the connection test and the demo reset stay on the managed identity.
- **Api.** `UserAssertionMiddleware` for the portal bearer token. `ScrumAgent` approve routes use the `sso` user-authorisation handler (`autoSignInHandlers`) and `SsoToken` to read `oid` and `tid`; no connection configured means approve is refused.
- **Bridge.** The ACS connection-string branch is removed.
- **Hooks.** Portal app: Azure DevOps `user_impersonation` consent, `api://botid-<id>`, Teams clients pre-authorised. Post-provision: two federated credentials, the `sso` bot connection, agent settings. Teams manifest `webApplicationInfo`. `verify.ps1` checks the comment author.
- **Port.** `IWorkItemWriter` has one method and it takes a `DelegatedWriteContext` (approver plus token provider); an architecture test enforces it. The Azure DevOps client types are `internal` to Infrastructure, so no other project can construct one (architecture test); an AgentHost source scan test is in place for prompt 4. A 401 or 403 from Azure DevOps becomes `ado.approver_cannot_write`; an Entra interaction, consent or Conditional Access answer becomes `delegation.interaction_required`.
- **Known limits.** Teams SSO with a managed-identity bot id and the portal app id in `webApplicationInfo` is unverified. Every approver must be a Contributor in the Azure DevOps project. `IDemoAdoReset` still uses the managed identity (it is not an approved-change write).

## 20. M11: hosted agent voice relay, session storage, pre-warm and canary

Built in M11 prompt 4 (ADR 0021, ADR 0015 S6). Prompt 4A's larger sample held
G1 point estimates, not confidence bounds. That does not erase later failed
fallback comparisons or prove candidate 5, which returned 424. Direct
remains the default; stable hosted 4 is the optional measured relay.

- **Voice relay.** `AgentHost` (`VoiceRelayHandler`, `VoiceRelay`) is a hosted agent with `/invocations_ws`. It opens Voice Live with its own identity and forwards every frame unchanged, refuses a call with no `team` (close 1008), and keeps no state. `VoiceLive:RelayEndpoint` set means the hosted route; empty means Voice Live directly. Both are real adapters of `IVoiceSessionFactory`. `VoiceSessionConfig` carries the team id and the session id (the hosted agent session id, so a reconnect reattaches to the same sandbox).
- **Sessions.** `Sessions` Cosmos container, partition `/teamId`, one item per session (`ISessionItemStore`, `SessionRepository`). `SessionMigration` (run at API start by `SessionMigrationService`) moves the old `sessions.json` document and deletes it once every session is in; demo reset clears the items.
- **State and restart.** The host holds nothing a restart could lose. `HostRestartTests` kills the host with a connection open, reads the session back from the store with its events, reconnects to the same hosted session id and continues. The gateway also reconnects on a Voice Live fault (existing `VOICE_RETRYING` path).
- **Pre-warm.** `PrepareStandupHandler` calls `IAgentHostWarmer` (hosted: one call to the agent on the standup's session id with the team id; direct: nothing to warm) and records `HostWarmUp` on the session (route, warmed, milliseconds, note), shown in `GET /api/sessions/{id}`. "Prepare now" and the scheduler's pre-brief both go through `PrepareStandupCommand`. A failed warm-up never blocks preparing.
- **Versions and canary.** Each `agent-host.ps1` run adds an immutable agent version. The platform refuses a percentage split on one endpoint, so the stable agent is pinned (`canary.ps1 -Pin`) and the canary is a second agent (`agent-host.ps1 -Canary`) that the API chooses per session and team (`VoiceRoute`, `VoiceLive:CanaryEndpoint`, `CanaryPercent`, `CanaryTeams`); `canary.ps1` has `-Canary`, `-Promote`, `-Rollback` and `-Show`. The warm-up route shows `hosted-canary` for canary sessions.
- **Greenfield.** `agent-host.ps1` (postdeploy) builds the image, creates the version and assigns the agent identity Cognitive Services User and Azure AI User on the Foundry account. `main.bicep` `voiceRoute` (Direct or Hosted) sets the API's relay endpoint. The registry already gives the project identity AcrPull.
- **Not moved (see ADR 0021).** The voice gateway, the ceremony agents, the Teams and Copilot handlers (with the 5A single sign-on) and the Activity protocol endpoint stay in the API. The host holds no Azure DevOps credential (architecture test scans `src/Zava.Scrum.AgentHost`).

## 21. M11: agent identity (5B)

Built in M11 prompt 5B (ADR 0022). The hosted agent `zava-voice-relay` signs in as the Entra Agent ID Foundry created for it (agentIdentity, no secret, named sponsor, sign-ins visible with `agentType agenticAppInstance`); `infra/hooks/agent-identity.ps1` checks this after each deploy and an architecture test keeps secrets out of `src/Zava.Scrum.AgentHost`. It is used for Voice Live and the Foundry account only; Azure DevOps reads stay on the API's managed identity and writes on the approver's token. Conditional Access and sponsor attestation are human gates (DEPLOY.md). No user account is created for the agent.

## 22. M11: Teams-native media alternative (6a/6b)

Hosted in zavaw3/westus3, not verified in a real Teams meeting (ADR 0023). `Bridge:Mode` is `Acs` by default; `GraphMedia` selects the real Windows Graph SDK host; `Simulated` retains the browser room and refuses Teams joins. Application commands and `IMeetingBridge` are unchanged. A session started as `acs` uses the configured meeting adapter.

The host validates the session/team/stored link at the API, uses authenticated Graph callbacks, maps unmixed sources to tenant-checked Entra object ids, follows own-participant lobby state, caps calls at one and leaves on the existing lifecycle event. The API's separate `/invocations_ws` checks both a secret header and the session capability before loading team facts. The existing voice factory supports Direct and Hosted; Direct remains deployed, not an obligatory hosted relay. The live portal displays native call state and guests; guest audio is not sent to the model. Unmatched, duplicate or cross-team object ids receive no member facts or approval rights.

**Non-recording boundary:** the ephemeral gateway has no mediator, repositories or executable tools. It saves no PCM, transcript, spoken update, voice-derived attendance or follow-up proposal. Parking, live participants and the conversation are memory-only. Existing metadata-only lifecycle cards may still be emitted on completion, but contain no learned meeting updates. ACS and the Simulated Room retain the complete saved follow-up/approval demo. Recording in a future feature needs successful `updateRecordingStatus` first and the applicable recording requirements.

Barge-in clears queued audio, drops late response output and truncates at the host's played-frame acknowledgement. Shared floor-control exceptions for opening/recap/absent announcements are unchanged. A bounded transport reconnect retains the real call but restarts the ephemeral conversation. Prompt 7 adds native board sharing; live visibility/encoder acceptance remains open.

Opt-in Bicep/azd adds a public four-vCPU Windows Server 2022 D4s_v5 host (D3_v2 was unavailable), HTTPS/TCP 8445, certificate vault, artefact storage, scoped roles and SYSTEM startup task. Let's Encrypt HTTP-01 uses temporary TCP 80; the Azure DNS-label certificate expires 2027-02-24 and monthly hook reruns are required for renewal. The separate calling app's `Calls.AccessMedia.All` grant is verified; meeting join still needs organiser RSC `Calls.JoinGroupCalls.Chat`. Daily SDK-age CI enforces newest-or-within-three-months. Fixed added cost is about USD 0.39415/hour, plus variable operations/storage/egress.

Real SDK/HTTPS readiness reports no recording and cap 1, including after an idle VM reboot without manual task start; the task runs as SYSTEM with an at-startup trigger. The aligned direct native-package reference and explicit Graph v1.0 service URL fix startup defects that managed unit tests did not exercise. This does not establish native call capacity or a Teams media handshake.

Native G1/G2/G6 and real-human barge-in remain n=0: catalogue upload is now completed by the operator, but no real meeting link is stored, Daniel/Maya lack roster Entra ids, and organiser installation/RSC/voices are outstanding. Synthetic probes passed 90/90 tagged frames and dispatched stop 29.6 ms after threshold (n=1), not SDK attribution or live audio. Opening-state 600 ms speech does not cancel, so unconditional barge-in fails synthetically. See DEPLOY.md for exact human actions and the gate table for fallback measurements.

## 23. M11: content-free observability and evaluation

The persisted session correlation is the W3C trace id. Verified owning-team
metadata follows native sampled PCM frames, API forwarding, the optional hosted
relay, decision calls and approved Azure DevOps writes. A different incoming
portal trace is linked, not mistaken for the session trace. Authenticated hello
metadata binds browser/bridge requests to it. PCM remains unchanged; at most
one actual native frame per second receives a producer span. Native facilitation
still saves no meeting data and has no decision/approval/write path: do not
claim one live native-frame-to-write chain or a vendor Voice Live span.

Shared BCL `SessionTelemetry` avoids a writable Application dependency in the
media host. API/agent/media use the existing Azure Monitor exporter; the Node
bridge emits explicit HTTP spans, not automatic URL/SDK/log-body traces.
`Telemetry:CaptureContent=true` is refused. MAF sensitive data and GenAI content
capture are disabled, non-allowlisted span tags are removed and application
log bodies are not exported to Insights. Immutable SDK events cause a span
to be suppressed, with a team privacy counter, rather than exporting its text.
Raw browser console/error and model exception bodies are not logged.
Console/container diagnostic logs are
separate, not a new content archive. Metrics carry the team and fixed labels:
hop/use-case duration, join accepted/refused and established connection events,
turns, barge-in cancellation, approval and refusal counts.

Gate timing events retain trace/span ids. Turn and phase spans use the already
measured intervals; the decision call has its actual enclosing span. A shared
Insights workbook is deployed by `observability.bicep` and the scoped existing
environment hook. It filters one team for hop latency, join events and the
approved-decision-to-write chain. Connection events are not unique-call rates;
missing data is never passing evidence.

Live check2026-11-27: the saved four-panel workbook matches source, with16hop
rows and211session-hop samples under trace
`e48b795beab7972d69f7145550a925f0` across API and bridge, decision/model SDK,
approval and one actual write. Mandatory verification passes8/8; the exact
approver-authored revision52 audit and a separate unapproved403/refusal are
checked. The433team-tagged records have18dimension keys, zero prohibited key
occurrences and no Insights log/exception rows in the bounded window.
API/bridge8859a7a revisions84/9 and nativee8c95b6 are live. Candidate hosted5
failed readiness/protocol424 testing and was not promoted; stable4 remains.
This closes the API/bridge live-telemetry gap, not native/hosted/vendor acceptance.
Before/after total p50/p95 is3200/5600ms(n10) versus3398/4707ms(n12),
zero talk-overs. The p50+6.19% point estimate fails the historical5% tolerance.

The repeatable runner and daily non-blocking CI use 72 authored synthetic
goldens, not employees or real work items. Final real model n=72: intent/items
98.6%, comment kinds95.8%, dates75% on n=8, four rules fallbacks, no failures,
zero escaped ids and0/36 rejected candidates (the adapter bounds candidates).
Threshold0.7 precision94.5% n73; threshold0.6 precision98.5% n68, with648binary
and72intent probabilities and no missing answers. Rules n72:95.8%/97.2%/55.6%,
dates0% n8; no invented probabilities/comment capability. The release command
fails dates; unchanged code takes the first spoken date phrase.
`docs/gates/decision-M11` retains the real report and golden hash.

Foundry live quality/safety/groundedness scoring remains not_run, n=0: its
required input/output messages conflict with content capture OFF. The checked-in
rubric states that restriction, rather than enabling an invalid scheduled
evaluation. The daily decision report is synthetic, not live quality scoring.
Knowledge and Work IQ remain unbuilt/unrun. ADR0028 and G7 record the measured
deployment evidence and privacy deviation; no default adapter or Domain rule
is changed to make a gate pass.

The rules CI report is wired, but actual repository model/OIDC variables are
unset; real-model CI and default-branch scheduling are not enabled by a draft
PR. DEPLOY.md names the identity-owner prerequisite.
