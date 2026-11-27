# M11 gates

## G8 recovery attempt 1 (26 November 2026 after20:00PT)

Only zavagf/eastus2 is used; zavaw3/mcm are not modified. Actual Global
Administrator readback, unchanged application/AllPrincipals consent
PATCH/readback n1 each, portal reconciliation and dedicated native
Calls.AccessMedia.All consent pass. Old group13resources.
Cheapest unrestricted compatible catalogue candidate: Standard_D4als_v7,
4available vCPUs,8GB,x64,Gen2-only,Windows consumption USD0.345/hour.
Family and regional quota used0/limit100; no quota request.
Gen2/purge parameters and native/permission/RBAC hook defects are addressed
in ADR0031; protected defaults remain unchanged for existing environments.
Focused hook regressions16/16 and all three builds pass, .NET0warnings/errors.
No full suites rerun. Both full empty up/verify cycles remain pending,
not passed at this checkpoint. Exact durations/failures will be appended.

Corrected preflight04:07:14Z to04:10:37Z: **202.8385s,exit0,n1**.
Preview27s, package<1s, official schema/Gen2 image/full C# format pass.
Initial old-group `down --purge` started04:13:08.8429494Z and is running;
20:22PT read found only deleting Cosmos/Container Apps environment.
Completed final up/verify cycles **0/2**. No new Azure resource yet.
Initial purge subsequently passed at04:40:11.4755627Z, **1622.6326s**
(27m02.63s), including the exact old resource group and Foundry purge.
The first up starts only after empty-group readback.
Public checkpoint is [g8-recovery-M11.json](g8-recovery-M11.json).
Known selected VM+E10disk+staticIPv4 estimateUSD0.363150685/hour,
before usage-based storage/vault/operations/egress; actual disk is still
unverified. Basic ACRUSD0.1666/day. Do not present these as an all-in bill.

First empty-up iteration started04:41:08.8381940Z from88d79a9 and created
target resources, but media vault creation failed BadRequest:
`The property "enablePurgeProtection" cannot be set to false. Enabling the
purge protection for a vault is an irreversible action.`
No matching deleted vault exists. ADR0031 records the correction: omit
that property for the explicitly disposable vault rather than emitting
false; defaults still emit true. Bicep and16focused checks pass.
This iteration is not a passed cycle; its partial footprint must be purged
before retrying a full empty up. Final duration is recorded when it exits.

Iteration exited04:49:21.3802308Z,**492.5420s,exit1,n1**;17target resources
were created. Actual D4als_v7 allocation and127GB StandardSSD_LRS/V2 disk
are Succeeded. Known VM+E10+IPv4 tariffUSD0.363150685/hour is now tied to
that readback, not an assumed disk. AZD's generic soft-deleted-resource
diagnostic was misleading; the actual API error and omission fix above win.

Failed-footprint purge passed04:51:57.1146390Z to05:09:02.1228983Z,
**1025.0083s (17m05.01s),exit0,n1**. Exact group absence readback false;
corrected empty preview passes29s. First empty-up iteration2 started
05:09:56.5199151Z from35812cd. Both completed verified cycles remain0/2.
The corrected vault now reaches Succeeded: actual readback has omitted
purge protection=null, softDelete=true, retention7days, EntraRBAC=true.
That resolves the observed BadRequest without modifying an existing vault.

At05:40Z both private-image apps are Succeeded with ready revisions
api--azd-1795757354/bridge--azd-1795757372 and image tag
azd-deploy-1795757159. Actual target child-scope ARM role assignments13,
including two registry-scoped AcrPull assignments; no unrelated role added.
The automatically issued media certificate is enabled and expires
2027-02-25T04:36:54Z. Native postdeploy bootstrap remains running; these are
provisioning observations, not yet a passed up or mandatory verification.

First corrected empty up **passed1828.7691s (30m28.77s),exit0,n1**,
05:09:56.5199151Z to05:40:25.2889925Z. Mandatory verify started05:40:51Z;
native trusted HTTPS/no-recording/cap1, real1280x720renderer and protected
401 checks pass. The standup/approved-write/audit portion is still running.
Completed verified cycles remain0/2 until that runner exits.

One authorised new-zavagf catalogue POST returned403 Forbidden:
`Missing scope permissions on the request. API requires one of
'AppCatalog.Submit, AppCatalog.ReadWrite.All, Directory.ReadWrite.All'.`
Requestf5606812-a404-4556-9256-78921efeb3a5,05:41:36Z. No authentication
retry or second equivalent upload attempt. Core Entra hooks succeeded.
This OAuth catalogue gate belongs to the new zavagf packages, not the
operator-completed zavaw3 uploads; exact administrator action is in DEPLOY.md.

