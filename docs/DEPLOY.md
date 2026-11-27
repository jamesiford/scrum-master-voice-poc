# Deploying the current solution

The checked-in Bicep/hooks describe a new-resource-group deployment: Foundry
with chat, realtime and decision deployments (three by default), ACS, Cosmos,
ACR, API/bridge Container Apps, Bot/portal Entra configuration, Azure DevOps
project/seed and Teams/Copilot package 1.1.0. Postdeploy adds the hosted relay
and Agent ID checks; opt-in adds the native Windows host/certificate/package.
This is infrastructure coverage, **not a passed final-solution G8 proof**.
See [ADR 0015a](adr/0015a-greenfield-deployment-model.md),
[ADR 0029](adr/0029-final-documentation-and-greenfield-proof.md) and the
[measured gates](gates/M11-gates.md). Knowledge/Work IQ are unbuilt and add
no delivered sources, Search service or signal resources.

## Redo reconciliation is not an all-green release

On27November2026 the requested first aggregate runs pass968/968.NET and
25/25bridge checks (unit13, real E2E12), plus17pure hook checks. All three
builds/full C# format/both TypeScript typechecks/ESLint/Prettier pass;
portal has no configured tests. Initial concurrent Debug compilation hit
CS2012 before tests; isolated Release compilation passes35.18s with
zero warnings/errors, without stopping shared compiler/security processes.
No new feature/runtime deployment, grant, billing, resource, governance
policy or zavagf lifecycle action was made.

Explicit `azd exec -e zavaw3 -- pwsh -NoProfile -File infra\hooks\verify.ps1
-Runs 1 -GateOutput .m11-plan\run-log\reconcile-zavaw3-gates\runs` passes
8/8 in443.50s, including native readiness5/5 without a restart and one
genuine approver-authored item1/revision62/complete audit. Direct scripted
latency3065/3933ms,n12,talkovers0 is within regression tolerances versus
preceding8B-r, not a formal/native/retrieval result. Existing APIgit-3d24f8e/
revision85 and bridgegit-8859a7a/revision9 remain Running/HTTP200.
Inherited failures, G8failed1/2 and G10/G11not_run,n0 remain explicit.
[Aggregate evidence](gates/reconcile-M11.json) retains initial compilation
failure, current measurements and unchanged golden hash.

### Historical final validation (26 November 2026)

On26November2026, explicit `azd exec -e zavaw3 -- pwsh -NoProfile -File
infra\hooks\verify.ps1 -Runs 1` passes all8mandatory checks. It includes one
real portal-approved, approver-authored comment on item1/revision53 and its
complete audit chain, plus native HTTPS/renderer readiness, not native Teams
participation. Use `-GateOutput <ignored-directory>` to retain the actual
scripted timing result. No provisioning, teardown or deployed setting change
was made. G8 below is still failed, not closed by this verification.

All947.NET and25bridge unique configured checks have passing evidence after
one unchanged focused retrospective retry; the initial model deviation is
retained. Final G1 fallback tail comparison fails+12.75%p95,n12/12;
G4 exact board identity remains failed11/11; G7 release exits1 with
dates75%n8, one timeout and missing binary/intent probabilities9/1 on72cases.
Do not change adapters, goldens, thresholds or Domain rules to label this
green. Native/Copilot/source/multi-team human gates below remain exact;
this checkpoint supplies no new human fixtures. ADR0030 and
`gates/final-validation-M11.json` distinguish test, readiness and acceptance.

## Final G8 attempt and recovery

**Second recovery attempt (27 November 2026, ADR0035):** current unchanged
calling-app reconciliation succeeds, so the historical403 below is not an
open application-write gate for this operation. Before the initial purge, live project
readback exposed the first cycle's agent despite an empty deleted
account list. A guarded `predown` hook now deletes only the two code-owned
hosted agents through the supported data-plane API and confirms absence
before project identity teardown. Protected environments and unrelated
images are refused. Controlled cleanup and same-project recreation pass:
version1 creates30222ms/active43485ms, new instance principalc1aa55e6,
unchanged project principal3bce4b0f/internalId2c4c5e3d. This isolates the
retained agent/workspace binding; account purge alone did not clear it.
This controlled probe is NOT a complete project-lifecycle repair.
Thirteen focused lifecycle checks,17existing hook checks and all3builds pass.
G8 remains failed at0/2 new complete cycles.
Only zavagf/eastus2 is authorised. Initial media bootstrap instead hit
OperationNotAllowed because external governance deallocated the VM at08:46Z;
one owned start/retry completed without a policy change. At the end of
this attempt the media VM will be deallocated, not deleted.
The owned start/retry now succeeds: actual certificate registration,
trusted HTTPS/cap1/non-recording/1280x720renderer and focusednative5/5 pass.
BaselineDirect3021/4168ms,n12/revision64 is recorded with its original
7/8 result because the SSL check preceded completed bootstrap; do not
silently relabel it8/8. Initial purge now precedes both new emptycycles.
The chronology below describes the previous attempt, not current readiness.

**Current provider gate, not an expired-administrator gate:** initial purge
passes1641.7849s and exact group/deletedaccount/deletedvault absence reads
back19:13:50Z with all native/default inputs. The first NEW full up starts
19:13:52Z and fails19:33:57Z after1205.4765s. Version1 creation succeeds30402ms,
then `GetAgentVersion` fails HTTP404 NotFound:
`{"error":{"code":"NotFound","message":"Project not found"}}`.
Two bounded follow-up version/agent reads and a distinct agent-list
diagnostic reproduce the same error. ARM remains Succeeded/SystemAssigned,
principal `35f5f03b-0e70-446e-a30d-929756941fd3`, internalId
`34539814c5034cb8aab9ebf782176a64`, with the documented endpoint.
Existing subscription Owner/Foundry User access is read back; no role is added.
No documented evidence supports a naming/identity toggle or authentication
retry. The hook now refuses this project404 rather than falsely reporting
that the successfully created agent is absent. A second purge/up is therefore
not safely countable until the provider restores project lookup.

**Exact owner action:** the Azure subscription owner opens a Microsoft
Foundry support request for
`/subscriptions/4b0dd31f-5379-4acc-8dcd-60f2929d2513/resourceGroups/rg-zava-scrum-zavagf/providers/Microsoft.CognitiveServices/accounts/fdy-zava-scrum-pi7fumnppr2hk/projects/proj-zava-scrum`,
including the fresh principal/internalId, version1/image
`agenthost:20261127113210`, creation30402ms and19:33-19:40Z read failures.
Microsoft must reconcile the account/project data-plane mapping; the owner
must prove real agent/version/list reads succeed before resuming TWO new
full empty up/verify cycles with intervening purge. Do not toggle identity,
randomise names, change policy/quota/region/subscription or grant more roles.
See [content-free evidence](gates/g8-proof-2-M11.json) and ADR0035.

**Final independent result and cost:** new-footprint calling-app/certificate/
bootstrap and native RequireReady5/5 pass. Mandatory Direct verify8/8 passes
376.6268s, total2862/4397ms,n12, questions3247/4397ms,n9, modelread10/12,
humansegments19/talkovers0; genuine approverrevision66/auditzsa-67e38e91111a.
Baseline3021/4168ms,n12 gives point deltas-5.26%/+5.49%, within existing
5%/10% regression tolerances, not paired/native/hosted acceptance; G1's
historical failures remain. Original baseline7/8 SSL failure is retained.
These checks do not credit the failed full up.

Only `vm-media-pi7fumnppr2hk` is deallocated, confirmed
`PowerState/deallocated` at19:51:52Z. The final safe restart through the
published guard fails in11.7053s at19:53:44Z, before any resource deletion,
with the same project404; no bypass or further retry is made. All25 rebuilt
resources remain, API/bridge healthHTTP200. Native HTTPS is intentionally
unavailable after deallocation; Acs/Share/Direct remain working defaults.
The recovery activates the existing F0 `bot-media-zavagf` registration,
USD0/hour fixed; no new designed resource type or permission is added.
Known media computeUSD0.345/hour is removed; retained E10disk/staticIPv4
subtotalUSD0.018150685/hour remains, plus ACR/foundations and variable
model/hosting/events/storage/ingestion charges, not an all-in bill.

Retained application endpoints:
https://ca-api-pi7fumnppr2hk.bluebay-467a80ea.eastus2.azurecontainerapps.io
and https://ca-bridge-pi7fumnppr2hk.bluebay-467a80ea.eastus2.azurecontainerapps.io.

