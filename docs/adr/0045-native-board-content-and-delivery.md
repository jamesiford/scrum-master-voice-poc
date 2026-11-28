# ADR 0045: Native board content and delivery, not pixel identity

Date: 28 November 2026. Status: selectable native alternative; no promotion.

## Context

The operator supersedes exact Chromium/native raster identity. G4 now
requires automatic screen-share reception for every roster member/guest,
intuitive hierarchy and legibility, the same information, and first/change
delivery within2seconds. Eleven historical raster differences remain
evidence but are not a defect under this decision.

## Decision

Retain Acs/Share/Direct, Simulated Room, all approvals and non-recording.
Native uses the same actual team/session facts, with a separate sprint-goal
line, larger standup item metadata, clearer coverage names/counts/status,
and an explicit current-item label only for a visible supplied id.
Native completion remains honest: no meeting-derived data is saved.
Do not promise browser-style saved follow-ups on the non-recording route.
Dense retrospective content and visible limits remain; they are not
represented by the four standup legibility fixtures.

The worker previously stopped sharing before submitting its ended frame.
Keep the share until its call owner closes it. Before deleting the Graph
call, await submission of a received recap (maximum2seconds, explicitly
reported timeout), then retain sharing2seconds before deletion. This is submission,
not an assertion that a guest saw it. Voice processing remains independent.
Draining is conditional on the ended snapshot already being received.
Ordering of the final WebSocket board against the HTTP leave request, and
actual client recap visibility, remain unproven live; the acceptance
protocol must check them rather than infer delivery from a rendered PNG.

[Current Learn](https://learn.microsoft.com/graph/api/call-changescreensharingrole?view=graph-rest-1.0)
defines app-hosted sharing, Calls.AccessMedia.All,202 and a roster update
to all participants with videoBasedScreenSharing/sendOnly. The real adapter
uses that sharer role and a VBSS send socket, not a camera, personal tab or
per-viewer app install. [Media concepts](https://learn.microsoft.com/microsoftteams/platform/bots/calls-and-meetings/real-time-media-concepts)
distinguishes webcam video from VBSS and confirms SDK encoding/encryption.
It does not guarantee automatic UI visibility/timing on every client.
Current Learn also explicitly does not recommend media bots for meeting
AI agents; retain this existing optional PoC, do not infer platform
endorsement or add speculative GPU/infrastructure investment.

## Evidence

Checks and thresholds were published in a90942d before measurement;
expected phrases are in `../gates/r2-board-fixtures.json`. The
[element audit](../gates/r2-board-audit.md) includes standup/retro,
statuses, flags, owners, coverage, focus and truthful completion.
FinalactualnativeOCR52/53 at1080p and480p fails the100% phrase threshold:
zero-days recognition remains wrong. Glyphink12/18/8 at720/1080/480 meets
the published floors. Initial51/53, second52/53, and third53/53 at1080/
52/53 at480 reports remain; finalsource is32px numeric text, not the
earlier best-looking cohort. Chromium unchanged52/52 and46/52.
ActualRGB24TCP receiver maxima153.51/113.80/147.02ms,n10first/change/
reconnect each meets2000ms locally. No SDK CPU/guest delivery inference.
The local reconnect event is worker re-sharing over the same TCP carrier;
forced TCP/API WebSocket disconnect and Graph/SDK negotiation remain n0.
RawOCR/source hashes and side-by-side contact sheets are in `../gates/r2-board`.
Compact>5rosters keep the original tenth row/bar and do not overlay a focus
label; their20px font and dense retro17/18px text remain outside the
four-state standup legibility sample. Compact20px coverage is below the
published24px standup font floor; preservation of existing large-roster
content does not make that an accepted exception or a passed gate.
No threshold is relaxed to pass. No new route, use case, data store,
permission, role, resource or fixed cost is introduced.
The existing VM had one authorised start12:03:09UTC; running confirmed
12:04:26UTC, final Deallocated confirmed12:58:26UTC. Approximate
allocation window0.921hours atUSD0.35-0.38/hour gives estimated
computeUSD0.3224-0.3500, not actual billing. API/bridge remain running.

Native13a842d is deployed and exact DLL SHA256 is recorded in
[runtime evidence](../gates/r2-board-runtime.json). Trusted HTTPS,
renderer1280x720/cap1/non-recording and protected app/role preservation
passed at12:49:15UTC. The first inline readback script failed because
Azure CLI stripped quotes; a read-only file-input receipt matched exact
bytes/version/font without another deployment or start.
Mandatory before7/8 became after8/8 while allocated, with genuine approved
write/audit revisions81/82,n1 each. These are not Teams acceptance.
Direct all-turn3448/4262ms,n12 became3407/6497ms,n12
(-1.19%/+52.44%); questions3448/4253ms,n9 became3590/4663ms,n9
(+4.12%/+9.64%). G1 still fails, including the substantially worse tail.
Same unchanged API/defaults do not make separate chronological windows
causal evidence for native-board changes; no favourable rerun was made.
Native readiness is time-bounded: the host is intentionally offline after
deallocation. A future real-meeting rehearsal needs new scoped allocation
authority, readiness verification and eventual restoration/deallocation.

Real Teams all-roster/guest reception, encoder CPU at cap1, reconnect and
live delivery remain not_run,n0 until the organiser/observer protocol in
DEPLOY.md is performed. Local RGB24 receiver evidence cannot close them.
Native is not promoted: strictOCR fails, real reception/CPU is unrun, and
non-recording does not deliver the pre-M11 saved-follow-up experience.
Improve the actual zero-day presentation/real client behaviour in its
own evidence-backed follow-on, not expectations or a personal-tab shim.