First verification iteration exited1 after395.0662s: the standup completed
358s/12timed turns, but the conductor wrapper incorrectly placed
operation.json beside GateRunner result JSON. SummaryWriter deserialised
that metadata as a run and threw NullReferenceException atline27; approval
and audit checks were consequently skipped/failed. This is an invocation
layout defect, not a shipped runtime failure. The ignored wrapper now puts
GateOutput in a dedicated `runs` child directory; original evidence is
retained and the complete verify reran at05:47:54Z.
The preserved incomplete-iteration latency is3070/6599ms p50/p95,n12,
talkovers0,humansegments19; versus inherited Direct3262/5307ms,n12, the
p95 point estimate still fails the10% tolerance. G1 is not promoted.
Actual native task readback: SYSTEM,Running,AtStartup=true, executable
1.0.0+bef4d5b,last result267009 (running). No reboot or real Teams join claimed.

First complete verification **passes8/8**,05:47:54.0723372Z to
05:53:38.9021640Z,**344.8298s,exit0,n1**. Actual positive approved comment
verify-606d907a on item1 is authored by the signed-in approver; audit
zsa-6e033b0292c9 binds objectaeaec64b-d720-4996-9022-02fa0dcf043d to
revision55,session0dfba242-d1a3-4d73-ae8e-f54681252b90. No approval bypass.
Complete-run latency3251/3682ms p50/p95,n9,questionsn6,model-read8/9,
talkovers0,humansegments15; the different denominator and inherited failed
points remain explicit, not formal/native G1 acceptance.
First full footprint inventory25ARMresources; model/data-plane/Entra objects
are additional logical objects, priced by the existing consumption ledger,
not invented fixed charges. **Completed cycles1/2**; intervening authorised
purge has started before the second empty up.

Intervening purge **passes1639.2013s (27m19.20s),exit0,n1**,
05:54:02.8015505Z to06:21:22.0028777Z. Both the media vault and Foundry
account were purged. Exact group exists=false and matching deleted-vault/
Foundry lists[] prove the second start is empty. All three builds pass again,
.NET0warnings/errors32.03s;16focused checks and full C# format pass. No full
suites. The second full empty up/verify is next, not yet counted as complete.

Second iteration started06:23:09.5200057Z from8c8ade9, but actual submitted
native opt-in is false. Down erased MEDIA_HOST_ENABLED because main.bicep
also emitted that same configuration name as an output. Native resources
are omitted; **this reduced deployment does not count as cycle2**.
ADR0031 records the input-only correction and compiled cleanup-contract
regression17/17; all three builds pass, .NET0warnings/errors46.73s.
The invalid iteration must finish and be purged before restoring the
documented true input and restarting empty. The conductor now refuses
Up/Verify with missing/false native opt-in; no smaller-footprint false pass.

The invalid iteration ended06:40:26.2085805Z,**1036.6886s,exit1,n1**,
with14ARMresources. Image/app deployment succeeded, but hosted version
creation returned409: `The project workspace identity changed during
version creation. Read the current agent and retry.`
Request279ff8f7e9fdc6c39b993d57de16d99f. One current-agent GET200 retained
the first cycle's instance principal. Invalid-footprint purge started
06:42:11.3489783Z. ADR0032 records the exact-conflict recovery, capped at
two read-before-create retries; authentication and other errors are not
retried. Focused recovery tests6/6 and all three builds pass, solution
.NET0warnings/errors21.42s; isolated tool builds with its existing NU1603
preview dependency-resolution warning, without an SDK upgrade. Formatting
verification follows targeted line-ending repair; completed cycles remain1/2.

Invalid-footprint purge subsequently **passes1742.0244s (29m02.02s)**,
06:42:11.3489783Z to07:11:13.3733913Z,exit0,n1. Exact group=false and
matching deleted-vault/Foundry lists[] read back. All13 documented inputs,
including native=true, were restored/read after complete cleanup. Guarded
full second empty up started07:15:07.5243294Z from pushebfc085; mandatory
verification remains pending, completed cycles1/2. Formatting passes.
At07:22Z actual ARM deploymentzavagf-1795763991 submits native=true and
the real second media vault is Succeeded, protection omitted, softDelete/
EntraRBAC=true, retention7days. This is full provisioning, not native SKIP.

**Current G8 outcome: human-blocked, goal failed, 1/2 complete cycles.**
Full second empty up ended07:35:37.0795566Z after1229.5552272s,exit1,n1.
All three hosted creation calls returned the exact workspace409 despite
two successful current-agent reads; final request
f5e2882b465eff0ce347b4f56906cecc. No further equivalent retry.
Actual D4als_v7/127GB StandardSSD_LRS/V2/vault/private API/bridge images
provisioned, target roles11 including AcrPull2, footprint24resources.
Retained hosted version1/first image/instance principal differs from the
recreated project's SystemAssigned principal; root cause is unproven.
No speculative cleanup or SDK/identity change was made.

