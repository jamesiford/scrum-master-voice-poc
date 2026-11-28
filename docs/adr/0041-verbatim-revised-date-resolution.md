# ADR 0041: verbatim revised dates and deterministic calendar resolution

## Context and evidence

Historical G7 dates failed at 6/8 (75%), with one timeout and missing binary/
intent probabilities9/1 on72 synthetic cases. The old adapter classified a
date change but extracted the first date phrase, even when it was the old
target. It had no team-local reference day or iteration calendar. Dates
were notes, not Azure DevOps date writes, so this was not a write breach.

The first extended run scored39/48 dates (81.25%), no model failures and no
missing probabilities. Seven failures were date-kind probabilities below
the unchanged0.7 threshold, especially explicit new targets and sprint
periods; two were intent confidence below0.6. The selected revised phrase
was correct whenever an eligible comment survived. Calendar resolution/
explicit abstention scored33/40. These failed numbers remain evidence.

The final frozen112-case run scores48/48 spoken revised expressions and
40/40 calendar resolutions/abstentions. Intent/items/kinds each111/112;
binary accepted precision138/142 (97.18%), intent106/106, missing0/0,
timeouts0, escaped ids0. G7 synthetic release passes unchanged thresholds.
This is one authored synthetic run, not live employee quality or a promise
of perfect future model fidelity.

## Decision

Use the documented Microsoft-Decision `choice` primitive to select an index
from code-extracted verbatim date expressions plus an abstention option:
[current Microsoft Learn contract](https://learn.microsoft.com/azure/foundry/foundry-models/how-to/use-foundry-models-microsoft-decision).
The model cannot generate a date. General classification instructions now
recognise explicit revised targets as well as moves/slips, and distinguish
contrasting old targets from retracting speech. New instructions live in
`agents/instructions/decision-utterance.md`, resolved by owning team.
No goldens or thresholds were tuned to model output:40 additional date
cases were frozen before evaluation; all original72 cases remain identical.

Capture the actual current/next iteration and team time zone before live
facilitation. Calculate the local reference day from `IClock` at analysis,
not the UTC date or preparation date. Resolve today/tomorrow, unqualified
weekdays to the next occurrence including today, and `next` weekdays to a
strictly later occurrence. Resolve explicit sprint-end expressions only
from actual prepared endpoints. Period-only `next sprint/week/month`,
ambiguous numeric/month-day dates, stale/missing calendars and unsupported
expressions abstain explicitly; never assume sprint length or date culture.

Calendar guards also reject reversed ranges, a future-labelled current
iteration, and a next iteration that has already started. A weekend gap
does not invalidate a verified future next iteration. Four focused cases
cover these boundaries without modifying the frozen model goldens.

Application rechecks that a model-selected date is verbatim in the
transcript and recomputes its absolute date, even for the optional chat
adapter. Resolution is note metadata, not a `ChangeOp`, proposal or write.
`FollowUp.GetApprovedChanges` and approved-writer guards are untouched.

## Consequences

No extra iteration I/O on the audio/utterance hot path and no new resource,
permission or fixed hourly cost. Legacy briefs without calendar facts keep
verbatim notes with `calendar_unavailable`; re-prepare to obtain context.
The bounded evaluator acquires one real Entra token in memory outside model
timing. Two earlier probes failed CLI token subprocess cancellation, once
before HTTP and once after68 calls; warming alone did not cache CLI tokens.
The shipped adapter logs credential faults and falls back to rules; missing
confidence is not treated as certainty. No authentication retries or grants.
All raw synthetic results stay untracked; public counts/hashes are in
`gates/r2-quality-M11.json`. No adapter promotion follows from this result.
