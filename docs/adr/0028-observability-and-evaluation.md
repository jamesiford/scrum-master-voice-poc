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
