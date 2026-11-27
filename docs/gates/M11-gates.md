# M11 gates

## Copilot channel (conductor 8, 2026-11-26)

ADR 0025 implements five real-mediator skills using membership resolution and
the asker's OBO token, never the service reader. Toolkit schema/rules pass on
both source and generated 1.25/version 1.1.0 manifests. The actual generated
zavaw3 app/bot/Copilot ids all match `bed81a44-b97d-4687-9ad3-c4cf546f6689`;
portal SSO is `265790b8-88a7-4975-bb8f-3fe381bb19ea`. There is no personal tab
or new RSC. Existing Azure DevOps AllPrincipals `user_impersonation` consent
was read back, not assumed from app declarations.

The signed-in Graph catalogue read returned Forbidden for missing AppCatalog
scopes after the administrator window expired. No authentication loop or
tenant policy/role change. DEPLOY.md/DEMO.md give the exact tenant upload,
admin review and consenting-user acceptance steps. Eligible user update
consent can propagate between contexts according to current Learn; an
administrator cannot force the user update.

| Gate/check | Step 8 result | Sample and evidence |
| --- | --- | --- |
| G1 native conversational latency | Not run | n=0 native; the Direct before/after harness is fallback regression only |
| G2 native attribution | Not run | n=0 new live samples; prior failures/prerequisites unchanged |
| G3 approver-authored writes | Measured portal subset | n=1 final approved comment, real approver author and complete audit chain; verify 8/8, not Copilot SSO |
| G4 full parity / rehearsal | Prior exact board parity failed; no full suite rerun | Prior checkpoint 849 .NET / 24 bridge passes retained; exact identity still fails 11/11 |
| G5 real Copilot skills, permission-negative and streaming | Not run | n=0 authorised Copilot turns; local tests/package validation are separate evidence |
| G6 stored-link native joins | Not run | n=0; existing catalogue/calendar/verified-roster/organiser-RSC gates unchanged |
| G7 observability / decision goldens | Not run | n=0 new live acceptance samples |
| G9 live multi-team matrix / concurrency | Not run | n=0 live; local membership/no-team/multi-team/isolation checks pass |
| G10 Foundry IQ grounding | Not run in step 8 | No new source or acceptance sample |
| G11 Work IQ consent / privacy | Not run in step 8 | Native non-recording and existing consent boundary unchanged |
| Local skill / permission / manifest checks | Pass | Domain 4, real-mediator skills 17 plus mediator compatibility 7, Infrastructure 11, Api 9, architecture 14: 62/62 |

All three builds, both TypeScript typechecks/linters and full C# formatting
pass. Focused tests cover actual-assignee selection, unretracted/readable
commitments, team-local dates, no-team/admin/multi-team/roster/consent refusals,
cross-credential HTTP isolation and model/id guards. Custom title/state labels
with unknown references are omitted; cards offer at most five source actions.
The automatically resolved team's use-case span/log/metric and model-source
context carry its identifier, tested through the real mediator.
No full suite rerun before the post-prompt-10 checkpoint. Added fixed cost
USD 0/hour.

### Step 8 Direct Simulated Room regression

Before: one deployed run, **12 timed turns**, **387 s**, **19 transcript
pieces**, **zero talk-overs**, API/native source `0d07e83`, bridge `c25287f`.
Total p50/p95 **3272/4379 ms**; analyser **26/27**, decision **160/240**,
orchestration **1522/2449**, Voice Live first audio **1718/2646**.
Decision model used **10/12** turns. Initial deployment `29f87e7` passed all
eight mandatory checks: one run, **12 turns**, **390 s**, **19 transcript
pieces**, zero talk-overs, total **3301/7343 ms**. Its p95 point estimate
failed (**+67.69%**) with Voice Live first-audio p95 **5496 ms**. That failed
sample is retained, not replaced by the final measurement.

Final committed runtime `d041890` is deployed as API image `git-d041890`,
ready revision `ca-api-3fsyvjqqjw5go--0000082`. ACR run `dss` succeeded
in **162 s**. The final mandatory verification passes **8/8**: one run,
**12 turns**, **379 s**, **19 transcript pieces**, **zero talk-overs**.

| Hop | Before p50/p95 ms | Final p50/p95 ms | Timed turns per run |
| --- | ---: | ---: | ---: |
| Total, speech end to first audio | 3272/4379 | 3304/4503 | 12 |
| Speech end to transcript | 0/212 | 0/201 | 12 |
| Analyser, rules and grounding | 26/27 | 26/57 | 12 |
| Decision model call | 160/240 | 161/240 | 12 |
| Orchestration | 1522/2449 | 1554/2488 | 12 |
| Voice Live first audio | 1718/2646 | 1439/2436 | 12 |

Final total point estimates change **+0.98% p50 / +2.83% p95**, within
the historical 5%/10% fallback regression thresholds. Questions alone:
before **3615/4379 ms**, final **3349/4503 ms**, **n=9** each; the decision
model reads **10/12** turns in each run. Percentiles are nearest-rank.
This is one before/final pair, not the formal three-run/model matrix or
native G1; absolute p50 remains above the SPEC's 1 s target.
No native or Copilot latency claim, or erased historical tail failure.

Final G3 sample: comment `verify-31084cd3`, ADO revision **51**, session
`ddbb0ca4-139f-4e84-be88-df73c296591f`, audit tag `zsa-c3e96bb06f29`,
authored by the real approver. The bridge remains `m11-7-c25287f`,
ready revision `--0000007`; native host and non-recording are unchanged.
Existing env, secrets, identity, scale and other Easy Auth properties
hash-match; only the two exact public legal paths were added. Both return
HTTP 200. Final gate summary/raw turns and verification are retained in
the untracked run log, `p8-final-after-verify` and `8-final-verify.txt`.
Later documentation-only commits do not change this deployed runtime.