Remaining independent postdeploy hooks also run: public legal pages and
`zava-scrum-agent-zavagf-1.1.0.zip` generation pass. One actual authorised
catalogue POST at20:00:28Z returns HTTP403 `Forbidden`, request
`47576c18-613e-4c3b-bb4a-8124f00f84d1`:
`Missing scope permissions on the request. API requires one of 'AppCatalog.Submit, AppCatalog.ReadWrite.All, Directory.ReadWrite.All'.`
The CLI token has none of those OAuth scopes; administrator privilege does
not add them. This is NOT the resolved calling-app403. No authentication
retry, unrelated CLI grant or broader permission is made.
**Exact catalogue owner action:** a Teams administrator uploads
`appPackage/dist/zava-scrum-agent-zavagf-1.1.0.zip` in Teams admin centre >
Teams apps > Manage apps > Upload new app, external/bot id
`654a0c74-9a31-4383-9dc3-f6a44450d419`. Only this new zavagf package;
do not replace zavaw3's completed catalogue entries or the old mcm app.
The already-listed interactive catalogue gate is separate from G8
hosted-project repair and from organiser RSC/native Teams acceptance.

**Recovery attempt, 26 November 2026 after 20:00 PT:** the operator renewed
Global Administrator. Live directory-role readback and unchanged
zavagf application/AllPrincipals consent PATCH/readback passed. The zavagf
portal hook reconciled successfully and granted its existing Azure DevOps
delegated permission; dedicated media `Calls.AccessMedia.All` consent passed.
The cheapest unrestricted compatible x64 eastus2 catalogue candidate is
**Standard_D4als_v7**, four available vCPUs, 8 GB, family/regional quota
used0/limit100, Windows consumption **USD0.345/hour** (compute/licence only).
It is Gen2-only. Only one of the two proof cycles is complete;
[ADR 0031](adr/0031-greenfield-recovery-and-idempotent-hooks.md) records
the iteration and the historical failure below is not current authority.

New documented recovery parameters preserve existing defaults:
`MEDIA_VM_IMAGE_SKU=2022-datacenter-g2` selects the compatible image;
`MEDIA_VAULT_PURGE_PROTECTION=false` is used **only for disposable zavagf**.
The vault keeps soft delete, Entra RBAC and protected secret delivery.
The false parameter omits the API property: Key Vault create/update rejects
an explicit `enablePurgeProtection=false`, even on a new vault.
Purge protection defaults true everywhere else and cannot be disabled after
creation. Selecting false before this new vault is created permits the
explicitly authorised `down --purge` without a seven-day wait. No existing
zavaw3 vault protection or tenant policy is changed.
The postprovision registry role gate confirms AcrPull before image deployment.
Native hooks admit the exact zavagf/eastus2 group as well as the original
zavaw3/westus3 group; this run executes **only zavagf**.

The following iteration chronology records historical checkpoints, not
currently running operations. The current outcome below is authoritative.
Initial old-group purge passed1622.63s and empty-group readback was false.
First empty up from88d79a9 failed after492.54s with that explicit-false
Key Vault BadRequest. The selected VM itself allocated successfully with
a127GB StandardSSD_LRS/V2 OS disk. ADR0031 records the omission correction;
the failed partial footprint is purged before retrying. Completed cycles0/2.
That purge passed1025.01s; exact group absence=false and corrected preview
29s pass. First empty-up retry started05:09:56Z from35812cd.
It passed1828.77s,exit0,ending05:40:25Z, then entered mandatory verification.
Complete verification subsequently passes8/8 in344.83s, with one actual
approver-authored ADO comment and complete audit/revision55. Completed
cycles1/2; intervening purge runs before the second empty up.
Intervening purge passes1639.20s, including the real media vault and Foundry
account; group exists=false and matching deleted-name lists[] read back.
The second empty up/verify remains required.
The second iteration exposed an input/output naming collision:
`down --purge` removed MEDIA_HOST_ENABLED, so its submitted native parameter
was false. That smaller deployment is not a full G8 cycle. The template now
keeps MEDIA_HOST_ENABLED input-only; configured flags survive output cleanup.
For recovery from the old colliding template, purge its invalid footprint,
then restore the documented parameter with
`azd env set MEDIA_HOST_ENABLED true -e zavagf` before the next empty up.
This is parameter recovery, not a manual resource or a hidden default.
Always read back the true opt-in before a full G8 up/verify.

The invalid second up exited1 after1036.69s with a Foundry409 workspace
identity change during hosted version creation (request
279ff8f7e9fdc6c39b993d57de16d99f). AgentDeploy now follows the service's
explicit instruction: read the current agent and retry that exact conflict
at most twice. Authentication, unrelated conflicts and failed reads are
not retried. Reduced-footprint purge passed1742.02s with complete absence;
all13 documented inputs were read back before the full native-enabled empty
restart at07:15:07Z. Its subsequent mandatory verification is recorded below.

**Current outcome: blocked, completed cycles 1/2.** The full second empty
up ended 07:35:37.0795566Z after 1229.5552 s, exit 1. Native was actually
enabled; all three hosted-version creation calls returned the exact
workspace-identity 409 despite two successful current-agent reads. Final
request: `f5e2882b465eff0ce347b4f56906cecc`. No more equivalent creation
retries were attempted. Current project identity is SystemAssigned,
`3bce4b0f-7f02-4430-aff8-5bd65045e8a2`; the retained owned agent
`zava-voice-relay`, version 1, still exposes the first cycle's image and
instance principal `ff6d1ab3-68c0-47c9-aa11-894f161b4db2`.
Whether separate data-plane lifecycle cleanup or a stale provider binding
caused this is unproven; no speculative deletion or identity toggle was made.
The subscription/project owner should escalate the exact request, identities
and project ARM id to Microsoft support before another full empty proof:
`/subscriptions/4b0dd31f-5379-4acc-8dcd-60f2929d2513/resourceGroups/rg-zava-scrum-zavagf/providers/Microsoft.CognitiveServices/accounts/fdy-zava-scrum-pi7fumnppr2hk/projects/proj-zava-scrum`.

**G8-PRIVILEGED-WRITE, attempted and blocked:** independent media completion
failed after 7.9184 s at the application reconciliation write, before
certificate/bootstrap. The exact one-attempt response was:

```text
ERROR: Forbidden({"error":{"code":"Authorization_RequestDenied","message":"Insufficient privileges to complete the operation.","innerError":{"date":"2026-11-27T07:40:44","request-id":"07d95ae3-fd0a-4479-9442-9e7d14dd3abf","client-request-id":"07d95ae3-fd0a-4479-9442-9e7d14dd3abf"}}})
```

An authorised Entra administrator must restore appropriate active
application-write authority for `admin@MngEnvMCAP763514.onmicrosoft.com`
on the existing zavagf calling app `aef240bc-3943-4f28-83f2-c7829c69a847`,
application object `25ab2fd7-c291-4560-b8c4-3a755b19697f`. The authorised
deployment operator then runs
`azd exec -e zavagf -- pwsh -NoProfile -File infra\hooks\media-host.ps1`,
including certificate registration, not a RuntimeOnly bypass.
Earlier successful consent writes do not establish current write authority;
their grants remain complete. There was no authentication retry or role
activation. No zavaw3/mcm object or tenant policy was changed.

Independent public legal exclusions and messaging package generation both
passed, exit 0, in 4.1405 s total. The package is
`appPackage\dist\zava-scrum-agent-zavagf-1.1.0.zip`,
id `df7bb1a7-ba3c-4bae-839b-e1ea1a628d2f`; no catalogue authentication was
attempted. Hosted identity reconciliation requires the failed hosted output
and was not run. The surviving footprint has 24 ARM resources including
the D4als_v7/127 GB StandardSSD_LRS/V2 VM and disposable vault. Provisioning
is not certificate/task/renderer readiness: the current native host is
unbootstrapped. It remains allocated for authorised continuation at a known
VM/disk/IP tariff of USD0.363150685/hour plus variable charges.
The first cycle's native readiness below is historical, not current readiness.

Final mandatory verification independently passed **8/8 in 361.4580 s**,
07:46:27.3643022Z to 07:52:28.8223331Z, source `7501b98`. It explicitly
printed `MEDIA-TLS: native readiness NOT verified`; optional VM/DNS hosting
success is not native acceptance. The Direct Simulated Room completed
10 timed turns, total p50/p95 **2821/4189 ms**, questions 2836/4189 ms (n=7),
9 model-read turns, 15 human segments and zero talk-overs.
Real approved comment on item 1 was authored by the approver; revision 58,
audit `zsa-1e117c8f2430`, session `17190e28-c4cc-409c-bb6a-a87e23c175f1`.
Passing independent verification cannot turn the failed full up into cycle 2.
Against first complete verification 3251/3682 ms (n=9), final p50 is -13.23%
and p95 +13.77%; the tail point exceeds the 10% tolerance. These are
different time windows, not causal or paired native evidence; G1 remains failed.

