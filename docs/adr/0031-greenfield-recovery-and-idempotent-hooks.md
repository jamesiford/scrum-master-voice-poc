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

## First empty-up iteration

The first empty up started04:41:08.8381940Z from88d79a9. The media vault
creation returned BadRequest: `The property "enablePurgeProtection" cannot
be set to false. Enabling the purge protection for a vault is an
irreversible action.` No matching deleted vault exists, so this is a new
template defect, not an attempt to alter existing protection.
[Current Learn](https://learn.microsoft.com/dotnet/api/microsoft.azure.powershell.cmdlets.keyvault.models.vaultcreateorupdateparameters.enablepurgeprotection?view=az-ps-latest)
explicitly says the create/update property does not accept false.
The disposable parameter now omits the property; the default still emits
true. Soft delete, retention and RBAC are unchanged. Bicep compilation and
16/16 focused hook checks pass. This failed iteration is not a completed
cycle; purge its partial footprint and restart from an empty group.

The failed iteration exited04:49:21.3802308Z,492.5420s,exit1. AZD's generic
soft-deleted-resource suggestion misclassified the actual BadRequest;
deleted-vault readback was empty. Seventeen resources were created. The
selected D4als_v7 VM and127GB StandardSSD_LRS/V2 disk both reached Succeeded,
so actual allocation is now proven, not merely catalogue availability.
Known VM+E10disk+IPv4 tariff isUSD0.363150685/hour before variable charges.

The failed footprint purge passed1025.0083s,04:51:57.1146390Z to
05:09:02.1228983Z,exit0. Exact group absence=false and corrected empty
preview29s pass. First empty-up iteration2 started05:09:56.5199151Z
from35812cd. This restart preserves the genuinely empty-cycle criterion.
Live corrected-vault creation succeeds: purgeProtection=null (omitted),
softDelete=true, retention7days, EntraRBAC=true. The original API defect is
resolved; full runtime/verification remains pending.

05:40Z checkpoint: both private-image Container Apps have ready revisions,
target child-scope ARM roles13 (including AcrPull2) read back, and the
automatically issued media certificate is enabled through25February2027.
Native bootstrap and complete verification still run; no manual resource
or certificate creation is substituted for the hooks.

First corrected empty up passed1828.7691s,exit0,ending05:40:25.2889925Z.
All core Entra hooks completed. One optional new-zavagf catalogue POST
returned403 Forbidden for missing AppCatalog.Submit/AppCatalog.ReadWrite.All/
Directory.ReadWrite.All OAuth scopes, requestf5606812-a404-4556-9256-78921efeb3a5.
An active administrator role does not add those scopes to an Azure CLI
token. No authentication loop; document the exact Teams administrator upload
action and continue both core proof cycles. Existing zavaw3 uploads remain done.

First mandatory verification failed395.0662s because this conductor's
ignored wrapper put operation.json in the GateRunner result directory.
The actual standup completed358s/12turns, but summary deserialisation of
non-run metadata caused NullReferenceException and blocked subsequent
approval/audit checks. Correct the wrapper to use a dedicated `runs` child,
retain the failed attempt, and rerun complete verification; no shipped
GateRunner/runtime/golden/threshold changes. Preserved latency3070/6599ms,
n12,talkovers0, does not close inherited G1. Actual native task is
SYSTEM/Running/AtStartup, executable1.0.0+bef4d5b; reboot acceptance is not claimed.

Complete first verification passes8/8 in344.8298s,ending05:53:38.9021640Z.
Actual approved write/auditn1: approverobjectaeaec64b-d720-4996-9022-02fa0dcf043d,
item1revision55,tagzsa-6e033b0292c9. Complete standup3251/3682ms,n9,
talkovers0, without changing inherited/native failed or unrun gates.
Full ARM inventory25. Completed empty up/verify cycles1/2; intervening purge
starts now. Second empty up/verify remains necessary for G8 closure.

Intervening purge passes1639.2013s,ending06:21:22.0028777Z. The actual
disposable media vault and Foundry account purge succeeds; group=false and
both deleted-name lists[] confirm a genuinely empty second start. Thus
the omission fix supports the required repeat purge in practice, while
existing protected environments remain untouched. Three final builds,
16focused checks and full C# format pass again; no full suites.
