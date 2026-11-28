# ADR 0043: mandatory retrospective circle-back instruction

## Context and evidence

The prior single real E2E retrospective passed5/6 assertions, then6/6
assertions on an unchanged retry. These are check counts, not six separate
retrospectives.
The agenda already returned deferred members before themes and recorded
`CircledBack`. The failure was spoken instruction fidelity: a welcome or
generic invitation could omit the three-point prompt, so the scripted
return/correction was not reliably elicited. Existing team prompt overrides
could preserve this behaviour after changing only the default file.

## Decision

Keep the deterministic Domain agenda and floor step. Append the required
`turn-retro-circle-back-required.md` contract after the team style template:
address the member, explicitly say "I'm circling back", and invite the
configured retrospective categories with up to three points. Require its
presence through the real mediator; missing instructions are explicit
errors, not silent advancement or a successful-looking default.

The model still provides actual Voice Live audio. This is a structured
mandatory instruction, not a prerecorded substitute or simulated completion.
The E2E assertion and GateRunner require recorded deferral/return plus the
correct member's spoken return and subsequent note before Themes.

The first real cohort exposed a second cause: run15 recorded the return and
the member's answer but no intervening agent transcript/first-audio timing.
There was no recorded talk-over. The gateway could treat response completion
without audio as successful speech, hand over the floor, and let the cast
answer against the previous member's caption. This is not evidence that the
model merely paraphrased the required clause; the service-side reason for the
empty response is unknown.

The Domain now refuses to complete an empty circle-back: reissue the same
real-model instruction at most twice, then pause and report failure instead
of skipping. Explicit unmute/release replays the invitation. The gateway
checks the atomic first-nonempty-audio observation as well as its actor callback,
waits for actual playout, and preserves the original latency baseline across
retries. Missing prompts pause rather than fabricate successful speech.
Native media uses audio-presence metadata only, preserving non-recording;
exhaustion follows its existing explicit voice-transport failure path.

## Consequences and measurement boundary

Real-mediator tests prove the required clause survives the older optional
style template. `eng/test-retro-fidelity.ps1` drives30 complete existing
Simulated Room retrospectives against zavaw3, using the real model and
refreshing API credentials between runs. Each result is saved independently;
no failed session is silently retried. The complete pre-guard cohort passes
29/30 real-model retrospectives: spoken returns29/30, answers30/30.
Run15 remains failed, not retried or removed. The separate30-run guarded
cohort currently passes25/25 complete real-model sessions, spoken/answered25/25;
5remaining are explicitly unrun. Final counts appear in
`gates/r2-quality-M11.json`.

The original cohort stays on deployed source e7c31fa/revision89; its old
per-run build labels are local metadata, not runtime provenance. The next
cohort explicitly pins the live digest-tag source so documentation
checkpoints cannot change that label.

Focused floor49/49 and native gateway10/10 pass; five new cases cover
empty-response retries/exhaustion, successful recovery, explicit retry and
unchanged ordinary/absent turns. Runtime response retries are not whole-session
test retries and must not be hidden from latency or the failure ledger.

No new resource, permission, recording exception, default adapter or fixed
hourly cost. Voice/model/Cosmos usage remains variable. This proves scripted
instruction fidelity, not actual Teams/user rehearsal or eternal LLM
determinism; any failing run stays failed with its denominator.