**ZAVAGF-CATALOGUE, attempted and still human-gated:** an actual authorised
POST of the exact new media package returned403 Forbidden at05:41:36Z:
`Missing scope permissions on the request. API requires one of
'AppCatalog.Submit, AppCatalog.ReadWrite.All, Directory.ReadWrite.All'.`
Requestf5606812-a404-4556-9256-78921efeb3a5. No sign-in retry was attempted.
A Teams administrator must use Teams admin centre > Teams apps > Manage apps
> Upload new app for `appPackage\dist\zava-media-bot.zip` (current zavagf
calling appaef240bc-3943-4f28-83f2-c7829c69a847) and
`appPackage\dist\zava-scrum-agent-zavagf-1.1.0.zip`, independently regenerated
after the failed up. Regenerate the media package after authorised bootstrap.
Alternatively obtain delegated AppCatalog.Submit through
the documented interactive package hook. Do not upload historical zavaw3
packages again, replace the old mcm app or mistake a role for OAuth scopes.
This does not undo the completed zavaw3 uploads or successful core Entra hooks.

### Historical finalisation attempt (superseded prerequisites)

Only the old **zavagf/eastus2**, resource group `rg-zava-scrum-zavagf`,
subscription `4b0dd31f-5379-4acc-8dcd-60f2929d2513`, is authorised for
this destructive proof. Never tear down zavaw3 or touch mcm. The inspected
old group contains 13 resources; an update is not an empty-environment run.
One full preflight took **231.30 s**, 2026-11-27 03:04:32Z to 03:08:23Z,
and exited 1 with:

```text
FAIL quota: Windows VM Standard_D4s_v5
Not enough  vCPU quota in eastus2
1 check(s) failed. Nothing was provisioned.
```

No `up`, teardown, new resource or successful verification cycle is claimed.
Both required full-solution up/verify cycles remain **not_run, n=0**.
The diagnostic read with `az vm list-skus --all` establishes the actual
cause: Standard_D4s_v5 has **NotAvailableForSubscription** location and
zone restrictions in eastus2. Both DSv5-family and total regional quota are
used0/limit100. This is SKU availability, **not exhausted vCPU quota**;
the preflight's missing-family message is misleading. Public metadata
evidence is `gates/g8-M11.json`.
The preflight Graph scope check passes, but read-only live directory-role
assignments list Agent Registry Administrator, Agent ID Developer, Agent ID
Administrator and Global Reader, not an application/privileged consent
administrator. The explicitly authorised Global Administrator window ended
at 14:27 PT on 26 November; cached OAuth scopes do not prove current role
authority. No role activation, consent mutation or authentication loop was run.

**G8-DEPLOYER-AUTHORITY, initially completed; later write refused:** actual
Global Administrator and privileged operation readback passed as recorded
above, but the later G8-PRIVILEGED-WRITE refusal now blocks native completion.
For any later recovery, an authorised Entra Application Administrator
(application changes) and Privileged Role Administrator/Global Administrator
(required tenant consents) must run the existing hooks for **zavagf**, using
their own approved active role. A Subscription Owner or equivalent scoped
resource/RBAC permissions and Azure DevOps Project Collection Administrator
are also required. Do not grant unrelated roles or change tenant policy.
Revalidate actual role authority, not just the token's `scp` field.

**G8-MEDIA-CAPACITY, catalogue/quota prerequisite resolved:** the operator
authorised SKU selection and Standard_D4als_v7 was selected as recorded
above. Actual allocation succeeded in both the completed first cycle and the
current partial footprint. For a future run,
the deployment owner must review
`az vm list-skus -l eastus2 --size Standard_D4s_v5 --all` and its
`NotAvailableForSubscription` restrictions. Approve an actually available
minimum-four-vCPU alternative through the existing `MEDIA_VM_SIZE` parameter
under existing eastus2 subscription capacity, then record its catalogue
restrictions/family/quota before a new approved run. Do not retry the unavailable
default or interpret used0/limit100 as a quota shortage.
Do not file a quota request, change subscription/region or silently omit
the native host to label the partial footprint complete.

After those prerequisites and fresh validation pass, an authorised operator
runs the following **only for zavagf**, recording UTC start/end, commit,
resource inventory and actual verification output for each numbered run.
The initial teardown is needed because the old group is currently nonempty.
Do not delete it while rebuilding remains blocked.

```pwsh
# Confirm the intended subscription, environment and eastus2 group first.
azd env set MEDIA_HOST_ENABLED true -e zavagf
azd env set MEDIA_VM_SIZE <approved-available-four-vCPU-sku> -e zavagf
azd env set MEDIA_VM_IMAGE_SKU 2022-datacenter-g2 -e zavagf
azd env set MEDIA_VAULT_PURGE_PROTECTION false -e zavagf
azd env set VOICE_ROUTE Direct -e zavagf
azd env set BRIDGE_MODE Acs -e zavagf
azd env set BOARD_MODE Share -e zavagf
# Only after preflight and current deployment validation pass:
azd down --purge --force --no-prompt -e zavagf
azd up --no-prompt -e zavagf
azd exec -e zavagf -- pwsh infra\hooks\verify.ps1 -GateOutput <run-1-folder>
azd down --purge --force --no-prompt -e zavagf
azd up --no-prompt -e zavagf
azd exec -e zavagf -- pwsh infra\hooks\verify.ps1 -GateOutput <run-2-folder>
```

`verify.ps1` already runs the GateRunner subset and prints eight mandatory
PASS/FAIL checks, including portal approval/author/audit. Its optional native
hosting check is not G6 or a human meeting test. It does not print a complete
formal G1-G11 verdict; that broader gate-status enhancement belongs to the
remaining full prompt, not this documentation-only subset. Record actual
failed/not_run gates separately, including G7 dates and G10/G11 n=0.
Exchange, catalogue, organiser RSC and real-user actions below remain exact
human gates; package generation is not tenant publication.

## Edition C owner actions (final consolidation)

**FILM-SCOPE, James Ford/product owner and Scout:** before booking a take,
choose the truthful recording scope. The original native-media chapters and
saved spoken follow-ups cannot be one demonstrated path: GraphMedia is
non-recording and cannot persist meeting-derived proposals. Use
**Acs / Share / Direct** for the approval story and label the Simulated Room
fallback. Film native join/share/identity/barge-in separately only after the
existing MEDIA-RSC, native voice and NATIVE-BOARD-ACCEPTANCE checks have real
samples. Do not describe an admitted lobby join as unattended G6 success,
the earlier smaller-footprint rebuild as final G8, or unbuilt knowledge/
Work IQ as delivered. Conditional Access policy changes remain outside this
run. Record the approved chapter substitutions and outstanding gates in the
film brief before narration or capture; no Presenter Pack change is authorised.
The dedicated audit portal page, Live per-hop panel, complete readable speaker
names and atomic Sprint 43/roster/cast reset are still separate product work.

**FILM-ASSETS, James Ford (recordings/backup owner) and Scout (film tooling):**
restore the authorised Edition B tooling, raw clips and original seven Admin
recordings from the owner's backup into a separate film workspace. The plan
names `C:\Projects\film-work`; its absence was recorded on 24 November, not
rechecked by final consolidation. Neither `video\README.md` nor
`video\admin-lines\record-admin-lines.html` exists in this checkout, and no
tracked film/recording assets were found. If the backup is unavailable,
Scout must first rebuild the capture/audio preflight/persona host/assembly
tooling and recording page in that workspace before asking James to record
the seven original and three new interruption lines (08 to 10) in his own
voice. Do not substitute TTS for the presenter's interruption.
Obtain consenting participants and complete real meeting/Copilot dry runs
before recording. Keep Daniel/Maya synthetic and labelled; use light theme,
right-monitor native 16:9 capture, eased scrolling, separate pre-brief/recap
clips and labelled fast-forwarded waits. Deliver the chaptered MP4 and
transcript only for the owner-approved, evidence-backed scope; the original
fourteen-chapter 15-to-17-minute plan is not currently ready to film as built.

## Parameters

Set once with `azd env set`. None is a secret.

| Name | Required | Meaning |
| --- | --- | --- |
| `AZURE_LOCATION` | yes | Must offer the configured models, Voice Live and selected native host. Earlier eastus2 API/bridge proof does not prove the final footprint. |
| `AZURE_TENANT_ID` | yes | The Entra tenant. |
| `ADO_ORG_URL` | yes | For example `https://dev.azure.com/your-org`. The project and seed data are created inside it. |
| `ADMIN_UPN` | yes | The administrator. Also the calendar organiser. |
| `REALTIME_SKU` | no | `DataZoneStandard` (default in `main.parameters.json`) or `GlobalStandard`. Global realtime quota is subscription-wide and was used by the original environment, so the default avoids it. |
| `VOICE_ROUTE` | no | `Direct` (default) or `Hosted`: whether the API reaches Voice Live directly or through the hosted agent relay (ADR 0021). Set it before `azd provision`; the agent itself is created by `agent-host.ps1`. |
| `MEDIA_HOST_ENABLED`, `MEDIA_VM_SIZE` | no | `false` by default; opt-in public Windows media VM, `Standard_D4s_v5` (four vCPU), hard call cap 1. `D3_v2` was unavailable in `zavaw3`/`westus3`. |
| `MEDIA_VM_IMAGE_SKU` | no | `2022-datacenter` by default; select `2022-datacenter-g2` for Gen2-only sizes such as the G8 eastus2 D4als_v7. Preflight checks image generation, x64 and available cores. |
| `MEDIA_VAULT_PURGE_PROTECTION` | no | `true` by default. `false` is for explicitly disposable teardown/rebuild proof environments only, before vault creation; existing protection cannot be disabled. Soft delete remains enabled. |
| `MEDIA_SERVICE_FQDN` | no | Optional certificate-covered DNS name. Empty uses the Azure public IP DNS label; the hook obtains a Let's Encrypt HTTP-01 certificate and imports directly from the VM to Key Vault. |
| `BRIDGE_MODE` | no | `Acs` (default), `Simulated`, or `GraphMedia`. Select native media only after HTTPS bootstrap and per-meeting RSC installation. The voice factory supports Direct or Hosted; preserve the working route until measured. |
| `BOARD_MODE` | no | `Share` (ACS canvas, default) or `MediaShare` (VBSS on GraphMedia). Bicep sets API `Board__Mode`; image-only updates preserve the current setting. No native browser or additional resource. |
| `CHAT_CAPACITY`, `REALTIME_CAPACITY`, `DECISION_CAPACITY` | no | Deployment capacity. Defaults 100, 10, 100. Below 100 the decision model hits its rate limit. `REALTIME_CAPACITY` applies to each model in the `realtimeModels` list in `infra/main.bicep` (default `gpt-realtime-2.1` only). Quota is one row per model, so a model you do not deploy frees nothing for another. |

