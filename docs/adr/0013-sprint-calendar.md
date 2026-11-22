# 0013. Sprint calendar (M9d)

## Context
CEREMONIES O2: from the iteration dates the agent proposes the sprint's meetings, and after approval creates them as Teams meetings in the demo tenant and invites the roster; the scheduler then joins the standup without a pasted link. The approval rule extends beyond Azure DevOps: nothing reaches anyone's calendar without a named approver.

## Decision
- **Pure planner.** `SprintCalendarPlanner` (Domain) lays out standups on every working day, planning after the day-one standup, and the review (only when `Team:HasSprintReview`) and retrospective after the last-day standup. Dates are dates; the standup time is local and converted with the team's zone so daylight saving holds. `IcsFormatter` writes the fallback file.
- **The brief's `GetSprintCalendarQuery -> card` is split.** A query cannot send a card, so `GetSprintCalendarQuery` is the read-only plan and `ProposeSprintCalendarCommand` stores the proposal and raises `SprintCalendarProposed`, whose handler sends the card. The card carries the calendar id like the planning card does.
- **Aggregate.** `SprintCalendar` (statuses Proposed, Approving, Approved, Rejected) is the guard: `Approve(approver)` refuses an empty name or a decided calendar and returns only meetings not yet created. A partial failure leaves it Approving; a retry creates the rest, and each meeting has a stable `transactionId`.
- **Ports.** `ICalendarWriter` (create one Teams meeting, `Enabled`), `ISprintCalendarRepository`, `ICalendarSettings` (`Calendar:*` and `Team:HasSprintReview`), and `IIterationReader.FindIterationAsync` for iteration dates. Handlers share `SprintCalendarBuilder`, a plain helper, because handlers do not call handlers.
- **Graph.** `GraphCalendarWriter` posts an event with `isOnlineMeeting` and `teamsForBusiness` to the organizer's calendar (`Calendar:Organizer`) and reads `onlineMeeting.joinUrl`. No Organizer means the writer is disabled and approving returns an error that points to `/api/calendar/{iteration}.ics`. `// VERIFY` the permission and access policy on Learn. `// PROD:` a recurring series, update and cancel on date changes.
- **Scheduler.** `Schedule.Meetings` holds the standup join URL per date; `JoinUrlFor(date)` prefers it over the pasted link. The brief says "calendar read"; the URLs are stored at creation instead of being read back, which avoids a second Graph permission and a read path. Approving also switches the schedule to enabled and `acs`, which the demo check needs; this is stated on the card text and in the demo script.
- **`.ics`.** The brief said to keep an existing `/api/calendar/{iteration}.ics`; no such route existed, so it is built here.

## Consequences
- Without Graph the feature still plans, shows the card and offers the file; only creating meetings needs the tenant.
- Checked live in the demo tenant: 12 Teams meetings created with the roster invited, and the scheduler joined the calendar meeting (lobby) without a pasted link. The Exchange application access policy was not created (it needs an interactive Exchange Online sign-in), so the app can currently write any mailbox until one is added.
- The schedule's "Teams meeting link required" rule moved from the validator to the handler: a link is not needed once a calendar has supplied join URLs (found while testing the auto-join).
- Approving changes the schedule's mode, which may surprise a team that pasted a link on purpose.
- One event per meeting means ten standup events a sprint; a series is a later change behind the same port.