## Full test checkpoint after prompt 7 (tests-1, 2026-11-26)

One aggregate `dotnet test ZavaScrum.slnx --no-build --no-restore` run passed
**849/849**, with zero failures or skips. Counts exceed every required G4
baseline; hosted and Windows-native projects are included rather than omitted.

| Suite | G4 baseline | Checkpoint passed / total |
| --- | ---: | ---: |
| Domain | 388 | 403 / 403 |
| Application | 143 | 170 / 170 |
| Infrastructure | 73 | 88 / 88 |
| Api | 132 | 151 / 151 |
| Architecture | 7 | 14 / 14 |
| AgentHost | Not specified | 3 / 3 |
| MediaBot (Windows) | Not specified | 20 / 20 |
| Bridge unit tests | Not specified | 12 / 12 |
| Bridge standup E2E | Not specified | 6 / 6 |
| Bridge retrospective E2E | Not specified | 6 / 6 |
| Portal tests | Not configured | 0 configured; no test runner or test script |

All three builds pass; solution build has zero warnings/errors. Both
TypeScript typechecks and ESLint/Prettier checks pass. The existing portal
bundle-size advisory is not a build failure. Full C# formatting exposed
16 pre-existing whitespace-only files; these are corrected on the tip branch
and full `dotnet format --verify-no-changes` passes, without changing runtime
behaviour. Ownership is earlier prompts 3/3A and 5A, not prompt 7.

Real bridge E2E passed **12/12**, zero failed/cancelled/skipped: one
retrospective in **298.3 s** and one standup in **366.4 s**, **666.4 s**
for the aggregate invocation. They used an isolated local Json store on
ports 5089/5099 with actual zavaw3 Azure services.
Initial setup cancelled all 12 assertions before execution because the local
credential chain stopped at inaccessible IMDS; valid Azure CLI tokens were
confirmed and the documented local credential selector was applied. A local
HTTPS/websocket invocation mistake was also corrected before the passing run;
neither required a shipped-code or test change. Helpers and temporary data
were removed after completion. No full .NET suite was rerun for whitespace.
No native participant, encoder, meeting-event delivery or latency gate is
inferred from these tests. Existing failed exact board parity and fallback
latency point estimates below remain failed. No resources or deployment
settings changed; added fixed cost is USD 0/hour.

Commands: `dotnet build ZavaScrum.slnx --no-restore`;
`dotnet test ZavaScrum.slnx --no-build --no-restore`;
`dotnet format ZavaScrum.slnx --verify-no-changes --no-restore`.
For each TypeScript project, run its existing `build`, `typecheck` and `lint`
scripts. Bridge unit checks use `npm test --prefix src\meeting-bridge`.
The two real E2E suites run once together from `src\meeting-bridge` with
`node --import tsx --test --test-concurrency=1 e2e\standup.e2e.ts e2e\retro.e2e.ts`;
local configuration prerequisites are in DEMO.md. Retained TRX and complete E2E
output are in the untracked `.m11-plan\run-log\tests-1-results` and
`tests-1-e2e-configured.txt`.

## Board share and meeting events (conductor 7, 2026-11-26)

ADR 0024 adds a genuine selectable native VBSS adapter and typed scheduled
meeting events. ACS canvas, Acs and Direct remain defaults. The administrator
attempted publication of the updated messaging package; Graph refused missing
delegated catalogue scopes despite the active admin role. Actual-calendar,
verified member identities, organiser RSC and real participants remain gates
in DEPLOY.md. No personal tab without positive S7 evidence.

| Gate/check | Step 7 result | Sample and evidence |
| --- | --- | --- |
| G1 native share latency | Not run | n=0 native; Direct Simulated Room before/after is fallback-only |
| G2 native attribution | Not run | n=0 live; join/leave/guest ownership is covered by focused gateway tests |
| G3 approver-authored writes | Measured portal subset | n=1 approved comment and complete audit chain; mandatory verify.ps1 passes, not a native speech write or Teams SSO proof |
| G4 parity / rehearsal | Failed exact board parity; live rehearsal/full suites not run | n=11 genuine Chromium/native board states; all differ; n=0 live native rehearsal; checkpoint owns full suites |
| G5 Copilot | Not run in step 7 | No new sample; preserve historical evidence below |
| G6 stored-link native joins | Not run | n=0; live meeting prerequisites absent |
| G7 observability / decision goldens | Not run in step 7 | Board images are not decision/trace acceptance |
| G9 multi-team matrix / concurrency | Not run in step 7 | Focused isolation tests are not the full live matrix |
| G10 Foundry IQ grounding | Not run in step 7 | No new sample; preserve historical evidence below |
| G11 Work IQ consent / privacy | Not run in step 7 | No new sample; preserve historical evidence below |
| Board automatically visible to every participant/guest <=2 s | Not run | n=0; renderer/socket readiness is not participant delivery |
| Board change visible <=2 s | Not run | n=0 native |
| Encoder CPU per meeting / cap | Not run | n=0; cap remains 1, not a measured capacity |
| Scheduled meeting start/end delivery | Not run | n=0 live; real-mediator tests verify isolation/idempotence, not delivery |
| Fallback latency point estimates | Failed p95 tolerance | n=12 turns per run; total p50 +3.34%, p95 +39.16%; not formal/native G1 |