```pwsh
azd env new <name>
azd env set AZURE_LOCATION eastus2
azd env set AZURE_TENANT_ID <tenant id>
azd env set ADO_ORG_URL https://dev.azure.com/<org>
azd env set ADMIN_UPN <admin upn>
azd up
pwsh infra/hooks/verify.ps1
```

Resources are named with the `zava-scrum` prefix. The original environment (`mcm-scrum-demo`, resource group `rg-mcm-scrum-demo-wus3`) keeps its `mcm` names until it is retired. Do not run `azd provision` for it with these templates: they would create a second set of resources, not update that one. The previous templates are in git history at commit `84498ad`.

## What runs, in order

| Phase | Script or template | What it does |
| --- | --- | --- |
| preprovision | `infra/hooks/preflight.ps1` | Fails fast: tools, sign-in, Azure roles, resource providers, Graph scopes, Azure DevOps administrator, region, model catalogue and quota, Cosmos policy. |
| preprovision | `infra/hooks/entra-portal-app.ps1` | Portal app registration, `access_as_user` scope (consented for the organisation), Azure CLI pre-authorised, the three app roles from `infra/entra/portal-app-roles.json` (Admin, Approver, Reader) assigned to `ADMIN_UPN`; sets `PORTAL_CLIENT_ID` and `ADMIN_OBJECT_ID`. |
| provision | `infra/main.bicep` and `infra/modules/*` | Everything in Azure, including role assignments. |
| postprovision | `infra/hooks/registry-rbac.ps1` | Confirms provisioned registry-scoped AcrPull before azd builds/deploys private images; bounded five checks, explicit failure. |
| postprovision | `infra/hooks/entra-post.ps1` | Redirect URIs (web for Easy Auth, single-page application for MSAL.js), the managed identity's Graph `User.Read.All` permission (roster identity resolution), `Portal__ClientId` and `Identity__AdminObjectIds` on the API, and Easy Auth on both apps. |
| postprovision | `infra/hooks/ado-setup.ps1` | Azure DevOps project, area, iterations, seed items, sprint; adds the managed identity to the organisation. |
| postprovision | `infra/hooks/exchange.ps1 -Mode Apply` | Switches calendar and email to Graph once the Exchange grant exists. |
| deploy | azd | Remote ACR builds of both images, then the revisions. |
| postdeploy | `infra/hooks/teams-app.ps1` | Builds `appPackage/dist/zava-scrum-agent-<env>-<version>.zip` with the deployed bot id. |
| postdeploy | `infra/hooks/media-host.ps1` | Opt-in only: separate calling app/Bot registration, minimum media permission, artefact upload, vault delivery, Windows startup task and native Teams package; reports TLS/RSC gates. |

Hooks are intended to reuse their tagged objects. A re-run can create an
immutable hosted version and rotate Easy Auth credentials; it is not a no-op.
Final repeatability and native eastus2 provisioning remain unverified.

## Identity model

- One user-assigned managed identity runs both apps. It reads and writes Azure DevOps (it is added to the organisation, ADR 0015 S4), calls Foundry, Cosmos DB and ACS, and is the Azure Bot's identity (`UserAssignedMSI`). There is no bot client secret, and the Teams `botId` is the identity's client id.
- Easy Auth supports only a client secret, not a federated credential. The hook creates one credential on the portal app and stores it only as the Container Apps secret `portal-secret`. Older `easyauth-*` credentials are removed on each run.
- Writes to Azure DevOps use the approver's own token (on-behalf-of). The API proves the portal app with the managed identity through a federated credential, so there is no client secret for it; Teams card actions sign in through a Bot Service OAuth connection that also uses a federated credential (`entra-post.ps1`).
- The bridge key is generated by the deployment (`newGuid()`) and kept as a Container Apps secret.
- The Azure CLI client is pre-authorised on the portal API scope, so `verify.ps1` and GateRunner call the deployed API with `az account get-access-token --scope api://<PORTAL_CLIENT_ID>/access_as_user`.

## Who can do what (ADR 0017, 0018)

The API validates the Entra bearer token itself; Easy Auth is an outer gate only. Three app roles on the portal app registration: `Zava.Scrum.Admin` (every team and the team registry), `Zava.Scrum.Approver` (approve or reject, for a team the person is in) and `Zava.Scrum.Reader`. The hook assigns all three to `ADMIN_UPN`. To give another person a role: Entra admin centre > Enterprise applications > *Zava Scrum Portal (<env>)* > Users and groups > Add user/group (an Entra Application Administrator or the app owner).

**Team roles are separate and stored by the app** (ADR 0018): TeamLead, Member and Reader per team, keyed by Entra object id. An Admin or the team's lead manages them on the Team page, or syncs them from an Entra group set on the team (add-only). A person with no membership sees no team. In Teams, administrators are the object ids in `Identity__AdminObjectIds` (set to `ADMIN_OBJECT_ID`); other Teams users can only decide their own follow-ups once their roster entry has an Entra object id.

The approval trail is the Cosmos container `ApprovalRecords`. The managed identity has a custom data-plane role there with create and read only (`data.bicep`), and Cosmos DB Data Contributor on the `collections` container only. `azd down` removes the trail with the environment.

## Remaining secrets (defects)

- **`portal-secret` (Easy Auth client secret).** Container Apps Easy Auth accepts only a client secret, not a federated credential. It is stored only as a Container Apps secret, rotated on each hook run, and is not used for Azure DevOps or ACS. This is a known defect, to be removed by moving the outer gate to the in-app validation already in place (ADR 0017) or when Easy Auth supports federated credentials.
- No ACS key or connection string, no bot secret, no Azure DevOps PAT in the deployment (`Ado:AuthMode` `Pat` exists for local development only).

## Container registry permissions

The hosted exporter hook uses `AzureMonitor__ConnectionString` to configure
the existing .NET SDK directly. Foundry reserves
`APPLICATIONINSIGHTS_CONNECTION_STRING` for project-monitoring injection and
rejects a version declaring it. Do not enable project-wide content capture as
a workaround. API/bridge/native retain the ordinary setting; all hosts refuse
`Telemetry:CaptureContent=true`. The workbook update uses a temporary typed
parameter file because Windows `az.cmd` strips inline JSON quotes.

The registry is created with `roleAssignmentMode: LegacyRegistryPermissions`, set explicitly. In that mode the Foundry project identity needs **AcrPull**, not Container Registry Repository Reader (ADR 0015 S3). Both identities get AcrPull in `data.bicep`. If you switch the mode to ABAC, change the role to Container Registry Repository Reader.

## Human gates

The platform forces these. Each is named in the script output as `GATE`.

### Operator-completed authority, consent and catalogue actions

On 26 November 2026 at approximately20:00PT, Scout, signed in as the
authorised administrator, completed and verified:

- Zava Scrum Portal (zavaw3), service principal
  `2b69cbf8-a7e7-432c-8441-726b72a9fffc`: Graph AllPrincipals delegated
  `User.Read openid profile Files.Read.All Sites.Read.All`, and Work IQ
  AllPrincipals delegated `WorkIQAgent.Ask` on resource service principal
  `ca4a440d-9d38-41a8-a140-f5ff230ac975`, with matching app declarations.
  No WorkIQ.Act, settings or application permissions were granted.
- Tenant catalogue uploads: Zava Scrum Media
  `b681ab3c-023c-4e91-9212-50980b43dec1` and Zava Scrum Agent1.1.0
  `bed81a44-b97d-4687-9ad3-c4cf546f6689`. The old mcm app with
  `38a0a184-...` external bot id was not changed.

