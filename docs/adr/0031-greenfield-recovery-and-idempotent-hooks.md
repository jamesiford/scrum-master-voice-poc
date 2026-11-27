# ADR 0031: Greenfield recovery preserves protected defaults

Date:26November2026. Status:accepted implementation; G8 cycles pending.

## Evidence

Only zavagf/eastus2 is authorised for the two-cycle destructive proof.
Global Administrator is renewed: actual directory-role read and unchanged
application/AllPrincipals consent PATCH/readback pass, n1 each.
Portal reconciliation and dedicated Calls.AccessMedia.All consent pass.
The old group has13resources. No zavaw3 or mcm mutation is permitted.

The full eastus2 SKU catalogue excludes every restricted size. Combined
with Windows consumption prices, x64, at least four available vCPUs and
actual family quota, the cheapest compatible candidate is Standard_D4als_v7:
4vCPUs,8GB,Gen2-only,USD0.345/hour including Windows, family/regional
quota0used/100limit. B4als_v2 and D4s_v5 are restricted
NotAvailableForSubscription despite unused quota. Catalogue availability
is not allocation success. Neither a quota request nor a region change
is the solution.

## Decision

Fix the real greenfield defects before teardown. Preflight reads the exact
SKU including restrictions, available cores, x64/image generation, family
and regional quota and counts this environment's existing matching VM only.
An empty group is valid. Native hook guards admit the exact authorised old
environment alongside the existing westus3 environment, but still reject
mcm, wrong region/group/subscription.

Preserve the existing Gen1 image default and expose a Gen2 image parameter.
Select2022-datacenter-g2 for this Gen2-only SKU. Preserve vault purge
protection=true by default; selectfalse **before creating only this disposable
proof vault**. Soft delete, RBAC and protected secret delivery remain.
Protection cannot be disabled on an existing vault, and protected vaults
cannot be purged before retention expires. No existing protection or tenant
policy changes. Registry-scoped AcrPull visibility is checked inside the
postprovision hook before private-image deployment, retaining azd up.

Merge existing requiredResourceAccess declarations with baseline Graph/ADO
requirements. Deduplicate by resource/id/type, never remove independently
approved optional Graph/WorkIQ scopes and never grant them automatically.
The operator completed zavaw3 Files.Read.All/Sites.Read.All/WorkIQAgent.Ask
consent and both tenant catalogue uploads at20:00PT; these administrator
gates are closed. Real users/corpus/billing/Exchange/CI/meetingRSC are not.

## Validation and consequences

Sixteen focused pure hook checks and all three builds pass; .NET0warnings/errors.
Full suites are not rerun. No Domain/approval/model/default changes.
Both up/verify cycles, timing, iteration failures and actual added resource
costs will be recorded in M11-gates.md; no greenfield success is claimed yet.

Current Learn:
[SKU restrictions](https://learn.microsoft.com/azure/azure-resource-manager/troubleshooting/error-sku-not-available),
[application update](https://learn.microsoft.com/graph/api/application-update?view=graph-rest-1.0),
[media requirements](https://learn.microsoft.com/microsoftteams/platform/bots/calls-and-meetings/requirements-considerations-application-hosted-media-bots),
[generation2](https://learn.microsoft.com/azure/virtual-machines/generation-2),
[vault recovery/protection](https://learn.microsoft.com/azure/key-vault/general/key-vault-recovery).
Live catalogue and platform behaviour take precedence over the unavailable
default. Compute tariff excludes disk/publicIP/storage/Insights/build/model
usage; do not present it as an all-in environment bill.

## Recovery checkpoint, approximately20:32PT

Corrected preflight passes202.8385s,04:07:14Z to04:10:37Z; preview27s
and package<1s pass. Official deployment schema, Gen2 image readback,
Bicep compilation and full C# format pass. Initial old-group purge started
04:13:08.8429494Z and is still deleting the exact group; both up/verify
cycles remain n0. A20:22PT read found only the deleting Cosmos account and
Container Apps environment. No timeout/failure or resource creation is
claimed. [Recovery evidence](../gates/g8-recovery-M11.json) is checkpointed.

Initial purge completed04:40:11.4755627Z,exit0,1622.6326s (27m02.63s).
The exact old group and soft-deleted Foundry account were removed/purged.
First up/verify remains pending; this successful teardown is not G8 closure.
