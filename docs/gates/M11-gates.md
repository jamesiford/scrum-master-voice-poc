# M11 gates

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


## Traceability gate

`--check-audit` fails by design until the audit endpoint exists (prompt 3). Status: **failing, not yet testable**.

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