KNOWLEDGE-CONSENT, WORK-SIGNALS-CONSENT, COPILOT-ROLLOUT catalogue upload,
MEDIA-CATALOGUE and MEETING-EVENT-CATALOGUE are therefore **completed for
zavaw3**, not open administrator actions. Historical refusal logs below
remain evidence of earlier attempts. User installation/update/sign-in,
real fixtures, Copilot Credits spending approval, Exchange mailbox RBAC,
evaluation CI identity/variables and per-meeting organiser RSC remain open.
The native catalogue app installed for "No one" proves no meeting install.
The portal Entra hook now merges baseline requiredResourceAccess with
existing declarations idempotently; it never removes these approved optional
scopes or automatically grants them to a new environment. IQ runtimes remain
unbuilt and require their independent source/user/billing spike.

### WORK-SIGNALS-CONSENT, WORK-SIGNALS-BILLING and WORK-SIGNALS-FIXTURE (8B)

ADRs 0027 and 0034 record an unregistered boundary and real metadata-only
owner-OBO/discovery adapters, not an enabled content runtime. Operator consent
and redo readbacks supersede the original absent-grant/administrator-window
block. Consent and administrator sign-in are completed prerequisites.
The8B-r administrator OBO succeeds n2, initialisation n2 and tools/list n1,
HTTP200/12tools; complete discovery9.352s,n1 is not retrieval latency.
The probe used the existing Easy Auth credential only in memory; deployed
managed-identity federation is still unproved.
All content/source-enforcement/negative-user samples remain not_run,n0.
Do not alter the mcm applications, grant application permissions, change
tenant policy or create an unapproved billing plan.

**WORK-SIGNALS-CONSENT, completed by Scout as authorised administrator:**
the following procedure is retained for another approved environment,
not an outstanding zavaw3 action. Review the broad permission and approve the plan-specific
delegated **WorkIQAgent.Ask** on **Zava Scrum Portal (zavaw3)**, app
`265790b8-88a7-4975-bb8f-3fe381bb19ea`. In **Entra admin centre > App
registrations > this app > API permissions > Add a permission > APIs my
organisation uses > Work IQ > Delegated permissions**, select
`WorkIQAgent.Ask` (`0b1715fd-f4bf-4c63-b16d-5be31f9847c2`) and
**Grant admin consent**. Resource app is
`fdcc1f02-fc51-4226-8753-f668596af7f7`, URI
`api://workiq.svc.cloud.microsoft`; actual resource service principal is
`ca4a440d-9d38-41a8-a140-f5ff230ac975`. Verify the actual grant for portal
service principal `2b69cbf8-a7e7-432c-8441-726b72a9fffc`, not just the
declaration. This scope includes write access, although the integration
must use only read-only retrieval. Do not add `WorkIQ.Act`, settings,
Graph retrieval permissions or agent/application grants for this spike.
The resumed runtime PR must encode optional declarations/idempotent
consent handling in the greenfield Entra hooks before enabling the feature.

**WORK-SIGNALS-BILLING, Microsoft 365/Copilot billing administrator and
the spending-policy owner:** confirm an existing approved Copilot Credits
usage-based plan and a spending policy that includes Work IQ API; assign
the named calling team lead and verify limits/tenant availability. Record
the plan/assignment evidence without exporting credentials. A Copilot
licence alone and an existing Work IQ service principal prove neither.
If no approved plan exists, the responsible billing owner must approve and
perform setup; this unattended run must not create one or change policies.
Use Microsoft365 admin centre's Copilot Credits usage-based billing and
spending-policy pages to read the approved plan, Work IQ API inclusion,
named lead assignment and limits. Redo readback is **not_run**: ARM inventory
is not Microsoft365 billing evidence; browser automation reports missing
Chrome, and the alternative desktop browser lacks a verified page URL/tree.
Do not infer billing approval from successful metadata discovery.

**WORK-SIGNALS-FIXTURE, team lead, Teams/channel owner and meeting
organiser:** name a real consenting licensed TeamLead with a verified Entra
object id and current app-team membership. Approve one exact team group/
channel (its **Get link to channel** URL, not a whole-team/chat link) and
one stored team meeting with its meeting UID and existing notes accessible to the
team's intended evidence audience. Provide a real board item and topic,
an absent/silent member's channel update, roster helper evidence and a
review excerpt with expected backlog feedback. Provide a consenting
negative member from another app team and private/unrelated source
boundaries for permission/scoping checks. Each person must sign in as
themselves; an administrator cannot supply their delegated assertion.
No arbitrary tenant-root search or private mail/chat read is authorised.
Live `tools/list` discovery is complete; retain runtime rediscovery because
schemas evolve. Prove pre-retrieval TeamsMessages.urls for that exact
channel and Meetings.items_by_id with that meeting UID/is_series=false;
post-filtering broad personal results is not proof of no private reads.
Measure three spike uses/latency and G11 positives/negatives before shipping
only supported integrations. Unsupported exact source scoping stays unbuilt.

The metadata-only classes are included in the committed API artefact but
remain unregistered; no content runtime, source, resource, role, permission
grant or setting is added. The boundary has no manual enable switch.
Real content adapters, scope configuration, storage/retention, owner
connection/revoke UI, optional permissions and hooks belong in the resumed
greenfield PR, followed by zavaw3 deployment and `infra\hooks\verify.ps1`.

### KNOWLEDGE-CONSENT and KNOWLEDGE-FIXTURE (8A/8A-r, ADRs 0026/0033)

Prompt 8A remains unbuilt before its mandatory permission spike, not deployed.
Only zavaw3 and its existing portal application are in scope for that spike.
The27November2026 actual grant/declaration readbacks confirm both retrieval
scopes; KNOWLEDGE-CONSENT is complete, not blocked by the old administrator
window. The source/two-user fixture and actual retrieval spike remain outstanding.
The signed-in administrator has a Copilot licence and can read the SharePoint
root through Azure CLI; neither proves caller-OBO retrieval. Five current
application teams have no knowledge bases or stored memberships. No approved
corpus or denied-user fixture is established; do not create people or search
unrelated tenant documents to manufacture one. No permission-trimming request
was made, so leakage and retrieval latency remain unmeasured.
The8A-r administrator's genuine portal-to-Graph OBO exchange and matching
`/me` identity-only read succeed n1 each. The bounded probe reuses the existing
confidential-client credential in memory; it does not validate deployed
managed-identity federation or source retrieval. Do not reopen consent or
administrator sign-in as human gates; obtain the actual approved source and
real positive/negative members below.
Existing-zavaw3 mandatory `infra\hooks\verify.ps1` passes8/8 on8A-r,
including genuine approver-authored item1/revision59 and complete audit.
No knowledge runtime or resources were added, so nothing was redeployed.
Leave zavaw3 running and preserve its current adapters; source approval
cannot be automated or replaced with a privileged administrator's broad access.

**KNOWLEDGE-CONSENT, completed by Scout as authorised administrator:**
retain this procedure for another approved environment. For
**Zava Scrum Portal (zavaw3)**, app id
`265790b8-88a7-4975-bb8f-3fe381bb19ea`, approve the plan-specific delegated
Microsoft Graph permissions **Files.Read.All** and **Sites.Read.All**.
In **Entra admin centre > App registrations > this app > API permissions >
Add a permission > Microsoft Graph > Delegated permissions**, select both;
then **Grant admin consent** for this tenant. Do not grant application
permissions, ExternalItem permissions, tenant policy changes or access to mcm.
The resumed implementation must encode the same declarations and idempotent
consent handling in the Entra hook; this approval is not a substitute for
greenfield automation. Verify the actual portal service-principal grant
(`2b69cbf8-a7e7-432c-8441-726b72a9fffc`) before OBO testing.
The inherited hook already preserves approved optional declarations using
`Merge-RequiredResourceAccess`; do not replace or revoke the operator's
Graph or WorkIQAgent.Ask permissions during reconciliation.

**Standards owner / team lead and SharePoint site owner:** name the approved
organisation and team site/library URLs and real standards, Definition of
Done/Ready, working agreements and runbooks. Supply an approved Blob corpus
and explicitly decide which content is team-wide. Establish a restricted
SharePoint document accessible to the positive user and denied to the negative
user; record its document id and expected source. Do not ingest the tenant
root or unrelated documents by default. Supply ten real spike questions and
at least forty questions with expected document ids per knowledge base.

**Two real consenting team members:** sign in through the application's
delegated boundary in this tenant, with verified team memberships. The
negative member must have no access to the restricted document but must have
permission to run retrieval. Each needs a Microsoft 365 Copilot licence or
already authorised Retrieval API pay-as-you-go; do not enable billing silently.
Run identical positive/negative questions using each person's OBO token
forwarded as `x-ms-query-source-authorization`, never the administrator's token.
Prove positive access first, then absence of restricted content and citations
for the negative user. An empty or failed query is not permission proof.

