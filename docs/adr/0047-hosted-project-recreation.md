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
