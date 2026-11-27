# ADR 0028: Observability and evaluation without employee content

Date: 2026-11-26
Status: Accepted; G7 release and live quality gaps remain explicit

## Context and current Learn

M11 9 requires traceability and continuous quality with content capture OFF.
Read current [Azure Monitor .NET](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable),
[MAF tracing](https://learn.microsoft.com/azure/foundry/observability/how-to/trace-agent-setup),
[scheduled evaluation](https://learn.microsoft.com/azure/foundry/observability/how-to/continuous-scheduled-evaluation),
[deployed interaction evaluation](https://learn.microsoft.com/azure/foundry/observability/how-to/cloud-evaluation-deployed-interactions)
and [workbook schema](https://learn.microsoft.com/azure/templates/microsoft.insights/2023-06-01/workbooks)
before coding. Foundry external agents target `otel_agent_id`; the continuous
responses trigger does not cover arbitrary external traces. Quality requires
input/output messages; absent messages mean absent scores, not a quality pass.
The protected native path must remain non-recording, with no saved-decision/
approval/write port. Knowledge and Work IQ sources are still unbuilt.

## Decision

Use the persisted W3C session id for custom hops, link a differing HTTP parent,
and carry verified owning-team metadata. Shared BCL source keeps media separate
from writable Application dependencies. Sample one actual native frame per
second; keep PCM unchanged and do not claim vendor-internal spans. Propagate
authenticated hello metadata through browser HTTP and the optional hosted relay.
Use the existing exporters, fixed session sampling and only allowed metadata.
Refuse content capture; disable MAF/GenAI message content and Insights log-body
export. Immutable SDK events cannot be removed, so their spans are suppressed
with a team privacy counter. Raw browser console/error and model exception
bodies are omitted; actual call errors remain visible in the UI.
Console/container operational logs remain a separate diagnostic surface.

Live Foundry creation rejected `APPLICATIONINSIGHTS_CONNECTION_STRING` with
HTTP400/`invalid_payload`: that variable is platform-reserved. Current
[hosted export guidance](https://learn.microsoft.com/azure/foundry/agents/how-to/configure-hosted-agent-telemetry)
requires project monitoring for automatic injection. This project has no
monitoring connection. Do not enable broader platform capture to evade the
content policy. Instead configure our actual .NET Azure Monitor exporter via
its documented `AzureMonitorOptions.ConnectionString`, supplied as
`AzureMonitor__ConnectionString`; never mutate/override the reserved variable.
The shared bootstrap supports the ordinary platform setting when no explicit
SDK destination is supplied. No additional collector, exporter, role or
project connection. Tests resolve the real SDK options with and without an
injected value. Windows `az.cmd` removed inline JSON quotes for workbook tags;
use a typed parameter file and delete only that unique temporary file.

The workbook filters one team and queries real hop spans, join event outcomes
and approved decisions preceding writes in the same session operation id.
Accepted requests are not established calls or unique join-success rates.
Gate spans retain measured intervals; pre-computed analysis can overlap the
phase decomposition, so the gate's durations are not a vendor execution graph.
Approval and id-grounding invariants/Domain rules are unchanged.

Author72 synthetic goldens and freeze their hash. Call the real
Microsoft-Decision-1.1 adapter and same grounding, recording raw pre-threshold
probabilities, missing data, fallbacks, exact readings and rejected-id
denominators. Rules emit neither probabilities nor comment kinds. Daily CI
reports non-blockingly; G7 release explicitly blocks on the policy in
`demo/evaluation/README.md`. Do not edit expectations to fit model answers.

Keep live employee-trace quality evaluation disabled because its inputs violate
the contract. Check in explicit tone/cited-item/refusal/safety/groundedness rubric
and n0 status, not an invalid schedule or invented score. The daily synthetic
decision report is not a substitute for live quality evaluation.

## Measured evidence and consequences

Final real model n72: intent/items98.6%,kinds95.8%,dates75% eligible n8,
zero failures,4rulesfallbacks,0/36 rejected adapter candidates,3 outside-id
challenges,0escapedids. Threshold0.7 accepted precision94.5%n73,
Brier0.0298/ECE0.0750;0.6precision98.5%n68,Brier0.0415/ECE0.1109.
All648binary/72intent probabilities present. Rules n72:95.8%/97.2%/55.6%;
dates0%n8. Release fails dates because code takes the first spoken date phrase;
no Domain rule was changed. Initial n72 with one timeout is retained separately.
Progress has no real adapter question and is excluded from calibration.
The report's measured input-byte hash uses CRLF; source was normalised to LF
without changing parsed JSON or expectations. `decision-M11/provenance.json`
records both verified hashes rather than rewriting measured evidence.

Before Direct regression n10: totalp50/p95 3200/5600ms,356s,0talk-overs.
Deployment/live workbook and after measurements are recorded in G7 and the
run log, not inferred from local exporters. Native acceptance/vendor traces,
knowledge/Work IQ and live quality remain n0 where unrun.
All three builds, both TSlinters, full C# format and focused trace/privacy/
approval/decision/architecture checks pass. No full suites before prompt10.

Greenfield templates and scoped update hooks contain the same workbook/export
settings. Native runtime-only deployment verifies existing consent/certificate
and makes no Entra write; hosted updates preserve traffic/defaults.
No resource teardown, unrelated role, tenant policy or SKU change. Fixed added
cost USD0/hour; existing variable ingestion/build charges apply.

The runtime-only preflight found an exact registered certificate but Graph
returned `customKeyIdentifier` as hex. Current
[keyCredential](https://learn.microsoft.com/graph/api/resources/keycredential?view=graph-rest-1.0)
defines a Binary thumbprint identifier; accept its exact hex or base64 form
in both preflight and restart checks, never another credential. This fixes the
deployment guard without renewing a certificate or making an Entra write.
The hosted benchmark now requires both actual audio and terminal ResponseDone,
not an audio event followed by a timeout. The first candidate5 creation timed
out on readiness then became active; that caller's session was stopped and
the single retry is recorded separately.

## Live handoff

The final saved workbook matches its committed template and all four panels
were queried2026-11-27T02:46:27Z. Trace
`e48b795beab7972d69f7145550a925f0` has211hop samples across actual
API/bridge roles, including37forwarded-audio,20decision-model,36SDK-model
and one approved write. All8mandatory checks pass; actual caller audit/
revision52 and separate unapproved403/refusal are verified. The433team-tagged
requests/dependencies have18dimension keys, zero prohibited key occurrences
and no Insights log/exception rows in this bounded window. Do not extrapolate
to unobserved content, vendor internals, Teams joins or native attribution.
One Voice Live connection is not a successful Teams join.

API/bridge8859a7a revisions84/9 are latest=ready; nativee8c95b6's actual DLL
hash/SYSTEM boot task and protected content-off settings are checked.
Non-telemetry environment hashes, scale, identities and secret names match.
Hosted candidate5 exceeded readiness in both own fresh creates; sessions became
active, but the paired protocol probe returned424 and no completed report.
Both were stopped. Readback leaves stable4 at100%, and Direct unchanged.
No healthy-hosted/version5 trace or paired latency is claimed. Collect startup
diagnostics before stopping the next bounded synthetic probe; do not promote
an active version without successful protocol/audio/terminal-response evidence.

Nearest-rank fallback comparison (one run per column, all phase denominators
10before and12after):

| Hop | Before p50/p95 ms | After p50/p95 ms |
| --- | ---: | ---: |
| Total | 3200/5600 | 3398/4707 |
| Speech end to transcript | 0/169 | 0/209 |
| Analyser | 26/28 | 27/39 |
| Decision model | 153/218 | 159/289 |
| Orchestration | 1587/2669 | 1548/2041 |
| Voice first audio | 1441/3729 | 1601/2835 |

Wall time356/397s; talk-overs0/0. Total p50+6.19% fails the historical5%
tolerance; p95-15.95% is an observed point estimate, not a causal improvement
or formal G1 matrix. The1s target and inherited exact-board/fallback failures
remain. `observability-M11.json` freezes metadata and denominators.

Repository variable names were empty: rules reporting is wired, but real-model
CI is not enabled and the daily schedule starts only after default-branch
merge. DEPLOY.md names the least-privileged OIDC identity-owner prerequisite,
not a secret/runtime-identity shortcut. Live employee quality remains disabled,
and knowledge/Work IQ remain unbuilt. Fixed added cost USD0/hour; two temporary
hosted compute allocations were stopped, with existing variable rates not guessed.
