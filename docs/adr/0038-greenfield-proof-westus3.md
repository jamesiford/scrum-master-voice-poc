# ADR 0038: Run the greenfield proof in a fresh westus3 environment

Date: 27 November 2026. Status: accepted; measurements pending.

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

Final cycle durations, failures, sample sizes, exact final state and costs
will be recorded in the gate table and content-free proof evidence.
No runtime feature, Domain invariant, default adapter or public API changes.