Independent media reconciliation failed7.9183946s,exit1,n1, before native
certificate/bootstrap. Exact403 Authorization_RequestDenied:
`Insufficient privileges to complete the operation.`
Date2026-11-27T07:40:44; request/client-request
07d95ae3-fd0a-4479-9442-9e7d14dd3abf. DEPLOY.md quotes the complete
error and administrator/support actions. No authentication retry or role
activation. Current native VM is unbootstrapped, unlike first-cycle
TLS/task/renderer readiness; known VM/disk/IPUSD0.363150685/hour remains
allocated plus variable charges for authorised continuation.
Independent public-pages and messaging-package hooks passed,exit0,n1
each,4.1405256s total (07:44:58.8972806Z to07:45:03.0378062Z).
Catalogue upload remains a named OAuth gate; no new auth attempt.

Final independent mandatory verify **passes8/8 in361.4580309s**,exit0,n1,
07:46:27.3643022Z to07:52:28.8223331Z, source7501b98. It explicitly
prints `MEDIA-TLS: native readiness NOT verified`; optional VM/DNS success
is not native bootstrapping, a live acceptance sample or cycle2 credit.
Actual session17190e28-c4cc-409c-bb6a-a87e23c175f1, item1revision58,
audit zsa-1e117c8f2430, approver objectaeaec64b-d720-4996-9022-02fa0dcf043d
and actual comment authoradmin@MngEnvMCAP763514.onmicrosoft.com verified.

| Hop | Final p50/p95 ms | n |
|---|---:|---:|
| Total speech end to first audio | 2821/4189 | 10 |
| Transcript | 0/95 | 10 |
| Analyser | 29/140 | 10 |
| Decision | 162/296 | 10 |
| Orchestration | 1447/1772 | 10 |
| Voice first audio | 1064/2248 | 10 |

Questions2836/4189ms,n7; modelread9/10; humansegments15; talkovers0,n1run.
First complete3251/3682ms,n9 to final2821/4189ms,n10 is p50-13.23%,
**p95+13.77%, fails10% tail tolerance**. Different time windows and
unchanged hot-path code are not causal/paired parity; G1 remains failed.
Inherited zavaw3 baseline3262/5307ms,n12 and earlier failed point estimates
are retained. Completed full greenfield cycles stay1/2.
Final required solution/portal/bridge builds all pass,59.5560s total,
07:54:32.6265882Z to07:55:32.1825998Z; .NET41.61s,0warnings/errors.
Existing portal606.42KB advisory and isolated preview NU1603 remain.
No full suites rerun. Final read-only exact-group inventory confirms24 ARM
resources; no resource, policy, role or protected-environment mutation.

| Gate | Current status | Denominator and decisive evidence |
|---|---|---|
| G8 full greenfield | Human-blocked; failed goal | 1/2 complete cycles; persistent409 plus actual privileged403 |
| G1 Direct latency | Failed point estimate; historical failures retained | First-to-final p95+13.77%,n9/10; native/paired samples n0 |
| Native G6/live G1/board | Not_run | New native meeting samples n0; allocated VM is not bootstrapped |

The operator completed zavaw3 Graph retrieval and WorkIQAgent.Ask delegated
consents and both catalogue uploads. Those administrator gates are closed;
real fixture/billing/Exchange/CI/user/organiserRSC gates remain.
G5/G6/G10/G11 still not_run,n0; old absence/refusal entries below are historical.

## Final validation (26 November 2026)

The final checkpoint runs the configured suites once, aggregated, without
changing goldens, thresholds, Domain rules, prompts or adapters. Gate failures
remain failures even when build checks pass. ADR 0030 records this boundary.

