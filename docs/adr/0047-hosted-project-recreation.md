# ADR 0047: Diagnose hosted project recreation before claiming G8

Date: 2026-11-28
Status: Investigation in progress; no lifecycle repair claimed

## Scope and established evidence

The fourth G8 attempt targets fresh `zavag9` in westus3. The teardown
and native deployment guards move from the retired `zavag8` proof to that
exact environment, resource group and subscription. Protected zavaw3 and
mcm remain untouched; Direct/Acs/Share and Simulated Room remain defaults.
No Application use case, endpoint, Domain rule or approval invariant changes.

The old zavag8 Agent Service lookup still returns HTTP404 at13:47:21UTC,
more than thirteen hours after its second recreation. ARM Resource Health
is Available, but that does not establish data-plane readiness. Model
deployment listing returns200 while hosted management returns404; new
preview-header and default-project-alias probes do not repair the path.
Both `Project not found` and `Subdomain does not map to a resource`
responses are retained rather than treating every404 as absent agents.

Minimal experiments use only owned `rg-zava-g8b-repro-e31974b3` in westus3,
S0 Foundry accounts and projects, with existing code-defined relay image
metadata. No model deployment, hosted invocation, VM or fixed-throughput
resource is added. Agent creation is not proof of invocation or G8.
Initial agent version creates succeed in17.25/17.56seconds; direct parent
deletion refuses409 `CannotDeleteResource` until projects are deleted.
The harness is corrected to the supported child-before-parent order.
A fresh project's initial404 recovers without a write, so an immediate
post-create404 alone is not the repeated-lifecycle defect.

## Current documented contract

[Recover or purge](https://learn.microsoft.com/azure/ai-services/recover-purge-resources)
documents48-hour account soft deletion and purge for same-name reuse.
[Create projects](https://learn.microsoft.com/azure/foundry/how-to/create-projects)
requires awaiting each project deletion before account deletion.
[Capability hosts](https://learn.microsoft.com/azure/foundry/agents/concepts/capability-hosts)
are optional for Microsoft-managed storage, not a mandatory hosted fix.
[Disaster recovery](https://learn.microsoft.com/azure/foundry/how-to/agent-service-disaster-recovery)
explicitly excludes Basic mode; its Standard-mode recovery durations and
capability reset are not automatically applicable to this footprint.
Machine Learning soft-delete rules do not prove an invisible workspace
tombstone: the supported owned-group inventory is empty.

## Decision and outstanding proof

Further isolated reads distinguish a missing resource from a stale route,
but do not yet establish the provider's internal cause. Recreated account A
has new account/project internal IDs; a changed project name under A and
the original project name under new account B return200, while the exact
recreated account/project tuple returns404. API-version changes do not fix
it. The account-advertised `cognitiveservices.azure.com` frontend initially
returns200 for A and the old zavag8 project when `services.ai.azure.com`
returns404. Actual hosted creation on A succeeds20.35seconds through that
frontend, but its version fails image authentication without the project
identity's AcrPull role: metadata creation is not an active deployment.

An independently primed account C disproves changing frontends as a
lifecycle fix. Its actual SDK version1 becomes Active in32943ms,
first minimal create-to-readback60.791seconds, with only project AcrPull
on the owned existing registry. Supported child/account deletion and purge
take84.251seconds. Immediate same-name recreation then returns404 through
both frontends. The experimental endpoint setting and template changes
are removed before publication; no alternate hostname, fallback or name
rotation is shipped. This is not two successful minimal or full cycles.

The ten-minute post-purge quiet variant also initially returns404.
The deleted-name experiment encounters actual DNS NXDOMAIN, then a
subsequent DNS read resolves again without account recreation. A single
DNS failure is not a verified global name-release predicate. Stable release
and the current registered/documented ARM API2026-11-01 are separate
bounded experiments, not accepted repairs. The original two-minute
project polls and ten-minute privilege reads continue to their bounds.

Two stable NXDOMAIN samples for both old R domains, two minutes apart,
plus active/deleted account ARM404, still produce404 after exact-name
recreation. DNS release is therefore not the repair. Current ARM lifecycle
E produces first Active31820ms/create57.991seconds/purge80.409seconds,
then52/52 second reads404. An account-only documented Agents capability host
on C provisions Succeeded in1.50/0.58seconds but leaves project404.
No project BYO host or added storage is used or shipped. The deployer
already inherits Foundry User and Owner. ARM advertises no backing
`amlWorkspace` link, so there is no discovered customer-owned workspace
to purge. Fixed non-default F is first Active32839ms/create68.753seconds;
parent DELETE504 is resolved by absence readback before purge/recreation,
not a blind retry. Its second target fails52/52 reads. Independently active
G waits for exact Project-not-found after child deletion before parent
deletion: create65.179seconds/purge90.892seconds, then52/52 second failures.
Previously unprimed F default-project Agents lookup returns200, while its
used target404; Connections/deployments on recreated C return200 while
Agents404. Request revalidation/hosted preview headers do not repair C.
This narrows the service/lifetime failure without proving a hidden cache
or workspace mechanism. Current scoped AppLens does not support Foundry.
The documented older purge API fails52/52 second reads. Project-only I
preserves parent internalId and reaches two actual Active cycles in
51.173/101.322seconds, child teardown12.840seconds. This is not full empty
credit. Resetting failed G's child under its current parent still fails.
The used Agents namespace's parent-incarnation boundary is now measured;
the internal implementation cause is not. Documented container GET/stop
on first Active T return404, so no completed-stop lifecycle result exists.
Original A/Q polling encounters401 near token expiry; standard renewal
restores authorised404. UTC-expiry checks resume their original deadlines
without an authentication loop, role change or invented missing samples.

The [sanitised machine evidence](../gates/g8-proof-4-evidence.json)
retains each request, outcome, duration and observed identity change.
Current unauthenticated Azure Retail Prices API westus3 meters are
USD0.0994/vCPU-hour and0.0118/GiB-hour, or0.1230 for the test's
1CPU/2GiB per aggregate active-session-hour. Actual billing is not
established; no hosted invocation or new model deployment is run.

The only additional published correction is fail-closed teardown:
an exact requested-agent `not_found` response can prove absence;
project, workspace, subdomain and unrelated-agent404s cannot. Eighteen
focused checks pass. All three current source builds pass, solution
zero warnings/errors. The isolated hosted SDK retains its existing
NU1603 dependency-resolution warning; Bicep retains its existing
observability dependsOn warning. Neither is hidden as a lifecycle success.

Do not ship speculative waits, name rotation, capability hosts, broad roles
or a success-shaped teardown bypass. Compare immediate same-name recreation,
an automated ten-minute quiet period and confirmed data-plane project
deletion, plus project/account-name and API-version controls. Poll any
persistent same-name lookup every two minutes for up to three hours.
The minimal results must precede a repair and the two full empty cycles.
The final gate table and DEPLOY instructions will record actual samples,
including failed variants and all automated wait durations.

Fresh portal registration succeeds, but actual delegated consent refuses
HTTP403 `Authorization_RequestDenied` at13:48:13UTC, request
`bedbc1ba-13c5-4c61-ad2a-b2f54914761e`: `Insufficient privileges to complete
the operation.` Privileged reads are polled every ten minutes for three
hours; no token loop, policy change or unrelated role grant is performed.
This is separate from the hosted-project defect. All three source builds
pass, .NET zero warnings/errors; no full suites are run.
