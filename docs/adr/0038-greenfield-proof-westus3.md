# ADR 0038: Run the greenfield proof in a fresh westus3 environment

Date: 27 November 2026. Status: accepted; first cycle verified, second lifecycle failed.

## Context

The operator deleted the former eastus2 proof resources and its dedicated
applications. The previous409 and hosted-project404 remain historical
failures, not a requirement for a support case before further investigation.
Westus3 hosts the working deployment. A new azd environment satisfies the
empty-environment criterion without touching that deployment.

## Decision

Use only fresh `zavag8` in `westus3`, exact resource group
`rg-zava-scrum-zavag8`, in the existing approved subscription. Move the
destructive hosted-agent lifecycle guard to that exact scope; reject the
former target, `zavaw3`, `mcm`, wrong regions, groups and subscriptions.
Native setup still permits the existing zavaw3 deployment, but this proof
does not mutate it. Preserve Acs/Share/Direct and the Simulated Room.

Run two full empty `azd up` and mandatory verification cycles, with an
intervening guarded `azd down --purge` and exact absence readback.
Keep the final resources and deallocate only the new media VM.
Do not create quota, policy, subscription or unrelated role changes.
Any unlisted manual step is a deployment defect, not a proof shortcut.

If westus3 reproduces the project404, compare the working project read-only,
measure propagation with bounded backoff and investigate endpoint, API
version and ordering before changing templates or hooks. Do not bypass
ambiguous project404 in the teardown guard.

## Validation and consequences

Harmless privileged directory read succeeds, n1. The fresh portal
application, baseline delegated consents and three administrator app roles
are created by the existing hook. Dedicated media Calls.AccessMedia.All
consent succeeds; organiser RSC remains a real human gate.

Current Learn confirms the project endpoint format and `v1` hosted-agent
REST API, with version readiness polling before invocation:
[deployment](https://learn.microsoft.com/azure/foundry/agents/how-to/deploy-hosted-agent).
The existing ACR explicitly uses legacy registry permissions, so its
registry-scoped AcrPull remains correct rather than changing to the
ABAC-only repository role. The postprovision hook verifies role visibility
before the full-up image deployment phase.

Cycle durations, failures, sample sizes, exact final state and costs
are recorded in the gate table, DEPLOY and content-free proof evidence.
First full retryup1665.7626s/mandatoryverify8/8/407.4025s/native5/5 pass.
Real managed Direct3417/3871ms,n10, approverrevision71/auditcomplete and
24 resources are measured. Owned hosted version1 reads HTTP200/active;
this first fresh cycle does not reproduce the former project404.
Intervening guarded purge passed1700.1703s, with exact absence and all13
inputs preserved. Second full empty up fails1192.4071s at hosted creation
with project404, despite new ARM identity/internal id and Succeeded state.
Working-project same-credential/APIv1 GET is200; repeated lifecycle, not
region, is implicated. Seven bounded backoff reads through00:19:45Z
remain404; connections read200, both agent API versions return404.
Current ARM API retains the same Succeeded identity/id; working and fresh
capability hosts are empty. Current Learn makes these optional in the
default managed setup, so adding BYO storage/Search or unrelated roles
would not be an evidence-backed correction.
One supported hosted-hook retry fails404 in386.4755s. The second and final
supported no-state provision/deploy fails917.7072s: actual RP upsert589.6757s
and real builds succeed, with unchanged project identity/id, but hosted
creation still returns404. Ordinary existing-footprint up skipped unchanged
ARM state and was cancelled before hosted POST; `provision --no-state` is
the documented diagnostic replay, not a shipped workaround or empty cycle.
Account Resource Health is Available; project health returns unsupported,
and AppLens has no Foundry support. Neither proves Agent Service health.
Independent native5/5 and mandatoryverify8/8/418.8114s pass,
Direct3338/3977ms,n12, approverrevision73/completeaudit.
No source-only cause or safe correction is established; no speculative
delay, identity toggle or teardown bypass is shipped. G8 remains failed1/2.
The fresh catalogue
POST403 is a separate OAuth-scope human gate, not a support prerequisite.
No runtime feature, Domain invariant, default adapter or public API changes.

Final independent native refresh260.3368s/RequireReady5/5 and
mandatoryverify8/8/416.2082s pass: real Direct3520/4592ms,n12,
questions3742/4592ms,n9, modelread10/12, zero talkovers and actual
approverrevision75/auditzsa-dc8089a89f16. These do not repair the failed
full-up lifecycle or credit cycle two. Final tail4592ms versus first3871ms
is a failed point, not an accepted trade-off or promotion proof.
Only the exact fresh VM is deallocated68.8504s including readback,
confirmed01:05:46Z. All25 resource identities and unchanged API/bridge
images remain; both health200/latest=ready reread01:07:34Z.
Existing Defender Standard/P2 automatically added MDE.Windows; no
policy/plan was changed. Actual base retail0.02/hour is additional while
allocated and stops for a deallocated Azure VM. Disk/IP0.018150685/hour
remains, not an all-in bill. DEPLOY binds every actual resource to its
fixed/variable cost boundary. G8 closes failed1/2; an evidence-backed
lookup investigation remains, not a blind third SDK retry or support prerequisite.

Current supporting references:
[agent identity](https://learn.microsoft.com/azure/foundry/agents/concepts/agent-identity),
[hosted permissions](https://learn.microsoft.com/azure/foundry/agents/concepts/hosted-agent-permissions),
[basic environment](https://learn.microsoft.com/azure/foundry/agents/environment-setup).