| Gate | Final status | Measurement and denominator |
| --- | --- | --- |
| G1 latency | Failed; formal/native acceptance not_run | Latest Direct 3262/5307 ms p50/p95, n12; versus preceding 3398/4707 ms n12, p50 -4.00%, p95 +12.75% (10% tolerance). Historical p50 +6.19% remains failed; native n0 |
| G2 identity/attribution | Native not_run; portal identity measured | Exact approver object id and ADO author n1; native speaker/overlap n0 |
| G3 audit/author | Measured portal subset | Final verify 8/8; actual positive approval/write/audit n1, revision53 on item1, tag zsa-35420fdf8067. New negative/Teams/Copilot samples n0 |
| G4 parity/rehearsal | Failed exact board parity; configured suites pass after focused retry | .NET947/947, bridge unit13/13, standup6/6 and retrospective6/6 on unchanged focused retry. Initial real E2E11/12 retained; exact board identity still differs11/11; real Teams/Copilot rehearsal n0 |
| G5 Copilot | Not_run | Consenting/permission-negative/streaming live turns n0; local tests are not channel proof |
| G6 native join | Not_run | Trusted HTTPS, running VM, no-recording/cap1, actual1280x720renderer and unauthenticated401 measured; stored-link joins0/10 |
| G7 trace/evaluation | Failed release gate | Real synthetic n72: intent/items98.6%, kinds94.4%, dates75%n8; one timeout, five fallbacks, missing binary/intent probabilities9/1. Trace/span metadata12/12; live quality/modelCI n0 |
| G8 final greenfield | Failed prerequisite; unchanged | D4s_v5 NotAvailableForSubscription in eastus2, quota0/100 used; privileged deployer authority absent. Both final up/verify cycles n0; no G8 operation here |
| G9 multi-team | Not_run in final checkpoint | New concurrency/second-org/human matrix samples n0; historical three sessions/36turns/three chains retained |
| G10 knowledge | Not_run, unbuilt | Retrieval/citation/permission/latency n0; exact consent/corpus/two-user gates in DEPLOY.md |
| G11 Work IQ | Not_run, unbuilt | Owner-OBO/source/privacy/latency n0; exact consent/billing/source gates in DEPLOY.md |

### Aggregate suites and checks

One `dotnet test ZavaScrum.slnx -c Release --no-build --no-restore` invocation
passes **947/947**, failed0/skipped0. All seven retained TRX files were
independently summed. No full .NET suite was rerun.

| Suite | Final passed / total |
| --- | ---: |
| Domain | 424 / 424 |
| Application | 225 / 225 |
| Infrastructure | 93 / 93 |
| Api | 168 / 168 |
| Architecture | 14 / 14 |
| AgentHost | 3 / 3 |
| Windows MediaBot | 20 / 20 |
| Bridge unit | 13 / 13 |
| Bridge standup E2E | 6 / 6, 371.16s |
| Bridge retrospective E2E, initial | 5 / 6, 351.44s |
| Bridge retrospective E2E, focused retry | 6 / 6, 267.29s |
| Portal | 0 configured tests; no runner or test script |

The initial real E2E aggregate has11passes/1failure/0cancelled/0skipped in
723.75s. Its retrospective records step-away and circle-back, but Voice Live
says "Thank you, noted. I'll hand the floor back to the facilitator" instead
of the requested question. The unchanged cast consequently never delivers
Daniel's correction. The actual team-scoped circle-back prompt reads back as
`Default` with the correct question; no deterministic interpretation defect
is established. One focused unchanged retrospective retry passes6/6 in
267.29s (runner268.17s), failed0/cancelled0/skipped0. No code, prompt,
cast or assertion change was made; the initial deviation remains a reliability
risk, not a fixed deterministic defect. No second retry or passing-suite rerun.
All **972 configured unique checks** now have passing evidence:947.NET plus
25bridge. The retry executes six existing cases, not six new tests.

All three builds, both TypeScript typechecks/ESLint/Prettier and full C#
format verification pass. Release solution build27.67s, zero warnings/errors;
portal retains its existing606.42KB chunk advisory. Retained raw test,
verification and model output belongs to the ignored run log, not the repo.
No runtime deployment, resource configuration, role, quota or policy change;
no resource added (fixed USD0/hour). Local E2E uses isolated Json data and
attached API/bridge helpers, not deployed data or approval authority.
Both helpers are stopped, ports5089/5099 released and the exact temporary
Json directory/launcher removed. Public content-free counters/provenance are
in [final-validation-M11.json](final-validation-M11.json).

### Final deployed harness

Explicit `azd exec -e zavaw3` runs the existing `verify.ps1 -Runs 1` and retains
its GateRunner output. All eight mandatory checks pass, including actual
approver-authored `verify-49f548e2`, revision53 on item1, and the complete
`zsa-35420fdf8067` audit chain. Session
`aab034b6-ab06-47b9-939f-58c2e45b4e62`, trace
`35420fdf80672ff45b53aab3d18c3c4d`; all12turns have trace/span metadata.
One scripted Direct run takes380s,19transcript pieces, zero talk-overs;
questions p50/p953330/5307ms,n9; decision model10/12turns.

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total | 3262 | 5307 | 12 |
| Speech end to transcript | 0 | 205 | 12 |
| Analyser | 26 | 27 | 12 |
| Decision model | 141 | 166 | 12 |
| Orchestration | 1507 | 2062 | 12 |
| Voice Live first audio | 1350 | 3720 | 12 |