Eleven golden fixtures use the actual existing Chromium canvas: waiting, empty,
agenda, speaker-highlight, covered-clipped-six, standup-complete,
retro-round-empty, retro-round-notes, retro-themes, retro-actions, retro-complete.
Their captured PNGs, native output, provenance, hashes and regional
measurements are in `src/tests/Zava.Scrum.MediaBot.Tests/BoardGoldens`.
Worst compared region (retro-complete/header): channel MAE **2.273/255**,
changed pixels **4.988%**, geometry maximum
channel difference **1**. Tests use explicit 2.5/255 MAE and 6% regional change
tolerance for browser/Skia text shaping; **exact identity fails all eleven**. Do not
remove ACS or mark the native visual gate passed. Native non-recording completion
intentionally changes saved-follow-up wording, tested separately.

Focused checks: board renderer 5/5, board lifecycle/failure recovery 7/7,
native gateway/ephemeral retro 13/13, Application meeting events/normal end
20/20, Domain session/lifecycle 10/10, SDK presentation 3/3 and dependency
architecture 14/14 (including the dependency subset). These are local code checks, not native meeting samples.

The deployed native DLL hash matches the local published Release DLL and its
product version contains source0d07e83. SYSTEM task Running, one boot trigger,
last run2026-11-26T22:09:09Z; real renderer readiness passes. This is artifact/
hosting proof, not encoder load, meeting-event delivery or participant visibility.

### Step 7 deployed fallback regression, Direct Simulated Room

Before: one run, **12 timed turns**, 382 s, 19 transcript pieces, zero
talk-overs; API/bridge images `m11-6b-1b7a614`. After: one run, **12 timed
turns**, 398 s, 19 transcript pieces, zero talk-overs. API/native source
`0d07e83`, API image `m11-7-0d07e83`, bridge image `m11-7-c25287f`.
API/bridge ready revisions are `--0000080` / `--0000007`. The runner source
label is not an image hash; the final documentation-only commit does not
change deployed runtime. Mandatory `verify.ps1` passes all eight checks,
including native renderer/capability protection, approver-authored comment
and complete audit chain. Raw before/after and summaries are in `m11-p7`.

| Hop | Before p50 / p95 ms | After p50 / p95 ms |
| --- | --- | --- |
| Total turn | 3230 / 3958 | 3338 / 5508 |
| Transcript | 0 / 192 | 0 / 198 |
| Analyser | 27 / 27 | 27 / 40 |
| Decision model | 152 / 167 | 144 / 236 |
| Orchestration | 1520 / 1967 | 1576 / 3798 |
| First Voice Live audio | 1564 / 2292 | 1459 / 1908 |

Total p50 rises 108 ms (3.34%); p95 rises 1550 ms (39.16%), exceeding the
historical 10% tolerance. The orchestration tail is larger. Question-only
before/after is 3350/3958 versus 3642/5508 ms, **n=9 each**, with p50 +8.72%.
Each run uses the decision model on 10/12 turns. These nearest-rank one-run
estimates fail the fallback latency point comparison; they neither establish
causality nor pass formal G1's >=30 questions/3 runs and model-on/off matrix.
Simulated transcript p50 zero does not exercise speech-to-text. Native G1
remains **not run, n=0**; preserve defaults, not a speculative timing fix.
The printed, unchecked Teams checklist is historical ACS guidance, not native
roster/share/rehearsal or native saved-follow-up evidence.

## Native media hosting checkpoint (conductor 6a/6b, 2026-11-26)

The Windows host and trusted Azure DNS-label certificate are deployed in zavaw3/westus3. Native acceptance remains unrun: the catalogue POST lacked delegated OAuth scopes, the disabled/simulated schedule contains no real meeting links, Daniel/Maya lack roster Entra object ids, and organiser RSC/real voices are outstanding. The rows below refer only to GraphMedia; historical ACS, Simulated Room and hosted-relay evidence is not native-media evidence.

Real SDK/HTTPS readiness reports no recording and cap 1; it survived an idle VM reboot with no manual task start. The running SYSTEM task has one at-startup trigger. Scoped VM vault/container roles read back correctly. This is hosting readiness, not media handshake or capacity acceptance.

| Gate | Native status | Sample size / evidence |
| --- | --- | --- |
| G1 latency with extra media hop | Not run | n=0 native; fallback before/after harness is not an extra-hop measurement |
| G2 participant attribution >=95% | Not run | n=0 live; 90/90 already identity-tagged synthetic frames (30 per identity) prove routing, not SDK attribution or voice recognition |
| G6 ten stored-link joins | Not run | n=0; TLS and tenant media consent completed, catalogue/stored calendar/organiser RSC remain |
| Live barge-in <=500 ms, cancel/truncate/answer | Not run | n=0 human; synthetic stop dispatch 29.6 ms after the 350 ms threshold, n=1, with cancellation/truncation at 120 played ms; not audible cessation or a spoken answer |
| Unconditional barge-in, synthetic opening state | Failed | n=1 state probe; sustained 600 ms speech does not cancel because the shared opening exception remains |

Local Windows parser/amplitude tests: 8/8; real-mediator adapter: 3/3; expanded ephemeral gateway: 7/7; media capability/ownership: 3/3; existing API sign-in classification: 1/1; architecture: 14/14. These are code checks, not gate measurements. Strongest-guest overlap suppresses quieter member audio; opening/recap/absent-announcement exceptions remain. GraphMedia saves no audio-derived transcript/follow-up and has no native board share yet. ACS and Direct voice stay the defaults. See ADR 0023 and DEPLOY.md.

### 6b deployed fallback regression, Direct Simulated Room

