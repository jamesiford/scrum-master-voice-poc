# ADR 0039: Do not require standalone model quota for managed Voice Live

Date: 27 November 2026. Status: accepted correction; fresh live proof pending.

## Context

The first fresh westus3 full up fails at preflight in206.1921s, before any
resource is created. Its default DataZoneStandard realtime deployment is
not offered. Actual westus3 catalogue lists only GlobalStandard for
gpt-realtime-2.1, while that deployment quota is already10/10 used by
existing environments. No protected resource or quota may be changed.

This exposed an incorrect platform assumption: the application's Voice
Live URL specifies a managed model ID, without a BYOM profile. It does
not invoke the separate Azure OpenAI Realtime deployment. Current
[Voice Live documentation](https://learn.microsoft.com/azure/ai-services/speech-service/voice-live)
explicitly states that supported models are fully managed and require no
model deployment, capacity planning or provisioned throughput.
gpt-realtime-2.1 remains the same supported Pro model.
[BYOM](https://learn.microsoft.com/azure/ai-services/speech-service/how-to-bring-your-own-model)
is a distinct explicit profile, not the current application URL.

## Decision

Add the declarative `PROVISION_REALTIME_MODELS` deployment input.
Its default true preserves all existing deployment inputs and optional
standalone Azure OpenAI realtime capability. Set false only for this fresh
proof, using the already-implemented real managed Voice Live service.
The same gpt-realtime-2.1 model ID, endpoint construction, Direct/Hosted
adapters, prompts and floor rules remain unchanged.

With false, the template does not create unused standalone realtime
deployments, and preflight explicitly reports the managed service rather
than checking unrelated Azure OpenAI deployment quota. Chat and decision
deployments and their quota checks remain mandatory. Never disable the
native host, substitute a model, share another environment's endpoint or
alter another environment's quota.

## Validation and consequences

The failed full up and exact empty-group readback are retained, n1; no
successful cycle is credited. Existing zavaw3 is read-only: it uses
GlobalStandard capacity5. Subscription realtime deployment usage10/10 is
measured, not inferred. This is not a quota request or workaround.
Focused tests exercise the real preflight loop: false skips only optional
realtime quota; true and unset preserve the original requirement.
The compiled-template input/output regression protects this new input
through intervening down. All three builds and exact format checks pass.

Both full empty up/verify cycles must prove actual managed Voice Live
audio, approved writes and the entire footprint before G8 passes.
No model failure is hidden or replaced by synthetic audio; a genuine
service failure remains a failed verification. New deployments retaining
the true default still need available standalone deployment quota.