Versus the preceding step9after3398/4707ms,n12, latestp50-4.00% but
p95+12.75% fails the unchanged10% tail tolerance. Versus step9before
3200/5600ms,n10, latest+1.94%/-5.23%; neither comparison erases any
historical failure or establishes causality. One scripted run is not formal
G1's model matrix, native latency, human attribution or rehearsal.
The printed Teams checklist is unchecked historical ACS guidance, not proof.

The final real G7 gate exits1:72synthetic cases, dates75% eligible n8,
one B01 timeout2520ms, five rules fallbacks, escaped ids0, rejected
candidates0/35 and three outside-id challenges. Binary threshold0.7:
precision94.4%n71,639probabilities, missing9, Brier0.0302/ECE0.0756.
Intent threshold0.6:precision98.5%n67,71probabilities, missing1,
Brier0.0405/ECE0.1079. Golden before/after SHA256 is unchanged:
`e1c580be02de6629818a22f597b2b1c721eae3efc87b397ed52b055812c8ae37`,
the historical semantic-LF hash. No retry discards this failure.

## Final documentation checkpoint (conductor 10, 26 November 2026)

This is the final **measured status**, not an all-green release claim.
Historical stage results below remain evidence, not current native/Copilot
proof. The final validation above supersedes only the pending test checkpoint.

| Gate | Final status | Measurement and denominator |
| --- | --- | --- |
| G1 latency | Failed; formal/native acceptance open | Latest Direct p50 3200 -> 3398 ms (+6.19%, tolerance5%), n10/12; p95 5600 -> 4707 ms. Earlier 4A held relay point estimates, not confidence bounds; no native samples |
| G2 identity/attribution | Identity checks measured; native not_run | Verified portal actor/revision n1; native speaker/human overlap n0. Synthetic tagged bytes are not attribution |
| G3 audit/author | Measured portal subset | Actual session/proposal/approved/applied/revision52 chain n1; separate unapproved403/refusal n1. Live Teams/Copilot approval n0 |
| G4 parity/rehearsal | Failed exact board parity; final rehearsal open | Native/Chromium exact identity differs11/11; prior full checkpoint849.NET+24bridge pass. Final Teams/Copilot rehearsal n0 |
| G5 Copilot | Not_run | Five skills/package1.1.0 implemented and local checks62/62; live consenting/negative/streaming turns n0 |
| G6 native join | Not_run | Trusted TLS/SYSTEM reboot readiness measured; actual stored-link unattended joins0/10 |
| G7 trace/evaluation | Failed release gate | API/bridge workbook211hop samples/433team-tagged records, prohibited keys0; model dates75%n8, overall goldensn72. Hosted5 completed paired samples0, modelCI/livequality n0 |
| G8 final greenfield | Failed prerequisite; proof not_run | One preflight231.30s/exit1, native-capacity failure; existing zavagf group13resources. Both final up/verify cycles n0; no teardown |
| G9 multi-team | Historical partial measurement; full matrix open | Three concurrent scripted sessions completed,36turns,3audit chains; ten-team/second-org/human matrix unrun |
| G10 knowledge | Not_run, unbuilt | Retrieval/citation/permission/latency n0; consent/corpus/two-user fixture outstanding |
| G11 Work IQ | Not_run, unbuilt | Owner-OBO/source/privacy/latency n0; consent/billing/source fixtures outstanding |

### Latency chronology

Nearest-rank total p50/p95; scripted cast is not real speech transcription.
Different regions/capacities/windows are not causal comparisons. No failed
sample is removed by a later better run.

| Baseline/stage | Total p50/p95 ms | Samples | Verdict/boundary |
| --- | ---: | --- | --- |
| M10 local decision/rules | 3827/4907;3116/3905 | 35turns each,3runs | Local baseline only |
| M10 deployed eastus2 decision/rules | 2994/4081;2828/3876 | 36turns each,3runs | Deployed baseline |
| 3 identity | 3071/4755 | 32turns,3runs | Measured, not native |
| 3A concurrency | 3400/4919 | 36turns,3concurrent sessions | Three sessions, not ten |
| 3B realtime/silence | 2.1rules3301;mini3342 p50 | 18standups/model quality review | Mini rejected; preserve2.1/800ms. About25% window drift |
| 5A delegated writes | 3806/4626 | 32turns,3runs | Tolerances not established; author proof separate |
| 4 initial hosted/model | 5679p95 vs4720direct | 35-36turns/cell | Failed+20%p95 |
| 4A larger hosted/model | 3239/4504 vs3197/4269direct | 201-211turns/cell,6rounds | Point estimates+1.3%/+5.5%; confidence bounds do not establish tolerances |
| 6B fallback before/after | 3419/4435 ->3311/5115 | 11turns each | Failed+15.3%p95 |
| 7 fallback before/after | 3230/3958 ->3338/5508 | 12turns each | Failed+39.16%p95 |
| 8 initial/final fallback | 3301/7343;3304/4503 vs3272/4379before | 12turns/run | Initial+67.69%p95 failed; final+0.98%/+2.83% point estimates held |
| 8A/8B | Not_run | n0 | No runtime/integration change |
| 9 fallback before/after | 3200/5600 ->3398/4707 | 10/12turns | Failed+6.19%p50; improvedp95 does not erase it |
| 10 docs/G8 | Not_run | n0 new turns/cycles | No runtime deployment or repeated latency proxy |

