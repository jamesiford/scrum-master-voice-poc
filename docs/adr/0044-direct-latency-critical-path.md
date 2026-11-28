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
preservation. The equivalent after cohort completed all twelve sessions,
without retries or overlapping evaluation, and restored its template,
configuration and identity exactly. Final outcomes:

| Configuration | Before p50/p95, n | After p50/p95, n | Change p50/p95 |
|---|---|---|---|
| Model, all turns | 3595/5176, 70 | 2959/4458, 71 | -17.69%/-13.87% |
| Model, questions | 3742/5176, 53 | 2973/4609, 54 | -20.55%/-10.95% |
| Rules, all turns | 3316/4161, 71 | 3211/4357, 71 | -3.17%/+4.71% |
| Rules, questions | 3462/4161, 53 | 3347/4537, 53 | -3.32%/+9.04% |

All values are milliseconds, nearest-rank percentiles. Talk-overs remain
zero in both modes. After Model windows are 3024/4126, n24; 2714/4401, n24;
2885/4785, n23. Rules windows are 3337/4096, n23; 3120/3814, n24;
3482/4555, n24. P50 spread rises to 11.42%/11.60%; chronological cohorts
cannot exclude ambient drift or isolate the two optimisations causally.
All five hops, question denominators and every window are published in
`../gates/r2-latency-after-M11.json` and `../gates/M11-gates.md`.

Model all-turn M10 deltas -1.17%/+9.24% meet the 5%/10% guides, but question
deltas -2.52%/+12.94% fail the tail guide. Rules all-turn +13.54%/+12.41%
and questions +13.69%/+17.05% fail. Absolute one-second targets also fail.
Therefore G1 remains failed: retain the useful candidate behind the flag,
not as a default. The guarded flag-only update restored false on ready
revision 102 with HTTP 200; measured image and unrelated state stay intact.
Acs/Share/Direct remains the working default, with no storage rollback.

The five-hop accounting excess over each turn's total was 111 ms p50
before (Model p95 132 ms, n70; Rules 136 ms, n71), versus -1/0 ms after,
n71 each, solely integer truncation. This verifies the diagnostic endpoint
repair; it is not counted as the observed total-latency improvement.

Mandatory verification completed 7/8: Direct/ACS standup, authentication,
real approved Azure DevOps write and audit pass. The optional Windows
check reports `The Windows media VM is not running.` Read-only instance
view confirms `PowerState/deallocated`; the shared native host was not
changed. This is not a Direct failure or native acceptance proof. The
native-owning follow-up is documented in `../DEPLOY.md`. Final solution
build has zero warnings/errors (24.61 s); portal and bridge also build.

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
