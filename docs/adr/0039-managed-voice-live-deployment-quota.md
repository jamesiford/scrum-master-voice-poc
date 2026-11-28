# ADR 0039: Do not require standalone model quota for managed Voice Live

Date: 27 November 2026. Status: accepted correction; first real live cycle verified.

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
The new parameter remains input-only; before/after-down readbacks prove
all13 inputs preserved, including false and native true, without manual
restoration. All three builds and exact format checks pass.

The corrected full up1665.7626s and mandatoryverify8/8/407.4025s pass,
including real managed Voice Live10timedturns, total3417/3871ms and
firstaudio1235/2269ms, approverrevision71/completeaudit and native5/5.
All24 resources/requiredmodels and owned hosted version1active are real.
No standalone realtime deployment is created or protected quota changed.
The guarded intervening purge passed1700.1703s with exact absence.
Second full empty up fails1192.4071s at hosted creation with project404,
after real models/images provision. This is a distinct lifecycle failure,
not a managed Voice Live quota regression; G8 is failed at1/2.
Independent verification after that failed up also passes8/8/418.8114s:
real Direct3338/3977ms,n12, approved revision73/completeaudit and native5/5.
It verifies the real managed-model correction, not another full-up cycle.
Both supported lifecycle retries also fail404 after the successful model
correction; no quota or model change is justified by that distinct failure.
Final independent verification passes8/8/416.2082s, actual managed Direct
3520/4592ms,n12, nativeRequireReady5/5 and approved revision75/completeaudit.
Final25 retained resources include the existing Defender plan's automatic
MDE.Windows extension, not a new model dependency. Only the fresh media VM
is deallocated after readiness; API/bridge remain healthy. No second
full-up credit follows and G8 remains failed1/2.

Both full empty up/verify cycles must prove actual managed Voice Live
audio, approved writes and the entire footprint before G8 passes.
No model failure is hidden or replaced by synthetic audio; a genuine
service failure remains a failed verification. New deployments retaining
the true default still need available standalone deployment quota.