### Final G8 attempt

Target is only `zavagf`/`eastus2`, subscription
`4b0dd31f-5379-4acc-8dcd-60f2929d2513`; the existing resource group
`rg-zava-scrum-zavagf` contains13resources. One current full preflight ran
03:04:32.2709665Z to03:08:23.5667972Z on27November (231.2965s):

```text
FAIL quota: Windows VM Standard_D4s_v5
Not enough  vCPU quota in eastus2
1 check(s) failed. Nothing was provisioned.
```

Model quota: chat1300/2000free need100; realtime10/10free need10
(own10addedback); decision4300/4500free need100. Azure resource/RBAC and
Azure DevOps administrator checks pass. Graph's delegated-scope preflight
passes, but the actual current direct directory roles are only Agent Registry
Administrator, Agent ID Developer, Agent ID Administrator and Global Reader.
The time-limited Global Administrator authorisation ended14:27PT; there is
no current application/privileged-consent administrator proof.
No Entra mutation, role activation or auth retry was made.

One explicit `list-skus --all` diagnostic confirms Standard_D4s_v5
`NotAvailableForSubscription` in eastus2 (location plus zones1/2/3).
Actual quota is used0/limit100 for both standardDSv5Family and total cores:
the failed preflight's empty-family/quota wording is incorrect. The failure
is SKU availability, not a reason to request quota. `g8-M11.json` freezes
these public metadata results and zero completed deployment cycles.

| Required cycle | Outcome | Timing/sample |
| --- | --- | --- |
| Initial teardown of existing old group | Not_run; cannot safely rebuild with failed prerequisites | n0 |
| First final-solution empty `azd up` + verify | Not_run | n0; preflight failure above, not a deployment |
| `azd down --purge` between verified runs | Not_run | n0 |
| Second final-solution empty `azd up` + verify | Not_run | n0 |

Exact recovery actions/owners are DEPLOY.md G8-DEPLOYER-AUTHORITY and
G8-MEDIA-CAPACITY; no quota/subscription/policy change is authorised.
Keep the old group and zavaw3 running; earlier smaller-runtime cycles below
do not prove this footprint. This step adds no resource, USD0/hour fixed cost.
The existing verify subset already prints PASS/FAIL and runs actual
GateRunner/portal-author/audit checks; formal G1-G11 reporting and film UI
remain out of this documentation-only subset.

## Content-free observability and evaluation (conductor 9, 2026-11-26)

G7 is **release-blocking and failed**, not a reason to discard working adapters.
The frozen72 authored synthetic goldens are not employee utterances or live
knowledge/Work IQ source evidence. Final measured report and both line-ending
hashes are in `docs/gates/decision-M11`.

