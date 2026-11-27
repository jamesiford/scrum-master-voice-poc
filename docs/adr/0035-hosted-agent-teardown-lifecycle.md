# ADR 0035: Delete hosted data-plane agents before project teardown

Date: 27 November 2026. Status: accepted correction; two-cycle proof pending.

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