**Resuming deployment operator:** after these prerequisites, add the real
Search service, sources, knowledge bases, scoped roles and MCP connection to
Bicep/hooks before deploying only zavaw3/westus3. Reviewed versions are Search
REST/MCP `2026-11-01-preview` and project connection ARM
`2025-10-01-preview`; recheck Learn on resume. Record actual versions, ten
retrieval samples and permission results before implementing consumers.
G10 needs correct citation rate, uncited answers, leakage and latency with
explicit denominators. No source means "I don't know"; infrastructure failures
must remain explicit. Keep knowledge off voice unless G1 is preserved.

### COPILOT-ROLLOUT and COPILOT-G5 (prompt 8, ADR 0025)

Only zavaw3 is in scope. Historically, the authorised Global Administrator window ended at
14:27 PT on 26 November 2026, before prompt 8 started. The signed-in Azure CLI
catalogue read was attempted with explicit Graph resource and refused:
`Forbidden: Missing scope permissions on the request. API requires one of
'AppCatalog.Submit, AppCatalog.Read.All, AppCatalog.ReadWrite.All,
Directory.Read.All, Directory.ReadWrite.All'.`
No authentication loop, new tenant policy or unrelated role was attempted.

**Catalogue upload completed by Scout as authorised administrator.**
The following is a repeat/update runbook, not an open upload gate:
in **Teams admin centre > Teams apps > Manage apps**,
open Zava Scrum Agent for bot/app id `bed81a44-b97d-4687-9ad3-c4cf546f6689`,
choose **Upload file**, and upload
`appPackage\dist\zava-scrum-agent-zavaw3-1.1.0.zip`. If absent, **Upload new
app**. Review/approve the permissions under existing policy, upgrading 1.0.5
or 1.0.4 without changing app id. Alternatively run
`pwsh infra\hooks\teams-app.ps1 -Interactive` as an allowed uploader, completing
the explicit AppCatalog.Submit device-code sign-in yourself. The unattended
conductor cannot complete it. Do not touch the old mcm bot.

**Each demo user / team lead:** accept the consent-required **Update** prompt,
install/open the updated app and select it in Microsoft 365 Copilot. Complete
the one-time sign-in in that context. A team lead must add each test user's
Entra membership and unique roster mapping; an Azure DevOps project
administrator must give the positive user project read access. Use a second
consenting team member without that project access for the negative test,
and a membership-free user plus a multi-team user for isolation. Ask all five
questions, verify source-card ids against that user's sprint and record
streaming, response time and refusals as G5 samples. No guest approval or
native meeting recording is authorised.

**Entra Global Administrator / Privileged Role Administrator, only if needed:**
verify/grant the existing Zava Scrum Portal (zavaw3) Azure DevOps delegated
`user_impersonation` consent in **Enterprise applications > Permissions**.
This is the same permission and federated OBO configuration from prompt 5A,
not a new permission or agent-identity grant. Prompt 8 readback confirms the
existing AllPrincipals `user_impersonation` grant on the portal service
principal, so no new Entra consent is needed or attempted in zavaw3.
No change to tenant consent policy. Evidence is retained in the run log.
An admin cannot force the user's Teams update or replace their user consent.

Greenfield owns the same runtime in the existing API image and existing
Entra hooks; no component is manually provisioned. `public-pages.ps1` adds
only `/privacy.html` and `/terms.html` to the existing API Easy Auth exclusions
without changing secrets/providers/protected routes, and runs in postdeploy.
`teams-app.ps1` fills all three bot/app/Copilot ids, SSO, legal URLs and exact
API/bridge domains and rejects a stale `TEAMS_APP_VERSION` downgrade.
Run `atk validate --package-file <zip> --telemetry false` before upload.

| Gate | Who | Action |
| --- | --- | --- |
| Deployer permissions | Subscription Owner (or Contributor plus User Access Administrator); Entra Application Administrator and Privileged Role Administrator (or Global Administrator) for the tenant-wide `User.Read` consent; Azure DevOps Project Collection Administrator | Checked by `preflight.ps1`. |
| Teams app upload | A Teams administrator, or a user allowed to upload custom apps | `pwsh infra/hooks/teams-app.ps1 -Interactive` (device-code sign-in; Graph accepts catalogue uploads only for a signed-in user with `AppCatalog.Submit`), or Teams admin centre > Teams apps > Manage apps > Upload new app, with the zip in `appPackage/dist`. Then approve the app. |
| Adding an Azure DevOps organisation | An Azure DevOps Project Collection Administrator of that organisation | Organisation settings > Users > Add users: add the API's managed identity (`id-<prefix>-<token>`, shown in the deployment outputs) with at least Basic access, then add it to the project's Contributors for each team project. Only organisations backed by this deployment's Entra tenant can be registered; the registry refuses another tenant. Then register the team (Teams page or onboarding wizard) and press Test connection. |
| Azure DevOps access for approvers | An Azure DevOps Project Collection Administrator (or project administrator) | Each person who approves needs to be a Contributor of the team's project, because an approval is written as that person (ADR 0020). Without it Azure DevOps refuses the write and the follow-up shows the reason. The managed identity no longer writes. |
| Admin consent for Azure DevOps | An Entra Global Administrator or Privileged Role Administrator | `entra-portal-app.ps1` grants the delegated Azure DevOps `user_impersonation` permission for the organisation. If the deployer cannot, do it in Entra admin centre > Enterprise applications > Zava Scrum Portal > Permissions > Grant admin consent. Without it every approval is refused with a consent message. |
| Deployer Entra roles | A Privileged Role Administrator | The deployer needs Application Administrator (or Cloud Application Administrator) to create the app registration, federated credentials and consents, and Privileged Role Administrator or Global Administrator for the tenant-wide consents. Global Reader is not enough: `entra-portal-app.ps1` stops with `Authorization_RequestDenied`. |
| Exchange calendar and mail scope | An Exchange administrator | `pwsh infra/hooks/exchange.ps1 -Mode Grant`. It gives the managed identity Application Calendars.ReadWrite and Application Mail.Send on the organiser's mailbox only (RBAC for Applications). Until then the calendar and email run on the outbox. |

## The hosted agent (prompt 4)

`infra/hooks/agent-host.ps1` runs in `postdeploy`: it builds `Dockerfile.agenthost` in the environment registry (`az acr build`, a small context), creates a new immutable version of the hosted agent `zava-voice-relay` (`tools/AgentDeploy`, needs the `Azure.AI.Projects` 3.0 beta) and assigns the agent's own identity Cognitive Services User and Azure AI User on the Foundry account. The agent identity exists only once the agent does, so those roles cannot be in the Bicep that runs first. The project identity already has AcrPull on the registry (the registry uses legacy permissions; Container Registry Repository Reader fails with `ImageError`). `canary.ps1` changes traffic; nothing else does. `voiceRoute=Hosted` (azd env `VOICE_ROUTE`) points the API at the agent.

Human gates for the hosted agent: none new. The greenfield environment's bot id and Teams app package remain the existing gate (Teams app upload, above); the old `mcm` bot id `38a0a184` is not used. The direct Foundry publish route is not used.

## Capacity and health

`Capacity__MaxConcurrentStandups` (default 3), `Capacity__StaggerSeconds` (5) and `Capacity__GraceMinutes` (10) limit how many standups start on one host; a standup that cannot start waits with a visible reason and the team is told after the grace. Realtime model capacity is `realtimeCapacity` in `infra/main.bicep` (`REALTIME_CAPACITY`); the GlobalStandard quota in the region must cover one deployment per concurrent standup you intend to run. Both container apps have HTTP startup, readiness and liveness probes on `/healthz`, so traffic reaches a new revision only when it answers.

## Checks

- `infra/hooks/verify.ps1` prints PASS or FAIL per check: health routes, the Easy Auth bearer path, the bot endpoint rejecting unauthenticated calls, a Simulated Room standup through the gate harness, a follow-up approved through the API and its comment found in Azure DevOps. It retries the two health routes for up to three minutes, because a new revision can still be starting.
- CI (`.github/workflows/infra.yml`) lints the Bicep and runs `az deployment sub what-if`. ARM cannot expand nested modules that depend on earlier outputs, so what-if shows the resource group and outputs only; `azd up` in a scratch group is the full proof.

## Content-free observability and evaluation (M11 9, ADR 0028)

Empty-environment Bicep creates the shared Insights workbook and supplies the
existing connection string to API/bridge. Hosted/native hooks fetch that
setting without printing or storing it in azd; native delivery uses the existing
vault, Secrets User and protected SYSTEM settings. No new role, SKU or policy.
`Telemetry__CaptureContent=false` is explicit; true is refused at startup.
MAF/GenAI messages and application log-body export are off. Fixed added cost
USD0/hour; normal variable Insights ingestion/retention and build charges remain.

For the authorised existing `zavaw3`/`westus3` environment, after validation and
the deployment workflow, use only these scoped hooks, never full provision:

