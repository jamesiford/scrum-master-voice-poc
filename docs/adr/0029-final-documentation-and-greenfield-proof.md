# ADR 0029: Final documentation and greenfield proof boundary

Date: 26 November 2026. Status: accepted documentation/operating decision;
final G8 acceptance remains open.

## Context and measurement

Conductor step 10 is explicitly limited to documentation, human-gate
runbooks and the old zavagf/eastus2 G8 rebuild. It does not own film UI,
runtime rework or the final full-suite checkpoint. The working zavaw3
environment and mcm must not be torn down or modified.

The old authorised group exists with13resources. One full final-footprint
preflight (native Standard_D4s_v5 requested in process only) ran
2026-11-27T03:04:32.2709665Z to03:08:23.5667972Z,231.2965s,exit1:

```text
FAIL quota: Windows VM Standard_D4s_v5
Not enough  vCPU quota in eastus2
1 check(s) failed. Nothing was provisioned.
```

Model quota and resource/RBAC/ADO checks pass. Graph's delegated-scope
check also passes, but read-only live direct-role assignments show only
Agent Registry Administrator, Agent ID Developer, Agent ID Administrator
and Global Reader, not application/privileged-consent authority.
The authorised Global Administrator window expired14:27PT.
No role activation or consent mutation is attempted.

One explicit SKU diagnostic with `--all` returns Standard_D4s_v5
`NotAvailableForSubscription` for eastus2/location and zones1/2/3.
DSv5 and regional quota each show used0/limit100. The quota error is
misdiagnosed because the ordinary catalogue query hides the restricted SKU,
leaving the preflight family empty. Do not request quota; an owner-approved
available minimum-four-vCPU SKU is the correct follow-on configuration.

Final up/verify cycles, actual native joins and real Teams/Copilot rehearsal
all have n=0. Prior smaller-runtime G8 is historical, not this proof.
Current G7 dates75%n8 and fallback p50+6.19%n10/12 remain failures.
Hosted candidate5 returned424 with completed paired samples0; stable4
remains. Knowledge/Work IQ integrations are unbuilt (G10/G11 not_run,n0).

## Decision

Preserve the working Acs/Share/Direct and Simulated adapters. Do not destroy
the old nonempty environment when current prerequisites prevent rebuilding;
do not silently omit native hosting, choose another region/subscription,
request quota or alter tenant policy to make G8 appear green.
Document exact role/capacity recovery in DEPLOY.md and both unrun cycles
in the gate table. Keep failed gates visible without blocking independent
documentation/build/push work. No new platform API or shipped adapter is added.

Reconcile the current docs with code: team-bound card routing, approver-OBO
writes, three default model deployments, optional stateless hosted relay,
Windows native non-recording limits and package1.1.0 with open live G5.
The audit API and workbook are not the requested dedicated film UI.
SeedAdo resets tagged work items/relative dates, not all roster/cast/meeting
state. Keep these product follow-ons explicit rather than shipping demo shims.

## Defects and consequences

The preflight prints a quota failure with an empty VM family and checks
Graph OAuth scopes/readability rather than current privileged role authority.
Neither is sufficient final-footprint validation; list both in the draft PR
instead of silently modifying hook behaviour in this docs-only subset.
G8 remains unproven with exact measurements, not an invented platform refusal.
No resources were added (fixed cost USD0/hour), secrets exported, defaults
changed or full suites rerun. All three builds are required before push.

Current Microsoft Learn confirms explicit environment targeting and teardown
semantics: [azd reference](https://learn.microsoft.com/azure/developer/azure-developer-cli/reference)
and [environment targeting](https://learn.microsoft.com/azure/developer/azure-developer-cli/work-with-environments#run-commands-in-specific-environments).
Use `-e zavagf` and process-scoped context for hooks. `down --purge` is
destructive, not validation; it must finish before reusing soft-deleted names.
These lifecycle docs do not promise native SKU or tenant consent availability.
The code/templates are the source of component coverage; live gates are the
source of delivery claims. No Presenter Pack changes are made.
