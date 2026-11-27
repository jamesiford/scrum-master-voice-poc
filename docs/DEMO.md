# Demo runbook

## Final M11 rehearsal boundary (26 November 2026)

Rehearse the saved-follow-up story with **Acs / Share / Direct**, then the
Simulated Room fallback. Do not switch to native media for the approval film:
it is non-recording and cannot persist spoken updates or proposals. Hosted
stable 4 is an optional previously measured relay; candidate 5 is unpromoted
after readiness/protocol 424 failure (completed paired samples n=0).
Knowledge and Work IQ are not built and the demo does not rely on either.

The final real Teams meeting and Copilot rehearsal are **not run, n=0**.
Scout has completed both zavaw3 catalogue uploads and delegated IQ consents.
An organiser and consenting users must complete DEPLOY.md's installation,
roster, meeting/RSC and Copilot gates. Record the actual session id, full
correlation id, proposal/approval records and Azure DevOps revisions from
each run; do not reuse the scripted trace as real meeting/Copilot evidence.
Existing measured scripted evidence is session
`bac27cf7-8a0e-40e7-9aec-92c779f23594`, correlation
`e48b795beab7972d69f7145550a925f0`, approver-authored revision 52,
with positive n=1 and separate unapproved/refused n=1.

For each take: choose the intended team, reset application state, re-seed
only its approved sandbox project, verify the roster and cast configuration,
prepare a fresh session, run the standup, approve one follow-up and reject
another, then inspect the audit API and the actual Azure DevOps comment.
The Live console has aggregate latency/events; the workbook has per-hop
measurements. A dedicated audit portal page, Live per-hop counter panel,
readable names for every participant and an atomic roster/cast/Sprint 43
re-seed remain product follow-ons, not features delivered by this docs step.
Do not edit the Presenter Pack or fabricate UI/evidence for the film.

### G8 recovery evidence is not a real Teams rehearsal

Only zavagf/eastus2 is rebuilt. First empty up passes1828.77s and mandatory
verify8/8 passes344.83s; actual approved auditrevision55 is positive n1.
Scripted standup3251/3682ms p50/p95,n9, does not replace the real meeting,
permission-negative, board-visibility or native attribution matrix.
First-cycle native trusted HTTPS/no-recording/cap1/1280x720renderer and SYSTEM
startup task were ready, not proof of a stored-link Teams join. Second empty cycle
is incomplete after intervening purge passed1639.20s with complete absence
readback. New zavagf package publication was
attempted and refused403 for OAuth catalogue scopes; use DEPLOY.md's exact
administrator upload action. Completed zavaw3 uploads remain complete.
The attempted second deployment lost the native opt-in through an
input/output collision and is not counted as full G8 evidence. The corrected
input-only template requires a clean retry; do not substitute a smaller
deployment's optional native SKIP for actual hosting verification.
It also exited1 after1036.69s with a hosted-version workspace-identity409.
The tool's bounded service-directed recovery is not a completed second
deployment or a native/live acceptance sample.
The full native-enabled retry failed1229.5552s after three409 creations/two
successful reads. Independent media completion then failed403
Authorization_RequestDenied, request07d95ae3-fd0a-4479-9442-9e7d14dd3abf.
G8 is human-blocked,1/2 complete cycles; the allocated current native VM is
not bootstrapped. Independent legal pages/messaging package generation
passed without a catalogue authentication retry. Follow DEPLOY.md's exact
administrator/support actions; retain the untouched zavaw3 demo.
Final independent Direct verify passes8/8 in361.4580s with an actual
approver-authored revision58/audit,2821/4189ms,n10,talkovers0.
It prints native MEDIA-TLS readiness not verified and does not credit
cycle2. First-to-final tail +13.77% exceeds10% tolerance across different
windows; do not call it a native/paired acceptance sample.

## Work IQ signals readiness (M11 8B)

**Not available for the demo.** Only the consent/evidence contracts and
unregistered read boundary are built. No connection/Revoke page, channel
enrichment, absent-member chase suppression, helper confirmation or cited
review-backlog draft is enabled. Do not present synthetic contract fixtures
as Work IQ results or M9c closure. Keep Acs/Share/Direct and Simulated Room.

DEPLOY.md WORK-SIGNALS-CONSENT / WORK-SIGNALS-BILLING /
WORK-SIGNALS-FIXTURE name the administrator, billing owner and team lead
actions. After they complete, sign in as the real owner through portal OBO,
discover the live schema and prove exact channel/meeting-only reads before
building any supported use. Show source/title/link/time for real evidence;
revoke and repeat to verify the visible **No signals available** banner.
Run the disabled-team, other-team, expired-token and private-source negative
cases and review proposal approval guard. Current live G11 samples are n=0,
not a privacy pass. See ADR 0027 and WORK-SIGNALS-PRIVACY.md.

## Foundry IQ readiness (M11 8A)

**Not available for the demo.** Prompt 8A stopped before its mandatory
caller-OBO permission spike; no Knowledge page or standards chat skill is
shipped. Do not present the prompt editor, repo working agreements, existing
Story Writer or ordinary escalation cards as cited knowledge retrieval.
Continue using the existing Acs/Share/Direct and Simulated Room paths.