| Check | Measured result | Samples |
| --- | --- | --- |
| Real Microsoft-Decision plus unchanged grounding | Intent/items98.6%,kinds95.8%; no model faults,4rulesfallbacks | n72 |
| Spoken dates | Failed75% against >=95% release policy; first spoken phrase is not necessarily the revised target | n8 eligible, not72 |
| Rules fallback | Intent95.8%,items97.2%,kinds55.6%,dates0%; no probabilities or comment capability invented | n72; datesn8 |
| Calibration0.7 | Accepted precision94.5%,Brier0.0298,ECE0.0750,missing0 | 648binary probabilities;73accepted |
| Calibration0.6 | Accepted intent precision98.5%,Brier0.0415,ECE0.1109,missing0 | 72intent probabilities;68accepted |
| Item guard | Zero escaped ids;0/36 rejected candidates. Adapter bounds candidates itself, so zero is not a universal adversarial guarantee | 3explicit outside-id challenge mentions |
| Foundry live quality/safety/groundedness | Not run: required message inputs conflict with content capture OFF; explicit rubric disabled, not an invalid scheduled pass | n0 |
| Knowledge and Work IQ quality | Not run, sources unbuilt; no invented citations/signals | n0 |
| Local privacy/trace/approval/architecture | Pass; actual normal exporter suppresses immutable content events; verified parent metadata never replaces explicit team ownership | privacy/native16,approval9,decision14,relay3,architecture14,bridgeHTTPtrace1 |
| Build/format/typecheck/lint | All three builds, full C# format and both TS typechecks/linters pass | targeted only; no full-suite rerun |
| Workbook/live deployment acceptance | Saved template matches; all4 panels queried live;16hop rows,211session-hop samples across API/bridge/model/approval/write; approval precedes the1successful write | one Direct scripted session, not native/hosted acceptance |
| Before Direct Simulated Room latency | p50/p95 total3200/5600ms,356s,zero talk-overs | n10 turns |
| After Direct Simulated Room latency | p50/p95 total3398/4707ms,397s,zero talk-overs; p50+6.19%,p95-15.95% | n12 turns, one run |
| Real refusal/approval/audit | Mandatory verify8/8; approver-authored revision52; unapproved apply403followup.not_approved, then fixture rejected; writes remain1 | positive1,negative1 |
| Live metadata privacy | 18dimension keys on433team-tagged requests/dependencies; prohibited key occurrences0; Insights logs/exceptions0 in probe window | bounded metadata-only read, not a universal proof |
| Hosted candidate5 | Two creates exceeded readiness deadline; own sessions became active, paired protocol probe returned424 and produced no completed report; both stopped, stable4 at100% retained | validated paired/hosted trace samples0 |
| CI real-model configuration | Rules reporting wired; actual repository variables empty, model job not enabled; default-branch schedule requires merge | live CI model samples0; named identity-owner prerequisite |

Release policy/commands are in `demo/evaluation/README.md`; daily CI reports
non-blockingly, `--release-check` returns1 for unmet or absent model evidence.
Initial n72 with one timeout and kinds94.4% remains in the run log. The corrected
calibration inventory omits Progress because the real adapter never asks it;
goldens, thresholds and Domain rules were not tuned to improve results.
Frozen live metadata and all before/after denominators are in
`observability-M11.json`. API/bridge8859a7a revisions84/9 are latest=ready;
nativee8c95b6 DLL hash/SYSTEM boot task/content-off settings and real renderer
are verified. Non-telemetry environment hashes, identities, scale and secret
names match the original snapshot. Hosted5 was not promoted.
One established Voice Live connection span is not a Teams join sample; join
reliability and human barge-in remain n0. Turn/approval/rejection/refusal counters
are12/1/1/1. The workbook's approval check is a necessary session-level
condition, not a substitute for the exact follow-up/caller/revision audit.
The fallback p50 point estimate fails the historical5% tolerance and remains
above the1s target; n12/one run cannot pass formal G1's matrix. The improved
p95 does not erase earlier failures or prove a causal improvement.

| Gate | Step9 handoff |
| --- | --- |
| G1 | Formal/native not_run; fallback measured, p50 tolerance failed |
| G2 | Native speaker/human barge-in not_run; scripted talk-overs0 is not attribution proof |
| G3 | Measured positive1/negative1; exact real caller/revision chain passes |
| G4 | Inherited exact-board identity failure retained; no new encoder/participant sample |
| G5 | Copilot human/permission-negative acceptance not_run |
| G6 | Native Teams joins/roster/organiser RSC not_run |
| G7 | Failed dates75%n8; actual API/bridge workbook checked; hosted5/live quality/real-model CI gaps explicit |
| G9 | No new multi-team human samples; local ownership guards retained |
| G10 | Knowledge unbuilt/not_run |
| G11 | Work IQ unbuilt/not_run |

Native G1/G2/G6, CopilotG5, multi-teamG9 and G10/G11 have no new human samples.
Prior exact-board/fallback failures remain. Preserve Acs/Share/Direct and native
non-recording; there is no native decision/approval/write path to trace.
Added fixed cost USD0/hour; existing variable model/ingestion/build charges
and two temporary hosted-session allocations, both stopped. No tariff invented.

## Work IQ contract and blocked spike (conductor 8B, 2026-11-26)

ADR 0027 adds independent consent/evidence contracts and an **unregistered**
Application reader, not a live signal adapter or four product integrations.
Actual Graph metadata proves the Work IQ service principal exists and the
portal has **no Work IQ grant**. Current Learn requires delegated
`WorkIQAgent.Ask`, approved Copilot Credits usage/spending assignment and
discovery of the live schema. No consenting owner, audience-safe channel/
meeting/negative-user fixture or billing assignment is established.
8A Graph retrieval consent is a different resource, not a Work IQ prerequisite.

