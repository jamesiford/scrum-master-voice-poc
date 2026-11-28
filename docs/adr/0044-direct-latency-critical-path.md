# ADR 0044: measure the Direct critical path before promotion

## Context

G1 is failed. The inherited after-quality point is3615/4453ms p50/p95,
n12, versus3418/4559ms,n10. Different windows cannot establish causality.
The westus3 M10 reference remains2994/4081ms,n36 (decision) and
2828/3876ms,n36 (rules), with unchanged5%/10% tolerances.

The current unchanged3ddd40b prefix contains two Model sessions:
3447/4312ms,n12, then3810/7968ms,n12, zero talkovers. The worst7968ms
turn records51ms analyser,185ms decision,5899ms orchestration and1820ms
first audio. This reproduces an orchestration tail without restoring
whole-document storage. Hop intervals overlap; their sum is not a
critical-path proof.

The next retained prefix has six complete sessions: Model3680/5328ms,
n24/questions3684/7968ms,n18; Rules3381/4491ms,n47/questions3502/
4491ms,n35. Rules windows1/2 are3319/4657ms,n23 and3381/3927ms,n24:
p50 spread1.87%, with a materially different tail. All talkovers remain0.
The third window and remaining Model sessions are still unrun, so this
prefix does not establish a final comparison or accepted optimisation.

Actual recent use-case spans show ReconcileUpdateQuery1505/1924ms,n25,
GetTurnInstructionQuery121/199ms,n25 and event writes110/130ms,n106.
These are an initial diagnostic window, not the final controlled cohort.
The reconciliation model is called even when authoritative decision
comments already determine all spoken events. Its consistency/risks
are not consumed by the gateway; corrections still need model retractions.

## Decision

Run three interleaved adjacent windows, alternating Model/Rules order,
two serial same-cast sessions per cell. This retains the same session
shape, reaches at least30 questions/configuration and measures window
spread without changing concurrent load. Retain failed cells, no retries
to improve a percentile, and restore the original configuration.
Finish this baseline before changing runtime code.

Evaluate avoiding the redundant reconciliation assessment on the voice
path only when comments are authoritative and no correction is pending.
Keep the diagnostic query and rules/model-failure paths intact. Any
candidate remains selectable until measured experience accepts it; no
floor silence, barge-in, approval, golden or threshold change is authorised.
No candidate runtime or promotion is claimed by this checkpoint.

## Consequences and platform evidence

Report total and question p50/p95/n, each hop, every window and talkovers
before/after. Simulated transcript0ms is not acoustic transcription proof.
The one-second absolute target and historical failures remain visible.
Added fixed resource costUSD0/hour; existing variable model/ACR charges
continue. No new service, identity, role or environment is required.

Current Learn confirms revision-scope changes produce immutable revisions,
and Voice Live reuses an integrated speech/model/synthesis connection:
https://learn.microsoft.com/azure/container-apps/revisions
https://learn.microsoft.com/azure/ai-services/speech-service/voice-live
Neither promises a one-second application result. A guessed dedicated
low-latency page returned404; no unsupported API setting was added.
