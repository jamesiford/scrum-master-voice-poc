# ADR 0035: Delete hosted data-plane agents before project teardown

Date: 27 November 2026. Status: accepted correction; two-cycle proof pending.

**Scope superseded by ADR0038:** the operator deleted the former eastus2
resources/apps and moved G8 to fresh zavag8/westus3. Former-target/support
actions and endpoints below are historical evidence only. The lifecycle
safety rule remains; its exact allowed target is now zavag8/westus3, never
zavaw3/mcm or the former target. No support case is a prerequisite for
investigating the fresh region.

## Context

The previous G8 proof completed only one of two empty deployment cycles.
After resource-group deletion and Cognitive Services account purge, the
same project endpoint still returned the first cycle's hosted agent,
version1/image and instance principalff6d1ab3. Recreated project principal
3bce4b0f differed. Three creation attempts returned409:
`The project workspace identity changed during version creation. Read the
current agent and retry.` Two successful reads did not repair it.

Current inspection again returns that exact retained agent while the deleted
account list is empty and the ARM project is Succeeded. This excludes an
unpurged Cognitive Services account as the immediate cause. Deleting the
retained data-plane agent and recreating it on the SAME ARM project resolves
the409: version1 creates in30222ms and becomes active in43485ms.
Project principal3bce4b0f and internalId2c4c5e3d62da4ae2a6509c130c084e98
are unchanged, while the new instance principal is
c1aa55e6-e064-45ff-a47d-f90bb3dba46b. This isolates retained agent/workspace
binding as the observed cause, not a concurrent ARM create or unpurged
account. The provider's internal retention mechanism is not inferred.

## Decision

Use the documented
[agent deletion API](https://learn.microsoft.com/azure/foundry/agents/how-to/manage-hosted-agent#delete-an-agent)
in an
[azd predown hook](https://learn.microsoft.com/azure/developer/azure-developer-cli/azd-extensibility).
Delete only the two code-owned relay names before destroying their project
identity, and read back404 before allowing teardown. Missing agents/groups
are idempotent. Account/tenant, exact authorised zavagf/eastus2 group,
project ownership tag, endpoint and hosted image registry are checked first.
Protected zavaw3/mcm and unrelated images are refused.

Do not toggle identities, randomise names, delete unrelated Entra objects,
upgrade the SDK or retry authentication. Non-success responses retain exact
status/body; visibility polling is bounded. Existing account/vault purge,
domain rules, adapters and native permissions stay unchanged.

## Validation and consequences

Eleven focused lifecycle regressions and17 existing greenfield checks pass.
All three builds pass, .NET zero warnings/errors. Actual controlled cleanup
deletes the retained relay and reads back absence; canary was already absent.
Same-project recreation and actual identity/image/active readbacks pass.
Two full empty cycles and final VM deallocation are pending and will
determine the measured G8 verdict.

The historical media reconciliation403 is not current: unchanged actual
calling-app PATCH and Calls.AccessMedia.All readback pass. Initial native
bootstrap instead returns OperationNotAllowed because external governance
deallocated the VM at08:46Z. One scope-owned start/retry succeeds, including
actual certificate registration and focusednative5/5;
no governance policy is changed. Catalogue OAuth/real organiser gates are
separate and do not establish full native acceptance.

BaselineDirect total3021/4168ms,n12,talkovers0, actual approverrevision64.
The full baseline7/8 retained an SSL check before bootstrap completion;
focusednative5/5 afterwards recovers it without erasing the failure.
Two empty cycles, their durations/verification and final deallocation are
recorded in [second-attempt evidence](../gates/g8-proof-2-M11.json).

Initial full purge passes1641.7849s. Exact resource group and matching
soft-deleted Foundry/vault absence read back19:13:50Z; first new full up
starts19:13:52Z with native enabled, approved D4als_v7/Gen2 and unchanged
Direct/Acs/Share. This is0/2 completed new cycles, not a passing G8.

### Full empty proof rejects a complete repair claim

New full up fails1205.4765s at19:33:57Z after successful version1 creation
30402ms. The immediate version read returns404 `Project not found`.
Two bounded version/agent follow-ups and the distinct supported list route
return the same error. ARM remains Succeeded with new SystemAssigned
principal35f5f03b-0e70-446e-a30d-929756941fd3/internalId34539814c5034cb8aab9ebf782176a64.
The endpoint is unchanged/documented and existing deployer subscription
Owner/Foundry User access is verified without a grant.

Deleting retained agents fixes the observed same-project binding but does
not prove full account/project data-plane lifecycle reconciliation. Provider
internals remain unknown. [Current Learn](https://learn.microsoft.com/azure/foundry/agents/how-to/deploy-hosted-agent#troubleshooting) documents the version-read poll and
directs support for unavailable project identity/provisioning; it does not
document a blind project404 retry, naming rotation or identity toggle.

The real failure exposed a safety defect: generic404 was treated as agent
absence even when the response said the whole project was missing. The hook
now refuses that exact error before deletion and during absence readback.
Two new regressions pass,13focused checks total. No success-shaped fallback,
authentication retry or speculative SDK/identity workaround is shipped.

G8 remains failed/newcycles0/2. Another purge cannot safely credit absence
of the created agent while its project namespace is unavailable. The Azure
subscription owner must obtain Microsoft Foundry support reconciliation and
prove real agent/version/list reads before both new full cycles resume.
DEPLOY.md gives the exact resource, identity, version, times and owner action.
Independent native/Direct recovery and final VM-only deallocation completed;
they do not credit the failed full deployment.

Final independent native5/5 and mandatoryverify8/8 pass376.6268s,
Direct2862/4397ms,n12, questions3247/4397ms,n9, modelread10/12,
humansegments19/talkovers0 and actual approverrevision66/auditzsa-67e38e91111a.
Baseline3021/4168ms,n12 yields-5.26%/+5.49% point deltas, not a paired
sample; inherited G1 failures and original baseline7/8 remain.
Only the media VM is deallocated19:51:52Z;25 resources remain,
API/bridgeHTTP200 and native HTTPS intentionally unavailable.

A final safe lifecycle restart through the published guard at19:53:32Z
fails11.7053s before any resource deletion on the same project404. This
live check proves the new refusal, not merely its fake-HTTP regression.
No additional project read/authentication loop or unsafe guard bypass
follows. Provider repair is the only remaining prerequisite for2/2 cycles.
Known media computeUSD0.345/hour is removed; disk/IPUSD0.018150685/hour
and foundations/variable usage remain. Existing F0nativebot USD0/hour
is the only additional ARM recovery registration; no new code resource type.

Independent legal-page/package hooks also pass. One real catalogue POST
20:00:28Z refuses403Forbidden for missing AppCatalog.Submit/
AppCatalog.ReadWrite.All/Directory.ReadWrite.All OAuth scopes, request
47576c18-613e-4c3b-bb4a-8124f00f84d1. This is not the resolved calling-app
permission gate; no CLI scope widening or authentication retry follows.
DEPLOY documents the already-listed Teams administrator upload for only
the newzavagf package/id654a0c74; existingzavaw3/mcm entries remain untouched.