Before: one run, 11 timed turns, 373 s wall, 17 transcript pieces, zero talk-overs. The runner label `0619189` names its source checkout, not the old deployed API image (`api-zavaw3:azd-deploy-1795640105`, revision `--0000077`). After: one run, 11 timed turns, 381 s wall, 17 transcript pieces, zero talk-overs; runner checkout `e1a1f78`, API/bridge images `m11-6b-1b7a614`. Mandatory `verify.ps1` passed all eight checks, including approver-authored Azure DevOps comment and complete audit chain. This comparison preserves the fallback path; it cannot pass native G1.

| Hop | Before p50 / p95 ms | After p50 / p95 ms |
| --- | --- | --- |
| Total turn | 3419 / 4435 | 3311 / 5115 |
| Transcript | 0 / 195 | 0 / 192 |
| Analyser | 28 / 33 | 27 / 62 |
| Decision model | 215 / 410 | 164 / 368 |
| Orchestration | 1536 / 1966 | 1601 / 3813 |
| First Voice Live audio | 1670 / 2164 | 1364 / 2407 |

Total p50 fell 108 ms; p95 rose 680 ms (15.3%), with a larger orchestration tail. The fallback p95 point estimate exceeds the historical G1 10% tolerance, so this fallback latency comparison is **failed on that point estimate**, not a pass. These are nearest-rank estimates over 11 turns per run, not confidence bounds or proof of a native-hop regression; native G1 remains not run. The harness's printed Teams rehearsal checklist is unchecked historical ACS guidance, not evidence of a native rehearsal or native saved follow-ups.

Status after prompt 1. A gate is **open** until its evidence is recorded in ADR 0015. Numbers are measured, not expected.

| Gate | Question | Status | Evidence |
| --- | --- | --- | --- |
| S1 | Keep the Teams app and bot id on publish | Failed for direct publish | Generated manifest uses the agent instance identity as `botId` and the agent GUID as `id`; Learn forbids changing them |
| S2 | Meeting events for a Foundry-registered bot | Not run | Agents SDK sample requires a Teams-managed bot; Foundry registration type unknown |
| S3 | Hosted agent availability and fit | Passed with preview caveats | West US 3 supported; cold start 3.4 to 4.1 s; warm 0.23 to 0.31 s; version `Active` in 18 to 21 s; `$HOME` survives idle; .NET Activity adapter is beta |
| S4(a) | Agent identity reads Azure DevOps | **Failed** | Token works; Azure DevOps cannot add a `ServiceIdentity` (`VS403283`), control service principal added fine |
| S4(b) | Delegated user token writes as the user | Passed with a caveat | Revision author was the signed-in user; real on-behalf-of exchange not run |
| S4(c) | Deny the agent write while the user writes | Not testable | The agent identity has no Azure DevOps access |
| S5 | Media bot joins from the join URL | Not run | Needs a Windows VM and a live meeting |
| S6 | Per-participant audio over `invocations_ws` with barge-in | Partly measured | Binary round trip p50 81 to 82 ms, p95 100 to 108 ms from a laptop; no media bot, no Voice Live, no barge-in test |
| S7 | Bot-shared board and side-panel tab | Not run | Needs S5 |

## Latency baseline for M11 (M10 build)

Measured with `tools/GateRunner` (see DEMO.md, "Gate runner"): the Simulated Room cast, autopilot, three runs per configuration, against a **local** stack built from commit `84498ad` (the M10 build that is deployed). The raw JSON is in `docs/gates/baseline-M10/`. Any M11 change re-runs the tool with the same labels and pastes the result here.

Caveats, stated rather than hidden:

- **Local, not deployed.** The deployed API is behind Easy Auth and the az CLI cannot obtain a token for it, so the runner (which accepts `--api` and `--token`) was not run against Azure. The deployed image is the same commit, but network distance to Voice Live differs. Running it against Azure needs a token for the portal app registration.
- **Speech end to transcript reads 0 at p50.** In the Simulated Room the cast text reaches the gateway directly rather than through speech-to-text, so this hop is not exercised; it is only meaningful in a Teams meeting.
- **The orchestration hop includes the silence wait** (`Voice__SilenceMs`, 800 ms by default in this configuration) plus the reconcile, because the floor waits for silence before it asks the model to speak.
- The decision-model run is about 0.7 s slower at p50 than rules (3,827 against 3,116 ms), but the decision model call itself is 282 ms p50. The rest of the gap is in Voice Live first audio (1,862 against 1,582 ms) and in turns where the model recorded more notes, so it is not all attributable to the model; three runs are too few to separate them.

#### decision (build 84498ad, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3827 | 4907 | 35 |
| Speech end to transcript | 0 | 202 | 35 |
| Analyser (rules and grounding) | 24 | 36 | 35 |
| Decision model call | 282 | 393 | 35 |
| Orchestration (floor, silence, reconcile) | 1520 | 2440 | 35 |
| Voice Live first audio | 1862 | 3362 | 35 |

Questions only (Clarify and Unblocker turns): p50 3850 ms, p95 4907 ms, n 26.
Read by the decision model: 29 of 35 turns; the rest used the rules.
API-reported session p50 per run: 3995, 3357, 3644 ms.

#### rules (build 84498ad, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3116 | 3905 | 35 |
| Speech end to transcript | 0 | 202 | 35 |
| Analyser (rules and grounding) | 21 | 27 | 35 |
| Decision model call | 0 | 0 | 35 |
| Orchestration (floor, silence, reconcile) | 1614 | 2023 | 35 |
| Voice Live first audio | 1582 | 2400 | 35 |