```powershell
pwsh infra\hooks\observability-update.ps1
pwsh infra\hooks\deploy-service.ps1 -Service api
pwsh infra\hooks\deploy-service.ps1 -Service bridge
$env:AZURE_TOKEN_CREDENTIALS = 'AzureCliCredential'
pwsh infra\hooks\agent-host.ps1 -Tag <committed-source-tag>
pwsh infra\hooks\media-host.ps1 -RuntimeOnly
pwsh infra\hooks\verify.ps1 -GateOutput .m11-plan\run-log\p9-after
```

The native runtime-only hook refuses missing existing consent or an unregistered/
near-expiry certificate before stopping the host, skips Entra/registration/
certificate renewal writes, verifies its package hash and restarts the existing
SYSTEM task. Full greenfield/renewal hooks retain the existing consent gates.
The registered thumbprint must match exactly; Graph's Binary identifier may
be returned as hex or base64. Runtime-only accepts those two encodings, not a
new credential.
The hosted version does not change traffic/default voice selection. Preserve
Acs/Share/Direct. Native media still cannot save decision/approval content.

Open the shared workbook in the environment's Insights resource; select one
team. Query its actual live role/hop, join-event and approval-chain tables.
Counter counts describe events, not unique successful meetings. Compare the
gate's trace id to `operation_Id`; inspect only allowed metadata, never full
employee bodies. Record exact samples and missing hops in G7. Native/vendor
and multi-team human acceptance are not established by a Direct scripted run.

Step9 live handoff: API/bridge8859a7a revisions84/9 are latest=ready; native
e8c95b6 has the checked DLL hash, running SYSTEM boot task and content-off
settings. All8 mandatory checks pass, including approver-authored revision52
and exact audit chain. Saved workbookb628a627-0f45-5727-a57e-a4f367cc4bf4
matches source; actual metadata/latency evidence is in
`docs/gates/observability-M11.json`. Non-telemetry hashes, scale, identity and
secret names were preserved.
Hosted5 is an unpromoted candidate: both fresh own sessions exceeded readiness,
the paired probe returned424 without a completed report, and both sessions
were stopped (platform log confirms Stopped). Stable selector4 remains100%.
Investigate the candidate startup/readiness log during a bounded synthetic
session before promoting; never switch defaults on version-active metadata
or an old ready revision's health. Temporary session compute/model/ingestion
is variable-cost; no unverified hourly tariff is claimed.

G7 release runs the real synthetic decision command in
`demo/evaluation/README.md`. The existing OIDC CI identity and
`EVALUATION_PROJECT_ENDPOINT`/optional `EVALUATION_DECISION_DEPLOYMENT`
variables enable its daily real-model report; absent configuration is not_run,
not fabricated evidence. Reporting is non-blocking, `--release-check` is not.
The checked-in final n72 report fails dates75% on n8; retain that failure.
Actual repository variable names were empty at the step9 check, so the
real-model CI job is not enabled here. **EVALUATION-CI-IDENTITY:** the repository
and Azure identity owners must approve a least-privileged model-inference OIDC
identity/federation for this repository, then configure `AZURE_CLIENT_ID`,
`AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` and `EVALUATION_PROJECT_ENDPOINT`
under repository Settings > Secrets and variables > Actions > Variables.
Do not expose the broadly privileged runtime identity or a client secret to CI.
The daily workflow starts after merge to the default branch; this run leaves
the PR in draft. The rules report runs without Azure credentials.

**FOUNDRY-EVAL-PRIVACY prerequisite:** live Foundry quality/safety/groundedness
evaluation requires captured input/output messages. This deployment contract
prohibits employee content, so no valid employee-trace schedule is enabled and
no score is claimed. A product/data owner may supply an explicitly approved
synthetic full-input/output and source-ground-truth corpus for a separate
synthetic Foundry evaluation, using the checked-in tone/cited-item/refusal
rubric. Do not enable employee content or waive privacy to close G7.
Knowledge and Work IQ evaluations still require their existing8A/8B source/
real-user fixtures and remain n0. Both zavaw3 delegated administrator consents
are completed; source approval, member consent and Work IQ billing remain
separate prerequisites. This observability step grants no permissions.

## Teardown

```pwsh
azd down --purge --no-prompt -e zavagf
```

Only the explicitly authorised disposable `zavagf/eastus2` proof may use
this command; never tear down `zavaw3` or `mcm`. The guarded `predown` hook
first deletes the two owned hosted agents and reads back absence.
`--purge` then releases the soft-deleted Foundry account so the same name
and quota can be reused. Account purge alone did not remove the observed
retained data-plane workspace binding (ADR0035). The Entra app, Azure
DevOps project and Teams catalogue entry remain for the next `azd up`.

## Known limits

- Provisioning takes about 20 minutes; the Container Apps environment alone took 13.5 minutes in the proven run.
- Hosted transport is measured in West US 3; native eastus2/final-footprint acceptance remains open. Optional relay 4 is retained; candidate 5 is unpromoted.
- Previous G8 proof failed at1/2 with409/403. Second proof is provider-blocked at0/2 new cycles: full up1205.4765s fails persistent project404 after version creation. Independent native/Direct checks do not credit that cycle. Exact provider action/final VM-only state are recorded above and in ADR0035; zavaw3/mcm remain untouched.
- A new-subscription run was not possible: the work used one subscription and tenant.

## Native media preparation and human gates (6a/6b, ADR 0023)

Use only `zavaw3`, `westus3`; never use `mcm` or its Entra objects. Keep `BRIDGE_MODE=Acs` while bringing up the alternative. For an empty environment the existing `azd up` templates include the opt-in module. For an existing environment, the scoped helper deploys that same module without resetting working Container Apps to greenfield placeholder images or rotating their secrets.

```pwsh
azd env select zavaw3
azd env set MEDIA_HOST_ENABLED true
azd env set MEDIA_VM_SIZE Standard_D4s_v5
azd env set BRIDGE_MODE Acs
pwsh infra\hooks\media-host.ps1 -ConsentOnly
# After the validation/deployment contract passes, for an existing environment:
pwsh infra\hooks\media-provision.ps1 -ValidateOnly
pwsh infra\hooks\media-provision.ps1
pwsh infra\hooks\media-host.ps1
pwsh infra\hooks\media-app.ps1 -Fqdn <MEDIA_FQDN>
pwsh infra\hooks\verify-media.ps1 -RequireReady
```

The opt-in module uses the pinned AVM Windows VM resource with four vCPU, one-call cap, a public IP and TCP 443/8445. TCP 80 serves only the temporary HTTP-01 challenge listener. The subnet exists solely for the Azure NIC; there are no private endpoints. The VM identity gets Key Vault Secrets User and Certificates Officer on its media vault, and Blob Reader on its artefact container. The named deployer gets certificate/secret operator roles on that vault and Blob Contributor on that container. No RDP ingress, tenant-wide joining role or extra tenant policy is added.

The postdeploy hook creates the dedicated `zava-media-zavaw3` calling app/service principal, Azure Bot and Teams calling webhook; it never changes the existing messaging bot identity. An Entra Privileged Role Administrator/Global Administrator must be able to consent to the minimum `Calls.AccessMedia.All` grant; if automation is refused, grant that **application** permission on the dedicated calling app only in Entra admin centre > App registrations > the calling app > API permissions > Microsoft Graph, then Grant admin consent. Do not add `Calls.JoinGroupCall.All` as a workaround. Per-meeting joining is the package's RSC permission, not this tenant-wide media permission.

