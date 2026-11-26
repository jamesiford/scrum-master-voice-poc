# ADR 0024: Board share and meeting events

Date: 2026-11-26. Status: implemented as a selectable alternative; native meeting acceptance unrun.

## Context

ADR 0015 S2 retained the Azure-managed SSO bot; S7 did not run, so there is no
evidence authorising a personal meeting tab. Prompt 6b provisioned the real
Windows media host but could not join a native meeting unattended. Native mode
remains non-recording, including media-derived notes and coverage (ADR 0023).

## Decision

Keep `Bridge:Mode=Acs`, `Board:Mode=Share` and Direct voice as defaults.
`Board:Mode=MediaShare` selects the real send-only VBSS socket on a GraphMedia
call; it is not camera video and never silently launches a browser. Selecting
Share with GraphMedia gives native audio without VBSS; selecting ACS retains
the existing canvas. Do not remove the proven canvas until native acceptance.
The Simulated Room stays unchanged.

SkiaSharp renders the existing 1280x720 board layout on Windows: waiting, agenda,
speaker items, focus, covered ticks, status/coverage and the retro columns,
themes/actions and completion. The worker draws the latest changed snapshot,
then resends cached RGB24 pixels at one frame per second. Negotiation uses the
SDK's single-resolution raw formats including its mandatory 1.875 fps format,
as documented by the pinned package's XML. [Learn confirms the native VBSS
modality and RGB24 resolution](https://learn.microsoft.com/microsoftteams/platform/bots/calls-and-meetings/real-time-media-concepts).
No camera, microphone or browser is used to render frames.

[changeScreenSharingRole](https://learn.microsoft.com/graph/api/call-changescreensharingrole?view=graph-rest-1.0)
requests sharer when the ceremony opens and viewer when it closes, and all
participants receive a roster notification. The independent worker waits for
the established call and active video socket, renegotiates after reconnect,
and reports failures to the live console without stopping voice. Actual visual
delivery to every participant, especially guests, still needs a native meeting.
An active socket or a successful Send is not proof that a participant saw it.
Sharing status follows actual SDK submission, not an inactive socket's silent
drop. Expected SDK/send/role failures and timeouts keep the worker alive, report
the failure and await reconnect or closure rather than retrying the role every
tick. Reconnect compensates viewer role before renegotiating. Seven focused
worker cases cover this recovery and accepted-frame reporting.

Board messages are carried only on the already-authenticated host WebSocket,
with explicit team and session ids checked again by the host. Prepared facts,
known-id coverage and retro interpretation/agenda reuse the existing Domain
rules in bounded ephemeral memory. There is no repository, publisher, writable
tool or new global team cache on this path. Native completion text says that
meeting-derived data is not saved rather than promising follow-ups. The shared
screen is an organiser-selected view visible to meeting participants, including
guests; it does not grant guest audio/query/approval rights. This is a necessary
clarification of ADR 0023's guest wording, not permission to answer guest requests
or access any other team. The signed-in portal is still required for writes.

Register typed start/end handlers through Microsoft.Agents.Extensions.MSTeams
1.9.205 on the existing Azure-managed ScrumAgent. Correlate the meeting id from
channel data, tenant, unambiguous bound conversation and the stored join URL;
never treat payload Id or conversation id as the meeting id. Start only records
matched lifecycle metadata, not an unsolicited new standup. End uses the normal
aggregate end events with cancellation-independent dispatch, idempotently.
Participant presence comes from the media call roster, not Teams-managed
subscriptions. [Learn meeting APIs](https://learn.microsoft.com/microsoftteams/platform/apps-in-teams-meetings/meeting-apps-apis)
and the [Agents extension](https://learn.microsoft.com/microsoft-365/agents-sdk/teams/teams-meetings)
confirm the scheduled-meeting RSC and channel-data id requirement.

Least permissions: `OnlineMeeting.ReadBasic.Chat` on the SSO messaging app
for scheduled start/end; `Calls.JoinGroupCalls.Chat` on the dedicated calling
app for that meeting's join; the already-admin-consented `Calls.AccessMedia.All`
for raw media and sharing-role changes. No `ChannelMeeting.ReadBasic.Group`
(channel meetings are not this adapter's scope), tenant-wide join role,
participant subscription, new OAuth grant or policy change is added. Preserve
the existing SSO webApplicationInfo identity/resource in generated packages.
No configurableTabs or meetingSidePanel is built because S7 has no positive
evidence.

## Evidence and consequences

The active Global Administrator's actual updated-package catalogue POST was
refused at 21:20:16 UTC, request 6cba8a7e-81ae-460f-a1b8-27abb7fe93ca:
"Missing scope permissions on the request. API requires one of
'AppCatalog.Submit, AppCatalog.ReadWrite.All, Directory.ReadWrite.All'."
Admin role activation does not add absent delegated OAuth scopes. Exact full
refusal and owners/actions are in the run log and DEPLOY.md. Dedicated media
consent was read back as Calls.AccessMedia.All only. Organiser RSC, real stored
calendar/roster identities and real participants remain prerequisites.

Native guest visibility within two seconds, board changes within two seconds,
encoder CPU/cap and G1 with share: **not run, n=0**. Meeting-event live delivery:
**not run, n=0**. The one-call cap stays unchanged, not a measured capacity claim.
Local render parity, lifecycle checks, actual VM renderer readiness and the
deployed Direct Simulated Room before/after measurements are recorded in
`docs/gates/M11-gates.md`; none passes these native gates by proxy.

Exact Chromium pixel identity **fails for all eleven states**: worst MAE
2.273/255 and changed pixels 4.988% in the worst compared region
(retro-complete/header), maximum geometry channel difference 1.
Chromium GPOS shaping/rasterisation differs from Skia's text path. Tests use
explicit 2.5/255 MAE and 6% changed-region tolerance; genuine browser/native PNGs,
regional metrics and provenance are retained. This is a measured deviation
from the plan's exact-identity requirement, not a fabricated native baseline.
Preserve ACS and keep the alternative selectable until actual participant and
latency acceptance, regardless of those local tolerance checks.