| Gate/check | Step 8B result | Sample and evidence |
| --- | --- | --- |
| G11 live owner consent/revoke and expiry | Not run | n=0 caller-OBO Work IQ calls; metadata is not consent proof |
| G11 disabled-team / other-team / private-source leakage | Not run | n=0 live negative queries; no zero-leakage claim |
| G11 channel evidence / chase wording / helper / review goldens | Not run | n=0 real source-labelled product cases; contract fixtures do not close M9c |
| G11 Work IQ latency | Not run | n=0 Work IQ retrieval samples |
| G1 before/after voice latency | Not run | n=0 new turns; no voice path, runtime registration or deployment change |
| Consent and scope contract checks | Pass | Domain 17; Application 36 WorkSignal checks (32 real-mediator port-fake, four policy), plus 24 Copilot/approved-write regressions; architecture 14 |
| Existing runtime health | Measured readiness only | n=1 healthz HTTP 200, body Healthy; unchanged deployment |

Local checks exercise the actual Domain rules and unregistered reader through
the real mediator with fakes only at the ports and a test-only presentation
query. They cover disabled-team zero provider/scope calls for all operations,
named owner/tenant/membership, revocation including during a provider call,
board/meeting scope, channel/group/roster/provenance, age/size/HTTPS rejection,
distinct at-most-three helpers and a typed unavailable state distinct from
valid empty results. The four positive synthetic fixtures return citations,
not live card integration or review proposal conversion.

DEPLOY.md names WORK-SIGNALS-CONSENT / WORK-SIGNALS-BILLING /
WORK-SIGNALS-FIXTURE. No scope is granted, policy/billing changed or source
read performed. No private mail/chat or broad `ask` call was made.
Do not treat post-filtering private results as proof of no private reads.
No new runtime, resource, store or cache, so no new deployment or
`verify.ps1` approval write. Acs/Share/Direct, native non-recording and prior
failed gates remain unchanged. G2 to G7, G9 and G10 have no new live samples.
Added fixed cost USD 0/hour, billable Work IQ queries n=0.

All **91 focused checks** pass, with no failures/skips. .NET Release build
has zero warnings/errors; portal and bridge builds and linters pass.
Full C# format verification passes with no changes.
No full suites or E2E rerun; the next full checkpoint remains after prompt 10.
Portal's existing >500kB bundle advisory is unchanged.

## Foundry IQ prerequisites (conductor 8A, 2026-11-26)

**Blocked before the required permission spike**, not a measured gate
failure or completed integration (ADR 0026). Live metadata reads succeeded:
the administrator can read the SharePoint root and has a Copilot licence,
but the portal application's actual Graph consent is only
`User.Read openid profile`, missing both delegated retrieval scopes.
No approved source/denied-user fixture is established. No Search service
exists in the authorised resource group; none was provisioned to stand in
for the blocked experiment.

| Gate/check | Step 8A result | Sample and evidence |
| --- | --- | --- |
| G10 correct citation rate | Not run | n=0 real questions; no approved source-labelled corpus |
| G10 uncited answers | Not run | n=0 answers; zero is not a measured rate |
| G10 permission leakage | Not run | n=0 caller-OBO and denied-user queries; metadata access is not trimming proof |
| G10 retrieval latency | Not run | n=0 retrieval samples; no MCP knowledge base exercised |
| G1 voice latency | Not run | n=0 new turns; no runtime changes, previous measurements/failures retained |
| Build regression | Pass | .NET Release 0 warnings/0 errors; portal and bridge builds exit 0 |
| Existing runtime health | Measured readiness only | n=1 HTTP 200; API git-d041890, latest=ready --0000082, not a new deployment |

G2 to G7, G9 and G11 have no new acceptance samples. Inherited failures
and native/Copilot human gates remain unchanged. No full suites or
before/after voice runs were repeated for this documentation-only blocked
checkpoint; it adds no voice or other runtime. The portal retains its
pre-existing Vite bundle-size advisory. Added fixed cost USD 0/hour.
DEPLOY.md names exact consent, corpus and real-user actions. Resume the spike
before implementing consumers; do not substitute Blob-only or admin retrieval.

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

Closed on the owner's decision: a second destroy-and-rebuild cycle was not run (the rebuild above is the proof that an empty environment comes up and verifies), and the westus3 deployed baseline (`baseline-M10-deployed-westus3/`) was not saved. The 3B work will measure a westus3 baseline itself. The westus3 p50 (3439 to 3470 ms) against 2994 ms in eastus2 is region and capacity, not a regression. That teardown is historical; zavaw3 was subsequently rebuilt and is running.
It is protected from this final G8 proof, which targets only zavagf/eastus2.

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