Questions only (Clarify and Unblocker turns): p50 3173 ms, p95 3905 ms, n 26.
Read by the decision model: 0 of 35 turns; the rest used the rules.
API-reported session p50 per run: 3166, 3116, 3072 ms.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.


## Identity and traceability (prompt 3: G2 and G3)

Measured on the greenfield environment `zavagf` (`eastus2`), built by `azd up` from an empty resource group with the prompt 3 code (28 min 5 s: 24 min 28 s provisioning, 3 min 37 s deployment). Logs are in `docs/gates/m11-p3-logs/`.

### G2: identity (passed)

| Check | Evidence |
| --- | --- |
| Display name alone is never an identity | `ActorIdentityTests`, `TeamsActorTests`: a name, an empty id or the zero id is refused (`identity.object_id_required`); `ApprovalIdentityTests.Approve_ADisplayNameThatMatchesTheRosterButNotTheObjectId_...` refuses a Teams user called "Priya S." whose object id is not the roster member's, writes nothing and appends no record |
| Authority by object id, tenant and role | `ApprovalAuthorityTests` (own follow-up, someone else's, Scrum Master items, administrator, wrong tenant even for an administrator, wrong channel, each portal role) |
| Legacy decisions | `FollowUpTests.Restore_LegacyApprovalWithNoIdentity_...`: shows `unverified (legacy)`, is never written, cannot be approved again |
| In-app token validation | `PortalAuthorizationTests`: no token, Easy Auth headers only, another tenant, wrong audience, wrong issuer, no `oid`, no `tid`, expired, wrong signing key: all 401 |
| Roles on every route | `PortalAuthorizationTests`: reader and no-role behaviour; approve with Reader or Admin only is 403; operate (`/api/demo/reset`) is 403 for Reader and Approver and allowed for Admin; a test enumerates every `/api` route: reader on all, admin on every change, approver on every approve or reject |
| Roster object id from Graph | Live: after `POST /api/teams/team-payments/members/sync` the member `admin` (mapped to `ADMIN_UPN`) carried `entraObjectId` `aeaec64b-...`, equal to `ADMIN_OBJECT_ID`; the API log shows the Graph `GET /v1.0/users/{upn}` calls. Daniel and Maya have no account, so none |
| Fail closed with no client id | `NoClientIdTests`: outside Development every portal route is 401, health stays up |

Test totals after this change: Domain 348, Application 129, Infrastructure 64, Api 26, Architecture 7; all pass. Build has no warnings.

### G3: chain up to the Azure DevOps comment (passed; author attribution is prompt 5A, below)

- **Application tests, real mediator:** `ApprovalIdentityTests.AuditChain_StandupApprovedInTeamsAndInThePortal_IsCompleteBackToTheSession` approves one follow-up as a Teams member and one in the portal and asserts: the session, two proposals with hashes, four append-only records (Approved then Applied for each, the Applied record pointing at its decision and carrying revisions), channels Teams then Portal, and every written attribution ending `Correlation: zsa-<tag>`. Other tests prove the write is refused without a matching approval record, or if the proposal changes after approval, and that a chain with a missing link lists it.
- **Deployed, `verify.ps1`:** all seven checks pass (`docs/gates/m11-p3-logs/verify-pass.log`), including a follow-up approved through the API with the deployer's token, the comment found on work item 1 with `Correlation: zsa-...`, and `Audit chain complete`: `zsa-31116ae1b878: approved by object id aeaec64b-d720-4996-9022-02fa0dcf043d, revision 31 on #1`. The records are read back from the Cosmos `ApprovalRecords` container.
- **Deployed, `GateRunner --check-audit`:** three runs, three complete chains (`zsa-ae925f81550b`, `zsa-ecaca2f47de7` and the first run's), each with 2 records and the approver's object id. A real comment reads: "Approved by System Administrator in the portal, proposed by Zava Scrum Agent from the 2026-11-25 standup. Correlation: zsa-ecaca2f47de7".
- **Earlier attempts, stated:** the first `azd up` stopped because azd needed `adminObjectId` before the preprovision hook could set it (now an optional parameter that `entra-post.ps1` also applies), and the second at a Graph query that does not support filtering `appRoleAssignedTo` by principal (now filtered in the hook). The first `verify.ps1` timed out in the standup after 12 minutes on a fresh deployment (bridge health also timed out once); the second run was interrupted by an environment change I made during it. The harness now waits up to 20 minutes, and the third run passed.

### Latency re-run (deployed, decision model on, three runs, `docs/gates/m11-p3-deployed/`)

| Hop (p50 / p95 ms, n 32) | Prompt 3 | Deployed M10 baseline (n 36) |
| --- | ---: | ---: |
| Total (speech end to first audio) | 3071 / 4755 | 2994 / 4081 |
| Analyser | 25 / 26 | 25 / 28 |
| Decision model call | 154 / 271 | 147 / 242 |
| Orchestration | 1560 / 1913 | 1446 / 1889 |
| Voice Live first audio | 1524 / 2622 | 1440 / 2584 |

The audio path does not use the mediator for identity or the audit trail, and nothing was added to a turn, so the difference (+77 ms at p50, +674 ms at p95) is within what three runs against Voice Live vary by (a different Container Apps environment from the baseline, same region and models). It is recorded, not explained away.

### Not verified

- **Cosmos platform denial.** The custom role gives the managed identity create and read only on `ApprovalRecords`; the code never replaces, and writes and reads through it worked. I did not attempt a replace through the data plane to see the platform refuse it.
- **A Teams approval from a real Teams user.** The Teams app upload is a human gate, so the `Activity.From.AadObjectId` path is covered by unit tests with constructed activities, not a live card action.
- **Portal sign-in in a browser.** The SPA build passes `tsc`, ESLint and Prettier, and the API accepts the token the SPA would send (the verify token has the same audience and scope), but I did not drive the MSAL sign-in redirect in a browser.
- **Destroy and rebuild** is still open from G8 (prompt 2); this deployment is a second `azd up`, not a teardown.

## Greenfield (G8)

Environment `zavagf`, resource group `rg-zava-scrum-zavagf`, `eastus2`, subscription `4b0dd31f-...`, tenant `fb049398-...`. Logs are in `docs/gates/g8-logs/`.

| Step | Result | Time |
| --- | --- | --- |
| `azd provision` into an empty resource group (first `azd up`) | Infrastructure created. The `ado-setup` hook then failed (it called a list endpoint Azure DevOps does not support); fixed and re-run | 21 min 12 s (Container Apps environment alone 13 min 30 s) |
| `ado-setup`, `entra-post` re-run (idempotent) | Pass | about 1 min |
| `azd deploy` (remote ACR builds, both apps, Teams package) | Pass | 4 min 21 s |
| `verify.ps1` | **All checks passed** (health, bearer token through Easy Auth, bot endpoint 401, Simulated Room standup through GateRunner, follow-up approved via the API and its comment found on work item 1; audit chain SKIP until prompt 3) | about 5 min |
| `azd down --purge` | Pass; resource group removed, Foundry account purged | 28 min 59 s |
| Second `azd up` | **Not completed.** `preprovision` stopped at `entra-portal-app.ps1`: Graph returned `Authorization_RequestDenied`. The deployer's privileged Entra role (active during the first run) is no longer active, so this is the documented deployer-permission gate, not a template fault | 1 min |

**Not proven:** the second `azd up` and its `verify`, so the "destroy and rebuild" half of G8 is open. Re-activate the Entra role (Application Administrator or equivalent; `preflight.ps1` lists what is needed) and run `azd up` then `pwsh infra/hooks/verify.ps1`. A new subscription was not available.

Defects the first run found and fixed: the Azure DevOps entitlement hook used an unsupported GET; Easy Auth rejected a valid Azure CLI bearer token until `allowedApplications` included the Azure CLI client; `azd down` clears any output whose name equals an input, so `ADMIN_UPN` and `AZURE_TENANT_ID` outputs were removed from `main.bicep`.

### G1 deployed baseline (M10 build 84498ad, greenfield environment)

Raw JSON and `summary.md` are in `docs/gates/baseline-M10-deployed/` (three runs per configuration, the Simulated Room cast against the deployed API with an Entra bearer token). These replace the local-stack figures as the baseline for later stages.

| Hop (p50 / p95 ms, n 36) | Decision model | Rules only |
| --- | ---: | ---: |
| Total (speech end to first audio) | 2994 / 4081 | 2828 / 3876 |
| Analyser | 25 / 28 | 29 / 39 |
| Decision model call | 147 / 242 | 0 / 0 |
| Orchestration | 1446 / 1889 | 1580 / 2042 |
| Voice Live first audio | 1440 / 2584 | 1381 / 1894 |

Against the local baseline (3827 / 3116 ms p50) the deployed runtime is faster, and the decision model adds about 0.17 s at p50 (2994 against 2828 ms) rather than 0.7 s.

## G9: concurrent standups across teams (westus3, `zavaw3`, build c3083b7)

Three Simulated Room standups ran at the same time through `tools/GateRunner --teams team-payments,team-g9-b,team-g9-c --check-audit`. The two extra teams were registered through the team registry API as copies of team-payments (same Azure DevOps project and area) and set Active. Ten teams were not run live (decision: three prove concurrency; realtime quota in westus3 is 5 per model).

| Measure | Result |
| --- | --- |
| Concurrent standups | 3, all completed, 379 s wall clock |
| Timed turns | 36 (12 per standup) |
| Total p50 / p95 | 3400 ms / 4919 ms |
| Single-standup verify run, same environment | p50 3439 ms / p95 4400 ms (10 turns) |
| API-reported session p50 | 3544, 3262, 3492 ms |
| Audit chain per team | complete (session, proposal, approval record), 3 of 3 |

Raw results: `docs/gates/g9-three-concurrent-westus3/`. Not shown by this run: cross-team isolation under load beyond the audit chains, and real Teams or Voice Live capacity beyond three sessions.

### Quota request for ten concurrent standups (to be filed by the owner)

Realtime capacity is the limit. After M11 prompt 3B (ADR 0019) the only realtime deployment is `gpt-realtime-2.1`; `gpt-live-1` is removed. Quota is one row per model: in westus3 `OpenAI.GlobalStandard.gpt-realtime-2.1` has a limit of 10, of which the `mcm` environment holds 5, so one new environment can take 5. Removing `gpt-live-1` frees its own row only; it does not raise the `gpt-realtime-2.1` limit.

Request: raise `OpenAI.GlobalStandard.gpt-realtime-2.1` in westus3 (and the chosen production region) from 10 to at least 20, so two environments can each hold 10. Whether one capacity unit supports one concurrent standup is an assumption: three concurrent standups ran at capacity 5 (G9), which is consistent with it but does not prove ten. The parameters stay `realtimeCapacity` and `realtimeSku` in `infra/main.bicep`. Status: **to be filed by the owner**.

### Note: bridge health timeout in the first westus3 verify run

The bridge /healthz route does no slow work and no browser work (Chromium launches lazily on the first Teams join, not at start). Probed afterwards it answered 200 in about 0.5 s each time (five probes). The failing check ran about a minute after the new bridge revision was created, so the replica was most likely still starting; this is a hypothesis, not proven. Changes: HTTP startup, readiness and liveness probes on /healthz for both apps, and erify.ps1 retries the health routes for up to three minutes. The G8 rebuild runs erify.ps1 immediately after zd up and will show whether the timeout recurs.

## G8: destroy and rebuild in westus3 (`zavaw3`): closed by the owner after one rebuild

Environment `zavaw3` (westus3), `REALTIME_CAPACITY=5`, `REALTIME_SKU=GlobalStandard`. Build 090bf4d (the Graph `--resource` fix on top of 3A).

| Step | Result | Time |
| --- | --- | --- |
| First `azd up` (build c3083b7) | passed | 14m30s |
| First `verify.ps1` | 6 of 7; bridge health timed out once | about 4 min |
| `azd down --purge` | passed | 27m09s |
| Rebuild `azd up` from an empty group, build 090bf4d | **passed** | 15m25s |
| `verify.ps1` on the rebuild | **7 of 7 passed** (both health routes 200, bearer token, bot 401, Simulated Room standup, follow-up written to Azure DevOps, audit chain complete). Single-standup p50 3470 ms, questions only p50 3730 ms and p95 6427 ms (n 9). | 6m50s |

Two things went wrong on the way and are recorded as they happened. (1) A rebuild attempt failed in preflight because the Azure CLI reused a cached Graph token (`TokenCreatedWithOutdatedPolicies`); the hooks now pass `--resource https://graph.microsoft.com`. (2) A later attempt failed preflight on quota (0 of 10 free) because the torn-down Foundry account was still soft-deleted and holding quota; `azd down --purge` had been interrupted, and purging the account freed it. Neither is a template fault, but the second shows `azd down --purge` must be allowed to finish.

Closed on the owner's decision: a second destroy-and-rebuild cycle was not run (the rebuild above is the proof that an empty environment comes up and verifies), and the westus3 deployed baseline (`baseline-M10-deployed-westus3/`) was not saved. The 3B work will measure a westus3 baseline itself. The westus3 p50 (3439 to 3470 ms) against 2994 ms in eastus2 is region and capacity, not a regression. `zavaw3` is torn down.

## 3B: realtime model and silence (ADR 0019)

Evidence in `docs/gates/m11-p3b/` (start with its README). Environment `zavaw3`, westus3.

| Measure | Result |
| --- | --- |
| Candidates | `gpt-realtime-2.1` and `gpt-realtime-2.1-mini`, same Azure TTS voice; `gpt-live-1` not tested by design |
| Latency, same mode, next to each other | p50 within 100 ms (rules only 3301 against 3342 ms; decision model on 4192 against 4253 ms); mini p95 worse |
| Quality (transcript read of 18 standups each) | 2.1 raised 87 % of flagged items, mini 72 %; mini repeated a turn 8 times, 2.1 never; mini finished 139 timed turns, 2.1 finished 215 |
| Choice | keep `gpt-realtime-2.1` |
| Silence 800 / 650 / 500 ms | no difference measurable, nobody was talked over; keep 800 ms |
| Drift | the same configuration moved about 25 % between time windows, so G1 at 5 % and 10 % cannot be re-confirmed by measurement; the default path is unchanged |
| Deployments | `gpt-live-1` removed; mini added for the test and removed |
| Not done | listening to audio; a longer pause test; the westus3 deployed baseline folder (closed by the owner under G8) |

## 5A: delegated Azure DevOps writes (ADR 0020)

| Item | Result |
| --- | --- |
| Rule and handlers (Domain, Application, real mediator, fakes at the ports) | **Passed.** No token, another person's token and a refused exchange are each refused before a write; an approve with no token is refused before the record is written; a retry by someone else is refused; the approver's retry writes once. |
| Managed-identity write path | **Removed.** The only writer is built per write from the approver's token. |
| ACS connection string | **Removed** from the bridge, docs and settings. |
| Hooks and Bicep | **Passed in `zavaw3` (westus3).** The second attempt (after Entra roles were restored) stopped once at the bot OAuth connection: the provider name must be `Aadv2WithFic` (fixed). The third `azd up` completed (provision 4m41s, deploy 5m05s). |
| G3 live, portal | **Passed.** `verify.ps1` 7 of 7 (`m11-p5a-logs/verify-pass.log`): the Azure DevOps comment on #1 is authored by `admin@MngEnvMCAP763514.onmicrosoft.com` (the approver) with `Correlation: zsa-466f0ef309e5`, and the audit chain is complete (revision 43). |
| G3 Teams | **Not verified.** Needs the Teams app upload (human gate) and a real user. |
| Latency (G1), deployed `zavaw3`, three runs, n 32 (`m11-p5a-logs/verify-latency-3runs.log`) | **Total p50 3806 ms, p95 4626 ms** against the westus3 figures of 3439 to 3470 ms p50 (G8 and 3B baselines): about +10 % p50. The change adds no work to the voice turn (the exchange runs at approval, not in a turn), and the same build varied about 25 % between time windows in 3B, so this is recorded as within that drift, not proven equal; the 5 % and 10 % tolerances cannot be confirmed by measurement. `verify.ps1` also passed 7 of 7 again (comment authored by the approver, revision 45). |

G3 second approver without write access refused: covered by Application and Infrastructure tests (403 becomes `ado.approver_cannot_write`, nothing written); **not run live** (needs a second Entra user without project access).

G3 for Teams stays open until a real user approves from a card (human gate).

## Prompt 4: hosted agent (ADR 0021, ADR 0015 S6)

| Item | Result |
| --- | --- |
| S6 relay (paired, interleaved, 64 turns per run, two runs, `docs/gates/m11-p4/s6-*.json`) | **Held.** Paired difference in turn time p50 +48.5 and +7.5 ms, p95 of the turn +230 and +178 ms (about 4 % of the baseline p95). Barge-in not slowed. New hosted session connect +2.4 s, first reconnect after the 15-minute idle stop +4 s (6.8 s). Written into ADR 0015 S6. |
| 0a one Cosmos item per session, partitioned by team, with migration | **Done.** `Sessions` container in Bicep; `SessionMigration` at API start; deployed in `zavaw3`, `verify.ps1` 7 of 7 through the new store. Unit tests for migration, idempotence, per-team items and reset. |
| Hosted agent and voice relay (`src/Zava.Scrum.AgentHost`), port 8088, `invocations_ws`, team required | **Done and deployed** (agent versions 1 to 4, canary agent version 1). |
| Voice gateway, ceremony agents, Teams and Copilot handlers moved into the host | **Not done** (ADR 0021, "Not moved"). |
| Kill the host mid-standup and resume (step 2) | **Test passes** (`HostRestartTests`, loopback sockets). Not run against a real platform stop of the relay carrying a standup. |
| Pre-warm and the result on the session (step 3) | **Done, deployed.** 3585 ms cold, 2320 ms on a new session of a running agent; `hostWarmUp` in `GET /api/sessions/{id}`. |
| Versions and rollout (step 4) | **Done with a difference.** The platform refuses a percentage split on one endpoint; canary is two agents chosen per session and team. Live: 50 % canary gave 8 of 14 sessions to the canary (the rest before the revision rolled over), rollback gave 8 of 8 stable once the revision settled. |
| azd and Bicep (step 6) | Hook, roles, `voiceRoute`, `Sessions` container added; `azd up` and two `azd deploy` ran in `zavaw3`. Destroy and rebuild **not run**. |
| No Azure DevOps credential in the host | **Test passes** against `src/Zava.Scrum.AgentHost`. |
| **G1 on the hosted runtime, first run** (three interleaved rounds, 35 to 36 turns per cell; **superseded by 4A below**) | **p50 holds** (model on +30 ms, +0.9 %; off +65 ms, +2.1 %). **p95 with the model on does not hold:** 5679 against 4720 ms (+20 %), worse in all three rounds; with the model off hosted was lower (4251 against 4594 ms). The slowest hosted turns were orchestration outliers with normal voice latency. Stopped as the prompt requires; default stays direct; options in ADR 0021. |
| G4 parity | Domain 397, Application 152, Infrastructure 85, Architecture 11, AgentHost 3, Api 132 pass, zero warnings. Bridge suite unchanged. The 10-step Teams rehearsal was **not run**. |

## Prompt 4A: G1 on the hosted route, settled (ADR 0021, "G1 settled")

| Item | Result |
| --- | --- |
| Interleaved comparison, direct and hosted, decision model on and off, six rounds, three concurrent standups per run, 201 to 211 turns per cell (`docs/gates/m11-p4a/`) | **Done.** Model on: direct p50 3197 / p95 4269, hosted 3239 / 4504. Model off: direct 2990 / 4604, hosted 3041 / 4246. |
| Paired and pooled differences (hosted minus direct) | p50 +42 ms (+1.3 %, interval -3.1 to +5.7 %) and +51 ms (+1.7 %, -1.9 to +6.0 %); p95 +235 ms (+5.5 %, -6.1 to +16.2 %) and -358 ms (-7.8 %, -16.3 to +18.9 %). Per-round p50 differences: hosted slower in 5 of 6 (model on) and 4 of 6 (off) rounds. |
| **G1 verdict** | **Holds on the point estimates in all four cells** (p50 within 5 %, p95 within 10 %); not proven inside the tolerance at 95 % (intervals reach +6 % at p50 and +16 to +19 % at p95). The first run's 20 % p95 was small-sample noise. Gate not tuned. |
| Slowest turns | With the model on the hosted tail is voice-dominated (62 % of the turn; voice median 3152 against 2667 ms), consistent with the relay hop; with the model off it is orchestration-dominated on both routes. The earlier "orchestration outliers" reading is corrected. |
| Decision | Hosted is a **documented option**; **direct stays the default**. Switch: `azd env set VOICE_ROUTE Hosted` and `azd provision`, or `VoiceLive__RelayEndpoint` on the API (docs/DEMO.md). |
| Defects found | Cards, follow-ups and bot activities are still single Cosmos documents and fail with 413 after about 90 standups (both routes); the harness missed the end of a standup in about a third of long runs (fixed in `RoomRun`). See ADR 0021. |
| 10-step Teams rehearsal | **Not run.** It needs the Teams app of this environment uploaded (a human gate in `docs/DEPLOY.md`; the app catalogue cannot be read with the current Graph permissions, so I could not confirm it) and people in a real meeting. Nothing was substituted. |

## Prompt 5B: agent identity (ADR 0022)

| Item | Result |
| --- | --- |
| Agent identity adopted (agentIdentity, blueprint) | **Verified live** in `zavaw3` by `agent-identity.ps1`. |
| No secret on the identity; none in the host source | **Verified** (Graph: no credentials; architecture test over `src/Zava.Scrum.AgentHost`). |
| Sponsor | **Verified** (`admin@`). |
| Agent sign-ins with agentType | **Verified** (5 recent rows, `agenticAppInstance`). |
| Conditional Access policies | **Not created, not tested** (human gate, DEPLOY.md). |
