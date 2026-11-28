# ADR 0044: measure the Direct critical path before promotion

## Context

G1 failed before this change. The inherited quality measurement was
3615/4453 ms p50/p95, n12, versus 3418/4559 ms, n10, in independent windows.
The westus3 M10 reference remains Model 2994/4081 ms and Rules 2828/3876 ms,
n36 each, with unchanged 5% p50 / 10% p95 tolerances.

Before editing runtime code, twelve unchanged `3ddd40b` sessions ran in
three interleaved windows, two serial same-cast sessions per mode per
window. Model measured 3595/5176 ms, n70, questions 3742/5176 ms, n53;
Rules 3316/4161 ms, n71, questions 3462/4161 ms, n53. Talk-overs were zero.
Window p50 spread was 5.72%/3.58%, confirming meaningful same-build variance.
Against M10, Model was +20.07%/+26.83%, Rules +17.26%/+7.35%; G1 failed.
Complete content-free hop/window evidence is
`../gates/r2-latency-before-M11.json`. Configuration and identity were
restored exactly; failures were retained, not rerun to obtain green numbers.

The worst initial 7968 ms turn included 5899 ms orchestration. Recent
diagnostic spans measured reconciliation 1505/1924 ms, n25, instruction
121/199 ms, n25, and event persistence 110/130 ms, n106. These spans are not
precisely matched to the final cohort. Overlapping hops and their percentiles
must not be summed or attributed causally to storage changes.

## Decision

`Voice:OptimiseTurnPreparation=false` preserves the original path. True
selects real optimisations: authoritative comments avoid an unused second
reconciliation assessment, and independent team/prompt reads overlap.
The gateway consumes neither the skipped consistency assessment nor its
risks. Corrections, rules/model failures and absent authoritative comments
still require the model; diagnostic queries retain assessment by default.
Code-decided questions/events, item grounding, floor silence, barge-in,
circle-back and approval guards remain unchanged. No surrogate adapter,
model response, static answer or cache of team data is introduced.
Greenfield templates expose `VOICE_OPTIMISE_TURN_PREPARATION=false`.

First-audio diagnostic endpoints are captured before latency persistence.
Previously that write inflated the hop by about 110 ms in the diagnostic
window, although total latency was already captured before it. Total
semantics are unchanged: the correction is not perceived speed improvement.
Application 44, floor 49, Api 10 and architecture 16 focused checks pass;
all three builds, format and TypeScript checks pass.

Committed `abdd4ff` was built by existing ACR run `ds19`, digest
`sha256:638e5bc39c7f4d8b7aa533098bef134150079977a9e1f84fdf5caccb2fc0e6ad`.
Fresh official validation passed 5/5 before its atomic image/flag update
only to zavaw3. Revision 96 became ready with HTTP 200 and verified
configuration, identity, unrelated settings, bridge image and AcrPull
preservation. The flag is enabled for measurement, not default promotion.

At 02:49 PT, four of twelve planned after sessions are complete:
Model 3024/4126 ms, n24, questions 3136/4458 ms, n18; Rules 3337/4096 ms,
n23, questions 3358/4537 ms, n17; zero talk-overs. This is only one window
per mode, below the required question/window counts. Finish the remaining
eight sessions; no final acceptance is claimed from this favourable prefix.

## Consequences and platform evidence

Report all turns and questions, every hop/window and talk-overs before/after.
Simulated transcript zero is not acoustic transcription or native acceptance.
The absolute one-second target and historical failures remain visible.
Added fixed resource cost is USD0/hour; existing variable charges continue.
No new service, identity, role, environment or human gate is required.

Current Learn confirms immutable revision-scope changes and the integrated
Voice Live connection, not a one-second application guarantee:
https://learn.microsoft.com/azure/container-apps/revisions
https://learn.microsoft.com/azure/ai-services/speech-service/voice-live
A guessed dedicated low-latency page returned 404; no unsupported setting
was added. Existing floor timers, thresholds and goldens were not tuned.
