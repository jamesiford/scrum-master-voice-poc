# M11 gates

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

### G3: chain up to the Azure DevOps comment (passed; author attribution is prompt 5)

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