| Gate | Who | Exact action |
| --- | --- | --- |
| `MEDIA-TLS`, only if automated issuance fails | DNS owner and Key Vault certificate operator for `MEDIA_VAULT_NAME` | Point a certificate-covered `MEDIA_SERVICE_FQDN` at output `MEDIA_PUBLIC_IP`; obtain a CA-trusted exportable PFX with its private key/full chain, the FQDN as primary name and more than 14 days remaining. Keep it outside the repo in a protected folder. Import with `az keyvault certificate import --vault-name <MEDIA_VAULT_NAME> --name media-tls --file <protected-empty-password-PFX>`, then rerun the hook. The normal Azure DNS label uses automated HTTP-01, not this fallback. |
| Calling-app consent, if refused | Entra Privileged Role Administrator or Global Administrator | Perform the dedicated-app consent described above, then rerun the hook. **Completed in 6b:** app `b681ab3c-023c-4e91-9212-50980b43dec1` holds only `Calls.AccessMedia.All`, verified by Graph readback during the authorised admin window. Never grant a broader join role. |
| `MEDIA-CATALOGUE`, completed for zavaw3 | Scout as authorised administrator | Uploaded `appPackage\dist\zava-media-bot.zip` to the tenant catalogue, app b681ab3c-023c-4e91-9212-50980b43dec1. Earlier Graph scope refusal remains historical. Installed for "No one"; organiser meeting install/RSC is still required. A different environment needs its own generated package/upload. |
| `MEDIA-RSC` | Each meeting's organiser, permitted by existing tenant policy | In the stored scheduled meeting's Teams chat, add the published **Zava Scrum Media** app and accept its resource-specific `Calls.JoinGroupCalls.Chat` permission for that chat before the ten unattended G6 joins. Catalogue publication and tenant media consent do not grant this meeting consent. If installation is prohibited, record the gate and retain ACS; do not alter tenant policy. |
| Lobby admission | Meeting organiser | If existing meeting policy still sends the bot to the lobby, admit **Zava Scrum Media** from the meeting's lobby. Record that as a failed no-manual-step G6 case, not a successful unattended join. Do not relax tenant policy. |
| Native voice acceptance | Meeting organiser plus Daniel, Maya and Admin as distinct signed-in Teams participants | Use the stored calendar link; perform ten joins without admission/manual steps, label each participant's speech and overlaps for >=95% G2, and interrupt the agent with >=350 ms real speech. Measure audio cessation <=500 ms, cancellation/truncation and the spoken answer, including opening/recap states. Run native per-hop G1 before changing the default. Synthetic identity-tagged bytes are not these acceptance samples. |
| `MEETING-EVENT-CATALOGUE`, completed for zavaw3 | Scout as authorised administrator | Uploaded Zava Scrum Agent1.1.0, app bed81a44-b97d-4687-9ad3-c4cf546f6689, to the tenant catalogue. Earlier step7 missing-scope refusal remains historical. User update and organiser OnlineMeeting.ReadBasic.Chat installation/consent are still required. |
| `MEETING-EVENT-RSC` | Scheduled meeting organiser and team administrator/lead | Add the published **Zava Scrum Agent** to the actual stored meeting chat and accept `OnlineMeeting.ReadBasic.Chat`. Bind that exact conversation to the correct team using the existing Bind card as a team lead/admin. The messaging bot keeps SSO; the media app's join consent alone does not grant event delivery. Start and end a real meeting, verify the API's matched lifecycle metadata and exactly one end/recap path; an unbound/other-team event must not end it. |
| `NATIVE-BOARD-ACCEPTANCE` | Meeting organiser, members and a guest | After both apps are installed and a fresh session uses the stored meeting/verified roster, select GraphMedia/MediaShare as in DEMO.md. Without participant action, time the board's first appearance and each change to <=2 seconds for every participant, including the guest. Observe share stop/reconnect; sample VM process encoder CPU throughout one meeting against the unchanged cap 1, and run native G1 with the share running. Record sample sizes and failures. Renderer startup/goldens or Simulated Room are not these samples. |

After importing the certificate, rerun `pwsh infra\hooks\media-host.ps1` in the selected `zavaw3` environment. It publishes win-x64, uploads a hash-addressed private blob with Entra, delivers the existing bridge key to Key Vault, bootstraps the VM using IMDS tokens, verifies the ZIP hash/trusted certificate/hostname, installs the signed Microsoft VC++ runtime if needed, registers the public certificate on the calling app and creates a SYSTEM startup task. Only the public certificate is returned by run-command. Secrets/private keys are not parameters or printed. It produces the Teams ZIP and checks `https://<MEDIA_FQDN>/healthz`. Task restarts are configured; a failure is explicit, not a successful fallback.

`media-certificate.ps1` pins Posh-ACME 4.32.0, reserves the challenge URL for SYSTEM, and imports a full-chain PFX directly into Key Vault using a random in-memory export password. ACME state is protected under `C:\ProgramData\ZavaMedia\acme`; no private key or password enters run-command output. **Verified in 6b:** Let's Encrypt issued the Azure DNS-label certificate, imported as `media-tls`, expiring 2027-02-24 19:22:03 UTC. Rerun `media-host.ps1` at least monthly and before certificate expiry: within 30 days of expiry it renews, imports, updates the app's public certificate and restarts the task. There is no unattended renewal scheduler; do not assume the certificate or app credential renews merely because the VM is running.

**Live hosting proof, 2026-11-26:** https://zava-media-3fsyvjqqjw5go.westus3.cloudapp.azure.com/healthz returned `service=media-bot`, `recording=false`, `maxConcurrentMeetings=1` with normal certificate validation, and remained healthy after rebooting the idle media VM without manually starting the task. `ZavaMediaBot` is running as SYSTEM with one at-startup trigger. Native package build assets and the required Graph service base URL are included; a managed-only compile had not exposed those startup defects. This is SDK/startup/TLS readiness, not a Teams handshake, join or capacity load test.

For this existing environment, `pwsh infra\hooks\deploy-service.ps1 -Service api` (then `-Service bridge`) builds the same Dockerfiles from `git archive HEAD`, avoiding untracked caches and conductor notes. Commit runtime inputs first. It preserves existing Container Apps settings/secrets and does not republish an unchanged hosted-agent version. Then run `pwsh infra\hooks\entra-post.ps1` to merge the independent native routes into existing Easy Auth exclusions, and `pwsh infra\hooks\verify.ps1`. The normal empty-environment `azd up` path is unchanged.

Keep `BRIDGE_MODE=Acs` until HTTPS readiness and meeting RSC are verified. Then persist `azd env set BRIDGE_MODE GraphMedia` and apply `az containerapp update -g rg-zava-scrum-zavaw3 -n ca-api-3fsyvjqqjw5go --set-env-vars "Bridge__Mode=GraphMedia"`; image-only deployment does not apply Bicep environment values. Restore `Acs` with the same two commands, leaving the VM intact. Direct/Hosted voice selection is independent; Direct remains deployed. The media routes are Easy Auth exclusions but have an independent ASP.NET authentication scheme requiring both `x-bridge-key` and `x-bridge-token`; the loader checks explicit team ownership and the stored link. A portal bearer token alone is insufficient. Native unauthenticated context returned 401 in 6b, not an Easy Auth redirect. Run `infra\hooks\verify.ps1` and native G6/G2/barge-in/G1 checks; do not apply old ACS/relay or fallback regression numbers to the native path. Preserve ACS if any gate fails. No native meeting-derived data is saved.

The inspected `team-payments` schedule is disabled/simulated with no stored meeting links, and Daniel/Maya have no roster Entra object ids (Admin does). The team's administrator/lead must approve a real sprint calendar through **Schedule > Proposed sprint calendar > Propose calendar > Approve**, or configure a real stored meeting link through the normal schedule flow. For this seed-backed roster, the deployment operator maps actual signed-in tenant accounts with `az containerapp update -g rg-zava-scrum-zavaw3 -n ca-api-3fsyvjqqjw5go --set-env-vars "Demo__MemberEmails__daniel=<real-Daniel-UPN>" "Demo__MemberEmails__maya=<real-Maya-UPN>"`; do not use the `m-` prefix in those mapping keys. Then the team administrator/lead invokes authenticated `POST /api/teams/team-payments/members/sync` and verifies non-null distinct `entraObjectId` values through `GET /api/teams/team-payments/members` before preparing a fresh session. These are actual-account choices, not invented object ids or scripted cast labels. Other seed-backed teams use their own configured seed file/mappings.

Prompt 7 adds no resources or roles. `media-host.ps1` checks both NativeMedia and
SkiaSharp payloads before uploading; the actual VM renders a real waiting frame
at startup. `/healthz` must report `boardRendererReady:true`, `boardWidth:1280`
and `boardHeight:720`; `verify-media.ps1` checks them without claiming an encoder
or participant sample. `teams-app.ps1` preserves the deployment's SSO resource
and adds only scheduled meeting event RSC. Empty-environment hooks/templates
include the same changes. No manual private keys, parameters or portal resources.

**Step 7 deployment proof:** API/native source `0d07e83`; API image
`m11-7-0d07e83` (ready revision `--0000080`), bridge `m11-7-c25287f`
(`--0000007`). Actual native DLL hash matches the local published Release DLL;
its product version includes `0d07e83` and the running SYSTEM task retains one
boot trigger. Mandatory `verify.ps1 -GateOutput docs\gates\m11-p7\after` passes
all eight checks, including the signed-in approver's Azure DevOps comment and
audit chain. One-run fallback p95 worsens 39.16% over 12 turns; native board/
event/encoder/G1 acceptance remains n=0. No resources or roles were added,
no default was switched, and all deployed services remain running.

## Agent identity governance (prompt 5B, ADR 0022)

`infra/hooks/agent-identity.ps1` (postdeploy) checks the hosted agent's Entra Agent ID: it is an agent identity, holds no secret or certificate, has a sponsor, and prints its recent sign-ins. Human gates, for a Conditional Access administrator (nothing in the deployment can create tenant policy):

| Gate | Who | Action |
| --- | --- | --- |
| Conditional Access for agents | Tenant policy owner in a separately authorised production governance activity | Policy creation/change is outside this conductor contract. Existing-policy review and a future report-only design do not authorise changing tenant policy here. |
| Sponsor attestation | The agent's sponsor (`admin@` by default) | Quarterly: Entra admin centre > Agent ID > the agent > Sponsors and Access reviews; confirm the agent is still needed and its roles (Cognitive Services User and Azure AI User on the Foundry account) are still right. Replace the sponsor when they leave. |
