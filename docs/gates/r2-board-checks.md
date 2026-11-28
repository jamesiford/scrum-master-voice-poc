# Native board checks, frozen before measurement

28 November 2026. Operator decision supersedes exact raster identity.
ACS/Share/Direct remains the default until all four requirements pass.
Synthetic board fixtures are presentation data, not customer evidence.

## Pre-stated thresholds

For empty, active-speaker, blocked-item and recap standup fixtures, render
the actual native renderer at its negotiated1280x720 source, then resample
the complete frame to1920x1080 and854x480. Do not OCR text extracted from
the drawing calls: use Windows OCR on those image pixels. Keep raw OCR,
expected phrases, source hashes and dimensions. Each state/resolution
must recognise100% of its pre-listed content phrases after only
case/punctuation/whitespace normalisation. Covered checkmarks, focus
borders and coverage bars are verified separately by geometry tests;
OCR is not their substitute.

Minimum standup body font24source pixels; measure actual lowercase
glyph ink height with the same typeface/rasteriser: at least12pixels at
720p,18at1080p,8at480p. Clock also24; primary heading34or larger.
This is a rendering floor, not proof of subjective mobile usability.
Retrospective content is audited too; its existing dense layout is not
represented by these four standup legibility samples.

Use a side-by-side contact sheet of the same actual Chromium/native states
at both target sizes, without editing either raster. Operator review must
confirm hierarchy, legibility and obvious current speaker/item.

Headless worker receiver: ten first frames, ten changed frames and ten
reconnects; every accepted frame must arrive within2000ms of the initiating
event. Decode/copy the actual RGB24 into the receiver and compare frame
hashes; include inactive-socket negotiation and recap retention. This is
the worker/receiver boundary, not Graph encoding or Teams delivery.

Native encoder CPU: the existing one-call cap, average host CPU<=70% and
p95<=85%, sampled every second for60seconds in an actual call with share.
No camera encoder, ffmpeg surrogate or renderer CPU counts as SDK encoder
evidence. If an actual SDK receiving call cannot be established, record
encoder/delivery as not_run,n0 with exact operator steps.

All-roster/guest acceptance: one actual meeting, at least two roster
observers and one guest, including one480p/mobile view. No participant
installs an app or clicks a tab/share to see it. Record each observer's
first-frame and ten change/reconnect timings<=2000ms. Any required viewer
action fails requirement(a); an SDK202/Send/roster update alone cannot pass.

## Content checklist extracted from board.ts

| Chromium information element | Native requirement / intentional difference |
| --- | --- |
| Waiting title and preparing message | Preserve both |
| Agenda title or named speaker's items | Preserve; speaker must remain visually obvious |
| Sprint name and days remaining | Preserve upstream values |
| Sprint goal, up to70characters | Preserve visible content; separate line improves hierarchy |
| First six items in gateway order | Preserve ids/titles and52-character title limit |
| Work-item type and state | Preserve |
| Every visible item's flags, including Blocked | Preserve; no invented blockers |
| Item owner on agenda when there is no speaker | Preserve |
| Covered tick, green background and stripe | Preserve |
| Focus/current-item blue outline | Preserve; also label actual focus id explicitly |
| Coverage heading, names and covered/total | Preserve; split lines to avoid right-edge overflow |
| Per-person status, such as away/no response | Preserve and wrap separately from counts |
| Highlighted current speaker in coverage | Preserve |
| Coverage progress bars, including zero totals | Preserve code-provided ratios |
| Completion and follow-up approval state | Preserve truthful state; recording=false says data is not saved, not fictitious follow-ups |
| Clock / capture keep-alive | Preserve clock; cached native frames still maintain media, not fresh time claims |
| Retro title, sprint, collecting/agreement phase and speaker | Preserve |
| Retro three column labels and note counts | Preserve |
| Last five notes per column, first two wrapped lines | Preserve existing visible limits |
| Retro themes with counts,110-character limit | Preserve |
| Retro actions heading / pre-round hint | Preserve |
| First three actions with owners,100-character limit | Preserve |
| Retro completion / approval state | Preserve truthful non-recording difference |

The browser already clips long text and limits visible rows. Neither board
is an exhaustive backlog or recap. Native non-recording is a pre-existing
product difference and prevents promotion for the saved-follow-up demo.