The8A-r actual readback verifies both delegated retrieval scopes and matching
declarations on the zavaw3 portal app; KNOWLEDGE-CONSENT is complete.
the standards/site owner must still identify an approved
corpus and a restricted document with real positive/negative consenting
users. Exact actions are DEPLOY.md KNOWLEDGE-CONSENT / KNOWLEDGE-FIXTURE.
On resume, prove ten real questions through the knowledge base MCP endpoint
and caller-OBO permission trimming before integration; evaluate at least
forty source-labelled questions per knowledge base. Show answer citations
and explicit "I don't know" for a genuine no-source result, never guesses.
Current retrieval, leakage and latency sample sizes are all n=0 (ADRs 0026/0033).
The admin's measured OBO token exchange and Graph identity read n1 each are
prerequisites only, not cited knowledge or a permission-trimming demonstration.
Redo existing-zavaw3 verification passes8/8 with real approver/auditrevision59;
Direct2969/4875ms,n12,talkovers0. Native hosting readiness does not establish
native meeting acceptance; inherited failed gates remain visible.
For the separately authorised G8 proof use only zavagf/eastus2 and DEPLOY.md's
exact Gen2/purge parameters; never reset or tear down the running demo zavaw3.

## Before the demo (5 minutes)
Placeholders: `<tenant-id>` and `<your-organisation>` come from your user secrets (`Azure:TenantId`, `Ado:Org`) or the azd environment (`AZURE_TENANT_ID`, `ADO_ORG_URL`); see README.
1. `az login --tenant <tenant-id>`.
2. Open the portal (local: `dotnet run --project src\Zava.Scrum.AppHost --launch-profile http`, Aspire dashboard at http://localhost:15888; Azure: the API URL printed by `azd up`).
3. **Live console** tab > **Reset demo** (clears sessions, follow-ups and cards; Azure DevOps is not touched).
4. **Team** tab > check the sprint goal. **Roster** tab > **Sync roster**.
5. Re-seed the approved Payments sandbox with `dotnet run --project tools\SeedAdo -- --tenant <tenant-id> --org https://dev.azure.com/<your-organisation> --payments-only`. It resets seed-tagged items in place and moves Sprint 42/43 dates relative to today; it does not remove previous comments, extra retro tasks, meetings or reset the app roster/cast. Check actual ids; never assume a fresh project starts at work item 1.
6. Teams: use the environment-specific **1.1.0** package in `appPackage\dist`, complete the rollout below and bind its conversation to this team. Messaging the bot alone does not authorise team card routing.

## Microsoft 365 Copilot rollout and five skills (M11 8)

Use only the zavaw3 package; do not update the old mcm application.
`azd env select zavaw3` then `pwsh infra\hooks\teams-app.ps1` builds
`appPackage\dist\zava-scrum-agent-zavaw3-1.1.0.zip` with the existing
zavaw3 bot id `bed81a44-b97d-4687-9ad3-c4cf546f6689`. Validate with
`atk validate --package-file appPackage\dist\zava-scrum-agent-zavaw3-1.1.0.zip --telemetry false`.

1. A Teams administrator opens **Teams admin centre > Teams apps > Manage
   apps**. For an existing Zava Scrum Agent, open its details and **Upload
   file** to replace 1.0.5 (or the earlier 1.0.4) with 1.1.0; do not create
   another app or change its id. If it was never published, use **Upload new
   app**. Review/approve the submission and its permissions under existing
   policy. This retains meeting `OnlineMeeting.ReadBasic.Chat` RSC; upgrading
   from 1.0.4 introduces that permission and requires approval/reconsent.
2. Each user accepts **Update** if Teams asks for consent, then reinstalls or
   reopens the updated app using its install link. An administrator cannot
   force a consent-required update on a user or consent for them. Current
   Learn says one user's consent can propagate to existing chats/channels/
   meetings they manage; it is not necessarily a separate update per context.
   A team-owner update does not grant that user's consent in other contexts.
   Accept any remaining context-specific prompt. Do not change tenant policy.
3. Open **Microsoft 365 Copilot** in Teams or
   [m365.cloud.microsoft](https://m365.cloud.microsoft/), select the Zava Scrum
   Agent in the agent picker, and confirm its five starters. Sign in once in
   that context with the Bot Service OAuth prompt. OBO uses the same dedicated
   portal app and existing Azure DevOps delegated permission; an Entra admin
   consent is not a grant of Azure DevOps project access.
4. Try the five questions below. A user in multiple teams repeats
   `team <id>: <question>` after the team-choice response; no team is guessed.
   The answer is short, contains code-computed figures and cited item ids,
   and offers **Open #id in Azure DevOps** on a read-only card. Streaming uses
   a checking-access update followed by the validated answer and final card
   where the channel supports it.
5. Use a second consenting team member with no Azure DevOps project access.
   Each skill must explain that Azure DevOps refused that person's access,
   without item ids, titles, figures or a source card. Also test an account
   with no team membership and one with multiple memberships. Record samples
   and elapsed time, not just a screenshot of the manifest.

| Starter | What to demonstrate |
| --- | --- |
| What is blocked on my team? | Only readable current-sprint blockers |
| What did I commit to? | The asker's unretracted saved commitments, linked only to readable current-sprint items |
| What is at risk in this sprint? | Existing Domain at-risk/overdue flags, not a model forecast |
| What is the standup status today? | Team-local date and lifecycle status, without native meeting content |
| Show my items and their state. | Actual Azure DevOps assignee, not demo owner tags or display-name guessing |

Personal questions require a uniquely verified Entra roster mapping; ask the
team lead to fix it if refused. The synthetic cast is not a substitute for that
identity. Native non-recording runs do not create saved commitments, so use
existing authorised ACS/Simulated history if demonstrating that skill.
An unavailable/ungrounded model produces an explicitly labelled code answer.
Other Teams reporting, planning, follow-ups and approvals are unchanged.

Live G5 remains unrun until actual consenting users and fixtures are available.
The operator has completed the zavaw3 catalogue upload. The earlier Graph
catalogue read was refused for missing AppCatalog scopes after the first
administrator window expired; DEPLOY.md records the completed action. Current
references: [enable Copilot](https://learn.microsoft.com/microsoftteams/platform/m365-apps/enable-agent-app-copilot),
[custom app updates](https://learn.microsoft.com/microsoftteams/teams-custom-app-policies-and-settings#update-a-custom-app-using-teams-admin-center),
[user update consent](https://learn.microsoft.com/microsoftteams/apps-update-experience).

## Scripted run (Simulated Room, about 3 minutes)
1. **Sessions** > **Prepare now** > open the pre-brief with **View** (burndown, flags, questions).
2. **Simulated Room**. Tick **Autopilot (cast plays my lines)** in the room, then press **Start standup**, to let the demo cast play the three-person standup (Daniel Park, Maya Lin and Admin).
3. Watch the **Live console** tab: floor, current speaker, timebox, parking lot, latency. Controls: **Begin**, **Skip speaker**, **Mute agent**, **Unmute**, **Take over**, **Hand back**, **End standup**.
4. When the standup ends: **Follow-ups** tab. The page shows **Deciding as** your signed-in account (your Entra identity is what is recorded). Press **Approve and write** on one, **Reject** another. The comment in Azure DevOps ends with `Correlation: zsa-...`; open `/api/audit/zsa-...` (or the full trace id on the session) to see the chain.
5. Open Azure DevOps and show the "Approved by ..." comment on the approved item and nothing on the rejected one.
6. **Reports** tab: sprint summary, monthly report, then ask "How is attendance?" (the same answer works in Teams chat).
7. **Planning** tab: load per person, carry-over, not-ready items and suggested owners.
8. **Retrospective**, **Story Writer** (pick a parked topic) and **Models and email** tabs for the stretch features.

## Real Teams meeting

For the M11 native-media alternative and its non-recording limitation, see "Teams-native media (6a/6b)" and "Native board and meeting events (7)" below. The existing ACS path remains the default for the full follow-up demo.
1. Create a Teams meeting in the demo tenant and copy the join link.
2. **Live console** tab > pick a prepared session > paste the link > **Start in Teams**.
3. Admit "Zava Scrum Agent" from the lobby. The bridge status line shows Joining, Lobby, Connected, Live.
4. Roster members who join are matched by name (a Teams user called "System Administrator" matches the roster member "Admin"). Anyone who does not join is marked absent. The standup begins 8 seconds after the first match; use **Begin** if it does not.
5. The agent shares the standup board as its screen (flagged-item agenda first, then each speaker's items). Share status appears as Bridge events.
6. Anything that goes wrong is shown on the Live console tab as a Bridge event (invalid link, lobby denied, removed).

### Things worth showing in a live run
- Ask "Scrum Agent, is that one overdue? What was the due date?" mid-update.
- Ask "What am I supposed to report on?" on your turn.
- Have a colleague say "Daniel stepped out, he'll be back in a few minutes": he is skipped and circled back to at the end.
- Stay silent when called: the agent records present-but-no-response (not absent).
- Correct yourself ("scratch that, I meant...") and show the removed blocker in the follow-up.
- Have one person say "I'll give Admin's update": the follow-up still needs Admin's approval.

## If something fails
| Symptom | Fix |
|---|---|
| "Voice Live is unavailable" | `az login` again; check the Foundry deployments exist. |
| Standup is slow to answer | Measured p50 is 3 to 4 seconds (22 Nov), p95 about 5; say the number on screen. |
| `ado.auth_failed: No Azure credential available` on a local run | `DefaultAzureCredential` stops at the managed-identity probe on some machines. Start the stack with `$env:AZURE_TOKEN_CREDENTIALS='AzureCliCredential'` and `az login`. |
| Cards missing in Teams | Message the bot once; cards are always in **Follow-ups** > **Show card outbox**. |
| Recap or follow-ups missing after the standup ends | Fixed and verified in a real Teams meeting (ADR 0016: the hang-up cancelled the end-of-standup handlers). If it recurs, look for `StandupEnded raised for session` and `Sent N follow-up card(s)` in the API log for the session id. |
| Teams join fails | Run the Simulated Room instead; it is the fallback for any live demo. |
| Approve shows a write error | The approver must have Azure DevOps project write access and a valid OBO sign-in/consent. Fix that person's access, sign in again and retry as the same approver; there is no managed-identity writer fallback. |

## M9 features (ceremony packs, unblock loop, carry-over)
Needs `az login`, the stack running and, for packs, a reachable Azure DevOps project. Nothing below writes to Azure DevOps until a person approves.

1. **Re-seed** Azure DevOps (`dotnet run --project tools\SeedAdo -- --tenant <tenant-id> --org https://dev.azure.com/<your-organisation>`) so the at-risk task "Prepare reconciliation report for audit" exists.
2. **At-risk flag:** **Sessions** > **Prepare now** > **View**: Daniel Park's brief flags that task. In a standup the agent asks about overdue items first, then at-risk, then stale.
3. **Unblock loop:** run a standup (Simulated Room), say a blocker ("I am blocked on the gateway timeouts"), and answer "Who can help unblock this?" with a teammate's name. After **End standup**, **Follow-ups** shows an unblock request for that teammate (and the outbox shows an email when `Features:EmailFollowUps` is on).
4. **Carry-over:** run the standup again after a day (or a second standup): the pre-brief lists the still-blocked item with its unblocker, and the agent opens with "Yesterday you said you'd finish ...". A commitment still open two standups later raises a Scrum Master escalation.
5. **Review and retrospective packs** (no portal button yet):
   `curl -X POST http://localhost:5080/api/ceremonies/SprintReview/pack -H "Content-Type: application/json" -d "{\"teamId\":\"team-payments\"}"` (also `Retrospective`). The card appears in **Follow-ups** > **Show card outbox** and in Teams. Press **Approve and write** (you are recorded from your sign-in): comments are added to the completed stories, or the retro action tasks are created, each with "Approved by {name} in the portal, proposed by Zava Scrum Agent from the {date} {ceremony}."
6. **Recap routing:** add the bot to a team channel and it becomes the recap channel (`PUT /api/teams/team-payments/recap-channel` sets it by hand). The recap then goes to that channel and the meeting chat instead of every conversation.
7. **Graph email (optional):** set `Features:EmailFollowUps=true`, `Email:Mode=Graph` and `Email:Sender=<agent mailbox>`; absent, no-response and unblock members receive an email as well as the card.

## Sprint planning (M9a)
Needs the stack running and a seeded sandbox (re-run `SeedAdo` first: it adds the next iteration "Sprint 43" and 10 backlog items, and puts back anything a plan moved). The velocity history is the illustrative fixture and the portal says so.

1. **Reset:** **Live console** > **Reset demo** (clears packs, sessions, follow-ups and cards; Azure DevOps is not touched).
2. **Planning** tab. Optional: set **Maya Lin**'s availability to 50 and leave the field; her capacity halves the next time you prepare the pack, and the plan warns if she is overloaded.
3. Press **Prepare pack**. The page shows the draft goal, the velocity range, capacity bars per person, the proposed selection (carry-over first, then ready backlog items in order, suggested owners marked), **Not planned** with a reason on each excluded item (no story points, no acceptance criteria, too large, beyond capacity, not started), and a warning when the plan is over- or under-committed.
4. The card **Sprint planning: Sprint 43** arrives in **Follow-ups** > **Show card outbox** and in Teams, with **Approve plan** and **Reject**.
5. **Reject** first: nothing changes in Azure DevOps. **Prepare pack** again.
6. Untick one item if you like, verify **Deciding as** and press **Approve plan** (or use the Teams card after SSO). The recorded identity comes from sign-in, not a typed name. Azure DevOps shows the selected items in `Sprint 43`, suggested owners and approver-authored attribution.
7. Chat: in Teams, a Scrum Master (an Entra object id in `Identity:AdminObjectIds`) can type "prepare sprint planning" to the bot; anyone else is told only the Scrum Master can.
8. Re-run `SeedAdo` to put the items back for the next run.

## Retrospective by voice (M9b)
Needs the stack running, a seeded sandbox and an Azure Speech resource for the cast voices (as for the scripted standup). About 5 minutes in the Simulated Room.

Final validation observed one omitted circle-back question, leaving Daniel's
self-correction unspoken; the effective default prompt was correct. Initial
retrospective5/6, then one unchanged focused retry6/6 in267.29s. This is a
live model reliability risk, not a removed assertion or a deterministic fix.
Retain that evidence when rehearsing; scripted success does not prove Teams
or native acceptance. See ADR0030 and `gates/M11-gates.md`.

1. **Reset:** **Live console** > **Reset demo**.
2. **Retrospective** tab > **Prepare retro**. A card **Run it by voice** appears.
3. Press **Start in Simulated Room**. The room opens in a new tab: press **Start standup** after ticking the autopilot box (or open the room link with `&autopilot=1`, as the e2e does). The cast (`demo/cast/payments-retro.yaml`) plays three people: Daniel steps away and is circled back to, Maya gives one unclear note so the agent asks "Is that something to start, stop or keep doing?", and Daniel corrects himself inside one answer.
4. Watch the board in the Teams meeting's shared screen (three columns that fill live, then the actions), or the **Live console** for the floor and timer. After the round the agent reads the themes back, asks for an action from each person and confirms the owner, then closes with a recap.
5. The Scrum Master gets a **Retrospective actions** card (**Follow-ups** > **Show card outbox**, and in Teams) and a summary in the recap channel. Press **Reject** on a copy of the card first if you want to show that nothing is created.
6. Verify **Deciding as** in **Follow-ups** and approve. Azure DevOps shows one Task per action tagged `retro-action` in `Sprint 43`, with the owner tag and authenticated approver attribution; no typed name supplies approval authority.
7. In a Teams meeting: **Retrospective** tab > **Prepare retro**, paste the meeting link and press **Start in Teams** (not checked live yet; the standup run is the reference). `Retro:Minutes` sets the timebox and `Retro:Format` the questions.
8. Delete the created tasks and re-run `SeedAdo` before the next run. Automated: `npm run e2e:retro --prefix src/meeting-bridge` with the API and bridge running.

## Sprint calendar (M9d)
Needs a seeded sandbox and the deployment's mailbox-scoped Exchange grant.
An Exchange administrator runs `infra\hooks\exchange.ps1 -Mode Grant` for
the intended environment; it grants the existing managed identity
Application Calendars.ReadWrite and Application Mail.Send on the organiser's
mailbox through Exchange RBAC for Applications, then applies Graph settings.
Do not create another calendar app/secret or touch mcm. Configure actual roster
emails and sync their distinct Entra ids before proposing meetings.
Without the grant/organiser, use the `.ics` fallback; do not claim live invites.

1. **Reset:** **Live console** > **Reset demo**.
2. **Schedule** tab, **Proposed sprint calendar**: the plan for the next sprint (daily standups, planning on day one, the retrospective on the last day; a review only when `Team:HasSprintReview` is true). Press **Download .ics (fallback)** to see the file.
3. Press **Propose calendar**. The card **Proposed sprint calendar: Sprint 43** arrives in **Follow-ups** > **Show card outbox** and in Teams, with **Approve** and **Reject**.
4. **Reject** first: nothing is created. **Propose calendar** again.
5. Verify **Deciding as** and approve (or use the Teams card after sign-in). Inspect the actual created count and stored join URLs; a Graph error is not successful invitation delivery. Record failures and use the documented calendar fallback.
6. The **Schedule** is now enabled in Teams mode with each standup's join URL stored. At the next standup time the scheduler prepares the session ten minutes before and joins the calendar meeting one minute before, with no pasted link. (To try it today, change the schedule's start time to a few minutes from now and make the first approved meeting start then.)
7. Delete the meetings from the agent's calendar before the next run.

## Gate runner (latency and traceability)
M11 9 timing events carry the persisted session trace id and turn span id. Open
**Zava content-free observability** under the environment's Application Insights
Workbooks, select the actual team and the last hour. Inspect hop latency and the
same operation id's approval/write chain. An accepted join is not an established
call; native live samples remain absent until the organiser/roster gates pass.
Never enable content capture for a demo. The72 synthetic decision report and
blocking release command are in `demo/evaluation/README.md`; G7 currently fails
spoken dates, not item leakage. Rules do not supply model probabilities or
comment kinds. Foundry live quality, knowledge and Work IQ remain unrun.

Checked live2026-11-27: select `team-payments` and the recorded trace
`e48b795beab7972d69f7145550a925f0` to see API/bridge/model hops and the
approved write. Its16hop rows/211session-hop samples,12turn events,
one approval/write, one refusal and one rejection are measured, not placeholders.
Mandatory checks pass8/8. Before/after total p50/p95 is3200/5600ms(n10)
versus3398/4707ms(n12), zero talk-overs; p50+6.19% fails the historical5%
point-estimate tolerance. Hosted candidate5 failed the readiness/paired probe
(424); keep Direct and stable hosted4. See `docs/gates/observability-M11.json`.

For the bridge's automated `e2e` and `e2e:retro` checks, run the local API and
bridge with a dedicated Json data folder and the intended environment's Foundry,
Azure DevOps and Speech configuration. Leave `VoiceLive:Endpoint` empty to derive
the websocket URL from `Foundry:ProjectEndpoint`; an explicit endpoint must use
`wss://`, not HTTPS. Set `E2E_API` when using a non-default local API port.
If valid `az` tokens exist but local `DefaultAzureCredential` stops at IMDS,
set `AZURE_TOKEN_CREDENTIALS=AzureCliCredential` in the local API process only,
as documented by [Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains#use-a-specific-credential).
Do not change deployed managed-identity credentials for a local test.

`tools/GateRunner` plays the Simulated Room cast (`demo/cast/payments-standup.yaml`, autopilot) through the real API in Edge and records, for every agent reply that follows someone speaking, the time from the end of their speech to the first audio, split into: speech end to transcript, analyser, decision model, orchestration (floor control, the wait for silence, the reconcile) and Voice Live first audio. Each run is saved as JSON; `summary.md` gives p50, p95 and n per hop (nearest-rank, no interpolation) and the build id. Every later change re-runs it and pastes the summary into the pull request and `docs/gates/M11-gates.md`.

1. Start the API and the bridge (`dotnet run --project src\Zava.Scrum.Api --urls http://localhost:5080`; `npm run start --prefix src\meeting-bridge`). The configuration under test is set on the API: the default is the decision model (`UtteranceAnalysis__Engine=Decision`); for the rules-only run set `UtteranceAnalysis__Mode=Rules` before starting it.
2. `dotnet run --project tools\GateRunner -- --label decision --runs 3 --out docs\gates\baseline-M10` (use `--label rules` for the other configuration; `--build <id>` names a deployed image tag, default is the git commit).
3. `dotnet run --project tools\GateRunner -- --summarise docs\gates\baseline-M10` rebuilds the summary from the saved JSON.
4. `--check-audit` runs the traceability check (G3) after each run: it proposes and approves one change through the API as the caller, waits for the write, and fails unless `GET /api/audit/{correlationId}` shows the session, the approval record with the approver's object id and the write record with an Azure DevOps revision. The caller needs the Approver and Reader roles (a local Development API gives every role); the check writes one comment on work item 1.
5. The runner prints `docs/gates/teams-rehearsal.md` at the end: a ten-step checklist a person ticks in a real Teams meeting.

To drive a deployed API behind Easy Auth, pass `--api <url> --token <bearer>` (or `GATE_TOKEN`); the bridge room must be reachable from the machine running the runner. A run takes about six and a half minutes.

To stand the whole environment up from nothing, and to check it afterwards, see `docs/DEPLOY.md` (`azd up`, then `pwsh infra/hooks/verify.ps1`). Run the gate runner against the deployed API with a token from `az account get-access-token --scope api://<PORTAL_CLIENT_ID>/access_as_user`; `docs/gates/baseline-M10-deployed/` holds the deployed M10 baseline.

## Teams, onboarding and roles (M11 prompt 3A)
The portal serves every team. The switcher in the sidebar lists only the teams you belong to and remembers your choice; an Admin sees all of them and gets an **Administration** group (Fleet, Teams, Onboard a team).

- **Roles.** Admin (the Entra app role) manages the registry and sees every team. In each team a person is a **team lead** (approve, schedule, edit prompts, manage members), a **member** or a **reader** (read only). A person in no team sees "You are not in a team yet". Add people on the Team page by Entra object id, or set an Entra group on the team and press **Sync from Entra group**.
- **Onboard a team.** Administration > Onboard a team: name and id, the Azure DevOps organisation (press **Load projects**, then pick the project and team), **Run test** (every step must pass), create and sync the roster, schedule, an optional Teams channel id, then **Enable**. The team stays a draft until the last step. Adding a new Azure DevOps organisation first needs the human gate in `docs/DEPLOY.md`.
- **Bind a Teams channel.** In a channel the bot is installed in but no team is bound to, the bot answers with a card and nothing else. An Admin or that team's lead types the team id and presses **Bind**. Or add the channel id on the team's edit panel.
- **Three demo teams.** `dotnet run --project tools\SeedAdo` seeds Payments, Identity and Catalogue (one project each). The API registers them from `Demo:Teams`. For the film set `Demo:PaymentsOnly=true` and seed with `--payments-only`: one team, as before.
- **Prompts.** The Prompts page edits the selected team's override and says whether the default, a deployment override or the team's own is in effect.
- **Pause and archive.** Pause stops a team's ceremonies from starting; archive makes it read-only. Neither deletes anything.
- **Capacity.** If more standups are due than the host allows, the extra ones wait and the schedule shows why; after the grace the team's recap channel is told.
- **Three at once.** `dotnet run --project tools\GateRunner -- --label g9 --teams team-payments,team-b,team-c --api <url> --token <bearer> --out docs\gates\g9 --check-audit` runs three Simulated Room standups together (it registers the extra teams as copies of the first). `--summarise <dir> --only-team <id>` filters the summary by team.

## Comparing a realtime model or the silence setting (M11 prompt 3B)
`docs/gates/m11-p3b/bench.ps1` sets the model, the analyser mode, the silence setting and the cast on the deployed API and bridge, then runs the gate harness with three Simulated Room standups at once (one per team) and saves the results. Example: `pwsh docs/gates/m11-p3b/bench.ps1 -Configs rt21-model,rt21-rules`. The valid analyser modes are `Model` (the decision model reads each utterance) and `Rules`. `Demo__CastFile=demo/cast/payments-standup-pauses.yaml` on the bridge plays the cast with mid-sentence pauses (`[pause N]` in a line). Compare only batches run next to each other: the same configuration moved by about 25 % between time windows. `GateRunner` prints the number of times the agent spoke over a person and the transcript pieces per run. If `prepare` returns 500 after many runs, see the Cosmos item limit in `docs/gates/m11-p3b/README.md` and use `POST /api/demo/reset`.

## Approving as yourself (M11 prompt 5A)

An approval is written to Azure DevOps as the person who approved it, so the revision shows their name, not the service identity.

1. Sign in to the portal as a person who holds the Approver role and is a Contributor of the Azure DevOps project, and approve a follow-up. In Azure DevOps the comment's author is that person, and the text ends `Correlation: zsa-<id>`.
2. In Teams the first Approve shows a sign-in prompt once (single sign-on); press Approve again. If Teams single sign-on is not set up, Approve says so and nothing is written.
3. If the write fails because your token was not accepted (consent, multi-factor) or you are not in the project, the follow-up stays approved and unapplied with the reason; sign in again and press Retry. Someone else pressing Retry is refused.
4. `pwsh infra/hooks/verify.ps1` fails unless the comment author is the approver.

### Entra sign-in log queries for an approval (5A)

An approval produces two sign-in entries for the approver's account: the interactive sign-in to the portal (or Teams single sign-on), and a non-interactive one for the on-behalf-of exchange.

- Entra admin centre > Monitoring > Sign-in logs > **User sign-ins (non-interactive)**: filter Application = `Zava Scrum Portal (<env>)`, Resource = `Azure DevOps` (499b84ac-1321-427f-aa17-267ca6975798). The row at the approval time is the exchange; a failure code here (50076 multi-factor, 53003 Conditional Access, 65001 consent) explains a refused approval.
- Log Analytics (if sign-in logs are exported), approver and exchange:

```kusto
union SigninLogs, AADNonInteractiveUserSignInLogs
| where TimeGenerated > ago(1h) and UserPrincipalName =~ "<approver upn>"
| where AppDisplayName startswith "Zava Scrum Portal" and (ResourceDisplayName == "Azure DevOps" or ResourceDisplayName startswith "Zava Scrum Portal")
| project TimeGenerated, Type, AppDisplayName, ResourceDisplayName, ResultType, ResultDescription, ConditionalAccessStatus, CorrelationId
| order by TimeGenerated desc
```

## Teams-native media (6a/6b)

This alternative is hosted in zavaw3/westus3 but not live-verified in Teams, and is not the full saved-follow-up demo. Trusted TLS and dedicated media consent are automated/verified. Before native acceptance, complete the catalogue upload, stored real calendar link, real roster identities and organiser RSC actions in DEPLOY.md. Native G6/G2/G1 and real-human barge-in currently have n=0.

After those prerequisites, persist the choice and apply it to the existing API (an image-only deployment does not change its environment):

```pwsh
azd env select zavaw3
azd env set BRIDGE_MODE GraphMedia
az containerapp update -g rg-zava-scrum-zavaw3 -n ca-api-3fsyvjqqjw5go --set-env-vars "Bridge__Mode=GraphMedia"
pwsh infra\hooks\verify-media.ps1 -RequireReady
```

The media hook sets `Bridge:GraphMediaBaseUrl` to the verified HTTPS origin and preserves the existing secret `Bridge:ApiKey`. Direct and Hosted voice routes both work through the existing factory; retain Direct unless separately testing the optional hosted route. Prepare a session for the chosen team and start it in Teams with its stored calendar link; the session's `mode` remains `acs`. The calling app must already be installed and consented in that meeting chat.

The live console shows native call/lobby state, the current floor and participants. Missing/cross-team/duplicate object identities are guests, including a guest with the same name as a roster member. Guests receive no query or approval rights; an organiser-selected shared screen is visible to all meeting participants. Synthetic stop was dispatched 29.6 ms after the 350 ms threshold (n=1), with truncation at 120 played ms, **not measured in Teams**. Opening-state speech sustained for 600 ms did not cancel; shared opening/recap exceptions remain. Test these explicitly, rather than claiming unconditional barge-in.

Native media saves no PCM, transcript, spoken update or meeting-derived proposal. Do not demonstrate a saved follow-up from native meeting speech. Restore the complete close-the-loop story with `azd env set BRIDGE_MODE Acs` and `az containerapp update -g rg-zava-scrum-zavaw3 -n ca-api-3fsyvjqqjw5go --set-env-vars "Bridge__Mode=Acs"`, leaving the Windows host intact. **Simulated** retains the browser room and refuses Teams joins; the room remains available even when `GraphMedia` is selected.

### Native board and meeting events (7)

Keep the working **Acs / Share / Direct** default. Only after the organiser and
administrator complete the catalogue, actual calendar/roster and meeting RSC
actions in DEPLOY.md, prepare a fresh session and select native screen sharing:

```pwsh
azd env select zavaw3
azd env set BRIDGE_MODE GraphMedia
azd env set BOARD_MODE MediaShare
az containerapp update -g rg-zava-scrum-zavaw3 -n ca-api-3fsyvjqqjw5go --set-env-vars "Bridge__Mode=GraphMedia" "Board__Mode=MediaShare"
pwsh infra\hooks\verify-media.ps1 -RequireReady
```

The bot automatically requests the screen-sharing role at ceremony opening,
updates the same agenda/current-speaker/coverage or retro board, and gives up
the role when it ends. Observe **Board** state in the Live console; a share
failure must not stop speech. Native completion explicitly says notes are not
saved. Disconnect/reconnect and observe resumption; end the actual Teams meeting
and verify the matching session's single normal end path. An unbound, other-team
or duplicate event must not end a different session. Participant changes come
from the media roster, not display names or polling.

These live checks remain **not run, n=0**, including automatic guest visibility,
two-second updates, encoder CPU/cap and native G1 with share. Local real-Chromium
goldens do not match pixel-for-pixel: worst mean channel error 2.273/255 and
4.988% changed pixels, n=11 board states. Bounded text-rendering tolerances pass,
but do not call this exact parity. No personal side-panel tab is present (S7
was unrun). Restore the full saved-follow-up demo without tearing down the host:

```pwsh
azd env set BRIDGE_MODE Acs
azd env set BOARD_MODE Share
az containerapp update -g rg-zava-scrum-zavaw3 -n ca-api-3fsyvjqqjw5go --set-env-vars "Bridge__Mode=Acs" "Board__Mode=Share"
```

`Share` with GraphMedia selects audio without native sharing; it does not
silently start Chromium. Simulated Room keeps its existing canvas.

## The hosted voice route, pre-warm and canary (M11 prompt 4)

The voice can run through a Foundry hosted agent (a relay in front of Voice Live) or directly. Both give the same standup; the hosted route is selected by `VoiceLive__RelayEndpoint` on the API (the `voiceRoute` Bicep parameter, `Direct` or `Hosted`).

1. **See which route a session used.** Prepare a standup (Prepare now, or wait for the scheduler's pre-brief). `GET /api/sessions/{id}` shows `hostWarmUp`: `{ route, warmed, milliseconds, note }`. On the hosted route `warmed` is true and `milliseconds` is the cold start the standup no longer pays (about 3.5 to 4 s); on the direct route it says there is no host to warm.
2. **Switch the route for a demo** without a redeploy: `az containerapp update -g <rg> -n <api app> --set-env-vars "VoiceLive__RelayEndpoint=wss://<foundry>.services.ai.azure.com/api/projects/<project>/agents/zava-voice-relay/endpoint/protocols/invocations_ws"` for hosted, or `--remove-env-vars VoiceLive__RelayEndpoint` for direct. Wait for `/healthz`.
3. **Canary a new agent version.** The platform does not allow a percentage split on one agent endpoint, so a canary is two agents and the API chooses per session (ADR 0021). First pin the stable agent so a new version does not go live by itself: `pwsh infra/hooks/canary.ps1 -Pin <version>`. Then:
   - `pwsh infra/hooks/agent-host.ps1 -Canary` builds the candidate into the `zava-voice-relay-canary` agent;
   - `pwsh infra/hooks/canary.ps1 -Canary -Percent 10 -Teams team-payments` sends 10 % of that team's new sessions to it (omit `-Teams` for every team; the route must be hosted);
   - `pwsh infra/hooks/canary.ps1 -Promote` makes the canary image a new stable version, pins it and stops the canary;
   - `pwsh infra/hooks/canary.ps1 -Rollback` is the one-step rollback (stops the canary; with no canary it pins the previous stable version);
   - `pwsh infra/hooks/canary.ps1 -Show` prints the pin and the canary settings.
   The API restarts onto a new revision for each change. A session keeps its
   chosen route; inspect `hostWarmUp.route` and the actual ready revision.
4. **Restart resilience:** loopback tests cover host interruption and store
   recovery. A real platform stop during a standup remains unverified; do not
   kill a shared host or present that test as a live recovery rehearsal.
5. **Which route to use.** Direct stays default. Historical hosted 4 held
   latency point estimates without confidence-bound margin; candidate 5
   failed actual readiness/protocol testing. Do not promote it. For an
   authorised rehearsal use the scoped API setting in step 2 and restore it
   afterwards; never full-provision the shared working zavaw3 environment
   merely to switch adapters.
6. **Measure the hop.** `dotnet run --project tools/VoiceRelayBench -- --mode paired ...` (header comment in the file) alternates direct and relayed turns; `pwsh docs/gates/m11-p4/g1.ps1` runs the gate harness on both routes with the decision model on and off.

### Agent identity sign-ins (5B)

For the separate calling application, see the native-media instructions below; it is not the hosted agent's Entra Agent ID.

The hosted agent signs in as its own Entra Agent ID. Evidence, after a standup or a warm-up:

- Entra admin centre > Monitoring > Sign-in logs > **Service principal sign-ins**: filter Service principal = the agent (`<foundry>-<project>-zava-voice-relay-AgentIdentity`). The Agent column shows type `agenticAppInstance` and the blueprint as parent.
- Graph: `az rest --resource https://graph.microsoft.com --method get --url "https://graph.microsoft.com/beta/auditLogs/signIns?`$filter=signInEventTypes/any(t: t eq 'servicePrincipal') and servicePrincipalId eq '<AGENT_HOST_PRINCIPAL_ID>'&`$top=5"` (the filter without the event type times out).
- Log Analytics (if exported): `AADServicePrincipalSignInLogs | where ServicePrincipalId == "<AGENT_HOST_PRINCIPAL_ID>" | project TimeGenerated, ResourceDisplayName, ResultType, IPAddress`.
- `pwsh infra/hooks/agent-identity.ps1` prints the identity, its (absent) credentials, the blueprint, the sponsor and the latest sign-ins.
