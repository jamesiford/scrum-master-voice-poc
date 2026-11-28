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

The nine-session checkpoint retains Model windows1/2 at3680/5328ms,n24
and3481/4082ms,n22, a5.72% p50 spread on the same build. The first third-
window Model session is3588/4598ms,n12. Model prefix3623/5302ms,n58
(questions3693/4598ms,n44), Rules prefix unchanged3381/4491ms,n47
(questions3502/4491ms,n35); all talkovers0. Finish the planned remaining
three sessions rather than selecting the favourable window as a verdict.

Actual recent use-case spans show ReconcileUpdateQuery1505/1924ms,n25,
GetTurnInstructionQuery121/199ms,n25 and event writes110/130ms,n106.
These are an initial diagnostic window, not the final controlled cohort.
The reconciliation model is called even when authoritative decision
comments already determine all spoken events. Its consistency/risks
are not consumed by the gateway; corrections still need model retractions.

## Decision

### Complete baseline and implemented candidate

The complete unchanged3ddd40b cohort is12sessions, three interleaved
windows with two serial sessions per mode: Model3595/5176ms,n70,
questions3742/5176ms,n53; Rules3316/4161ms,n71, questions3462/4161ms,
n53. Talkovers0 in both. Window p50 spread is5.72%/3.58%. Model exceeds
the westus3 baseline by20.07%/26.83%; Rules by17.26%/7.35%. G1 fails.
Exact template/configuration/identity restoration passes. Complete
content-free per-hop/window evidence is `../gates/r2-latency-before-M11.json`.

`Voice:OptimiseTurnPreparation=false` preserves the old path. True selects
two real optimisations: authoritative comments avoid an unused second
assessment, and independent team/prompt reads overlap. Corrections and
updates without authoritative comments still ask the reconciliation model.
The public diagnostic query defaults to assessment and serial reads.
The code-decided questions, events, item grounding and every floor rule
stay unchanged; no model-generated surrogate or response is substituted.
Greenfield templates expose `VOICE_OPTIMISE_TURN_PREPARATION=false`.

First-audio diagnostic endpoints are captured before latency persistence.
Previously that write inflated the reported first-audio hop by about110ms
in the diagnostic window, although total latency was already captured
before the write. The total definition is unchanged; this correction is
not credited as a user-perceived latency saving. Application44/floor49/
Api10/architecture16 focused checks and all three builds pass. Candidate
image `abdd4ff` was built by existing ACR run `ds19`, digest
`sha256:638e5bc39c7f4d8b7aa533098bef134150079977a9e1f84fdf5caccb2fc0e6ad`.
Fresh official validation passed 5/5 before its atomic image/flag update
only to zavaw3. Revision 96 became ready with HTTP 200 and verified
configuration, identity, unrelated settings, bridge image and existing
registry AcrPull preservation. The flag is enabled for the controlled
after cohort, not promoted as a default. After measurements are running;
their outcomes and final acceptance remain unknown.

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
