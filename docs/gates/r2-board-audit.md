# Native board information audit

28 November 2026. Source of the checklist:
`src/meeting-bridge/src/web/board.ts`. Native mappings are in
`src/Zava.Scrum.MediaBot/Board/BoardRenderer.cs`; both consume the gateway
board snapshot. Presentation does not create ids, owners, flags, status,
counts, notes or actions. Published checks in `r2-board-checks.md` remain
unchanged.

| Browser element / data | Native mapping and finding |
| --- | --- |
| Waiting agenda title and preparing message | `PaintStandup(null)` preserves both; waiting raster retained |
| Agenda or `speaker`'s items | `PaintStandup` uses the same supplied name; larger coverage name also highlights that speaker |
| `sprint`, `daysLeft` | Same values, separate numeric line in Consolas32; strict zero-day OCR still fails and is not normalised away |
| `goal`, first70characters | Separate24px line rather than the browser's combined20px header; no extra goal text |
| First six `items`, gateway order | `DrawItems.Take(6)` preserves the limit/order; clipping test covers hidden seventh item |
| Item `id`, `title`, first52characters | Same id/title/clip; title26px rather than22px |
| `type`, `state` | Same metadata line;24px rather than18px |
| Every visible item's `flags` | Joins the supplied list, including Blocked; same amber marker; blocked-state OCR covers the fixture flags |
| `owner` with no speaker | Same conditional owner text; agenda/blocked fixture contains it |
| `covered` tick, green background/stripe | Same semantics; RGB encoding and independent pixel geometry checks |
| `focus` blue item outline | Same supplied id comparison; additional explicit label only when that id is among the six visible items and coverage is not compact |
| Coverage heading and every `team` name | `DrawTeam` preserves the list; up to five names use26px and separate count/status line |
| `covered/total`, including zero | Same numbers with an Items label, not recomputed or replaced; empty fixture covers0/0 |
| Per-person `status` | Preserved alongside counts; away/no-response presentation remains product data |
| Current speaker coverage colour | Same name comparison; active fixture and geometry check |
| Per-person progress bar | Same code-provided ratio, zero total gives zero fill; geometry assertions cover partial/zero bars |
| Standup completion / follow-up state | Split completion and state lines. Native `recording=false` truthfully says meeting-derived data is not saved; it cannot promise saved Teams follow-ups. This pre-existing route difference prevents saved-follow-up demo promotion |
| Clock / stream keep-alive | Clock minimum24px; changed board renders immediately, cached frame is resent once/second. A cached timestamp is not claimed to be a live clock |
| Retro title, sprint, phase, named speaker | `PaintRetro` preserves all supplied fields and Collecting notes/Agreeing actions text |
| Retro column labels and note counts | `DrawColumns` preserves labels/counts/colours |
| Last five notes, first two wrapped lines | Same `TakeLast(5)` and40-character wrap/two-line limit; overflow test covers hidden old note |
| Themes with counts, first110characters | Same theme names/counts and clip limit |
| Actions heading or pre-round hint | Same phase-dependent text |
| First three actions with owners, first100characters | Same owners/text/order/clip; overflow test covers fourth action |
| Retro completion / approval state | Same completion with truthful native non-recording wording; original saved-follow-up wording retained when recording is not false |

The compact layout for more than five members retains20px text,
56px spacing and the original200px first-row baseline, so the tenth
member/bar still fits. It does not add the current-item annotation over
the ninth/tenth rows; the item outline remains. A focused regression test
checks the tenth progress bar and no label overlay.

This is an element/implementation audit, not proof that arbitrary long
metadata/names fit every device. Both boards retain existing row/text
limits; dense retrospectives and large rosters are outside the four-state
standup OCR sample. Compact20px coverage is below the published24px
standup font floor, not an accepted exception or a passed legibility gate.
Those client/layout cases need their own acceptance before promotion.
No browser raster was changed to make the native board
look equal.

## What is proven, and what is not

The actual native adapter uses `IVideoBasedScreenSharingSocket`,
`VideoBasedScreenSharing`, send-only RGB24, and the Graph Sharer role.
The socket contract test checks VBSS rather than camera configuration.
There is no viewer personal tab, install or click in that sending design.
[Graph's documented operation](https://learn.microsoft.com/graph/api/call-changescreensharingrole?view=graph-rest-1.0)
returns202 and updates every participant's roster; it does not prove
automatic presentation on every Teams desktop, guest and mobile UI.

Actual loopback TCP receipt verifies copied RGB24 frame hashes and timing
for ten starts, changes and reconnects each. It is not the SDK H264 encoder
or a Teams receiver. CPU and all-observer delivery stay **not_run,n0**.
The recap is now submitted before call deletion, with an explicitly
reported2second timeout and a2second submission-to-deletion dwell; actual
guest recap visibility remains unproven.

See `r2-board/ocr.json`, `r2-board/receiver.json` and the unedited
[1080p](r2-board/contact-1080.png) /
[480p](r2-board/contact-480.png) contact sheets. The first three OCR reports
remain alongside the final report, including failed zero recognition.
The published100% phrase threshold and glyph12/18/8px thresholds were
not relaxed. Exact operator roles/actions are in
`../DEPLOY.md`, **Round2 native board (ADR0045)**.
