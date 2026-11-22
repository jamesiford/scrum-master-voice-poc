# Demo runbook

## Before the demo (5 minutes)
Placeholders: `<tenant-id>` and `<your-organisation>` come from your user secrets (`Azure:TenantId`, `Ado:Org`) or the azd environment (`AZURE_TENANT_ID`, `ADO_ORG_URL`); see README.
1. `az login --tenant <tenant-id>`.
2. Open the portal (local: `dotnet run --project src\Zava.Scrum.AppHost --launch-profile http`, Aspire dashboard at http://localhost:15888; Azure: the API URL printed by `azd up`).
3. **Live console** tab > **Reset demo** (clears sessions, follow-ups and cards; Azure DevOps is not touched).
4. **Team** tab > check the sprint goal. **Roster** tab > **Sync roster**.
5. Optional: re-seed Azure DevOps with `dotnet run --project tools\SeedAdo -- --tenant <tenant-id> --org https://dev.azure.com/<your-organisation>` (idempotent; `--org` can be replaced by the `Ado__Org` environment variable).
6. Teams: the app package `appPackage/zava-scrum-agent.zip` is sideloaded and you have messaged the bot once, so it can send you cards.

## Scripted run (Simulated Room, about 3 minutes)
1. **Sessions** > **Prepare now** > open the pre-brief with **View** (burndown, flags, questions).
2. **Simulated Room**. Tick **Autopilot (cast plays my lines)** in the room, then press **Start standup**, to let the demo cast play the three-person standup (Daniel Park, Maya Lin and Admin).
3. Watch the **Live console** tab: floor, current speaker, timebox, parking lot, latency. Controls: **Begin**, **Skip speaker**, **Mute agent**, **Unmute**, **Take over**, **Hand back**, **End standup**.
4. When the standup ends: **Follow-ups** tab. Type your name in **Approving as**, press **Approve and write** on one, **Reject** another.
5. Open Azure DevOps and show the "Approved by ..." comment on the approved item and nothing on the rejected one.
6. **Reports** tab: sprint summary, monthly report, then ask "How is attendance?" (the same answer works in Teams chat).
7. **Planning** tab: load per person, carry-over, not-ready items and suggested owners.
8. **Retrospective**, **Story Writer** (pick a parked topic) and **Models and email** tabs for the stretch features.

## Real Teams meeting
1. Create a Teams meeting in the demo tenant and copy the join link.
2. **Live console** tab > pick a prepared session > paste the link > **Start in Teams**.
3. Admit "Zava Scrum Agent" from the lobby. The bridge status line shows Joining, Lobby, Connected, Live.
4. Roster members who join are matched by name (a Teams user called "System Administrator" matches the roster member "Admin"). Anyone who does not join is marked absent. The standup begins 8 seconds after the first match; use **Begin** if it does not.
5. The agent shares the standup board as its screen (flagged-item agenda first, then each speaker's items). Share status appears as Bridge events.
6. Anything that goes wrong is shown on the Live console tab as a Bridge event (invalid link, lobby denied, removed).

### Things worth showing in a live run
- Ask "Scrum Agent, is that one overdue? What was the due date?" mid-update.
- Ask "What am I supposed to report on?" on your turn.
- Have a colleague say "Daniel stepped out, he'll be back in a few minutes": he is skipped and circled back to at the end.
- Stay silent when called: the agent records present-but-no-response (not absent).
- Correct yourself ("scratch that, I meant...") and show the removed blocker in the follow-up.
- Have one person say "I'll give Admin's update": the follow-up still needs Admin's approval.

## If something fails
| Symptom | Fix |
|---|---|
| "Voice Live is unavailable" | `az login` again; check the Foundry deployments exist. |
| Standup is slow to answer | Normal p50 is 2 to 3 seconds; say so, and show the latency number. |
| Cards missing in Teams | Message the bot once; cards are always in **Follow-ups** > **Show card outbox**. |
| Teams join fails | Run the Simulated Room instead; it is the fallback for any live demo. |
| Approve shows a write error | Check the managed identity is in the Azure DevOps project Contributors group; use **Retry write**. |

## M9 features (ceremony packs, unblock loop, carry-over)
Needs `az login`, the stack running and, for packs, a reachable Azure DevOps project. Nothing below writes to Azure DevOps until a person approves.

1. **Re-seed** Azure DevOps (`dotnet run --project tools\SeedAdo -- --tenant <tenant-id> --org https://dev.azure.com/<your-organisation>`) so the at-risk task "Prepare reconciliation report for audit" exists.
2. **At-risk flag:** **Sessions** > **Prepare now** > **View**: Daniel Park's brief flags that task. In a standup the agent asks about overdue items first, then at-risk, then stale.
3. **Unblock loop:** run a standup (Simulated Room), say a blocker ("I am blocked on the gateway timeouts"), and answer "Who can help unblock this?" with a teammate's name. After **End standup**, **Follow-ups** shows an unblock request for that teammate (and the outbox shows an email when `Features:EmailFollowUps` is on).
4. **Carry-over:** run the standup again after a day (or a second standup): the pre-brief lists the still-blocked item with its unblocker, and the agent opens with "Yesterday you said you'd finish ...". A commitment still open two standups later raises a Scrum Master escalation.
5. **Review and retrospective packs** (no portal button yet):
   `curl -X POST http://localhost:5080/api/ceremonies/SprintReview/pack -H "Content-Type: application/json" -d "{\"teamId\":\"team-payments\"}"` (also `Retrospective`). The card appears in **Follow-ups** > **Show card outbox** and in Teams. Type your name in **Approving as** and press **Approve and write**: comments are added to the completed stories, or the retro action tasks are created, each with "Approved by {name} in the portal, proposed by Zava Scrum Agent from the {date} {ceremony}."
6. **Recap routing:** add the bot to a team channel and it becomes the recap channel (`PUT /api/teams/team-payments/recap-channel` sets it by hand). The recap then goes to that channel and the meeting chat instead of every conversation.
7. **Graph email (optional):** set `Features:EmailFollowUps=true`, `Email:Mode=Graph` and `Email:Sender=<agent mailbox>`; absent, no-response and unblock members receive an email as well as the card.

## Sprint planning (M9a)
Needs the stack running and a seeded sandbox (re-run `SeedAdo` first: it adds the next iteration "Sprint 43" and 10 backlog items, and puts back anything a plan moved). The velocity history is the illustrative fixture and the portal says so.

1. **Reset:** **Live console** > **Reset demo** (clears packs, sessions, follow-ups and cards; Azure DevOps is not touched).
2. **Planning** tab. Optional: set **Maya Lin**'s availability to 50 and leave the field; her capacity halves the next time you prepare the pack, and the plan warns if she is overloaded.
3. Press **Prepare pack**. The page shows the draft goal, the velocity range, capacity bars per person, the proposed selection (carry-over first, then ready backlog items in order, suggested owners marked), **Not planned** with a reason on each excluded item (no story points, no acceptance criteria, too large, beyond capacity, not started), and a warning when the plan is over- or under-committed.
4. The card **Sprint planning: Sprint 43** arrives in **Follow-ups** > **Show card outbox** and in Teams, with **Approve plan** and **Reject**.
5. **Reject** first (type your name in **Approving as**): nothing changes in Azure DevOps. **Prepare pack** again.
6. Untick one item if you like, type your name and press **Approve plan** (or press **Approve plan** on the Teams card). The page shows "Plan written to Azure DevOps" with links to the work items. In Azure DevOps the kept items are in `Sprint 43`, the unowned ones have the suggested owner (a `demo-owner-*` tag in the sandbox) and each has an "Approved by {name} in the portal (or Teams), proposed by Zava Scrum Agent from the {date} sprint planning." comment.
7. Chat: in Teams, a Scrum Master (a name in `Team:ScrumMasters`) can type "prepare sprint planning" to the bot; anyone else is told only the Scrum Master can.
8. Re-run `SeedAdo` to put the items back for the next run.

## Retrospective by voice (M9b)
Needs the stack running, a seeded sandbox and an Azure Speech resource for the cast voices (as for the scripted standup). About 5 minutes in the Simulated Room.

1. **Reset:** **Live console** > **Reset demo**.
2. **Retrospective** tab > **Prepare retro**. A card **Run it by voice** appears.
3. Press **Start in Simulated Room**. The room opens in a new tab: press **Start standup** after ticking the autopilot box (or open the room link with `&autopilot=1`, as the e2e does). The cast (`demo/cast/payments-retro.yaml`) plays three people: Daniel steps away and is circled back to, Maya gives one unclear note so the agent asks "Is that something to start, stop or keep doing?", and Daniel corrects himself inside one answer.
4. Watch the board in the Teams meeting's shared screen (three columns that fill live, then the actions), or the **Live console** for the floor and timer. After the round the agent reads the themes back, asks for an action from each person and confirms the owner, then closes with a recap.
5. The Scrum Master gets a **Retrospective actions** card (**Follow-ups** > **Show card outbox**, and in Teams) and a summary in the recap channel. Press **Reject** on a copy of the card first if you want to show that nothing is created.
6. Type your name in **Follow-ups** and press **Approve**. In Azure DevOps there are Tasks tagged `retro-action` in `Sprint 43`, one per action, with a `demo-owner-*` tag for the owner and an "Approved by {name} in the portal, proposed by Zava Scrum Agent from the {date} retrospective." first comment.
7. In a Teams meeting: **Retrospective** tab > **Prepare retro**, paste the meeting link and press **Start in Teams** (not checked live yet; the standup run is the reference). `Retro:Minutes` sets the timebox and `Retro:Format` the questions.
8. Delete the created tasks and re-run `SeedAdo` before the next run. Automated: `npm run e2e:retro --prefix src/meeting-bridge` with the API and bridge running.

## Sprint calendar (M9d)
Needs the stack running, a seeded sandbox (the next iteration "Sprint 43" supplies the dates) and, for the full demo, Graph: set `Calendar:Organizer` to the agent's demo-tenant mailbox and grant the app `Calendars.ReadWrite` scoped by an application access policy. Roster members need an email: set `Demo:MemberEmails:daniel`, `:maya` and `:admin` in user secrets (then run `POST /api/teams/team-payments/members/sync`). Create the Graph app with `Calendars.ReadWrite` (application) and admin consent (done for `mcm-scrum-agent-calendar`), and scope it with `New-ApplicationAccessPolicy` to the organizer mailbox. The calendar writer needs an app token: the az CLI token is refused (403) on calendars, so give it an app with `Calendars.ReadWrite` through `Calendar:ClientId` and `Calendar:ClientSecret`. Without `Calendar:Organizer` steps 1 to 4 still work and approving points you to the `.ics`.

1. **Reset:** **Live console** > **Reset demo**.
2. **Schedule** tab, **Proposed sprint calendar**: the plan for the next sprint (daily standups, planning on day one, the retrospective on the last day; a review only when `Team:HasSprintReview` is true). Press **Download .ics (fallback)** to see the file.
3. Press **Propose calendar**. The card **Proposed sprint calendar: Sprint 43** arrives in **Follow-ups** > **Show card outbox** and in Teams, with **Approve** and **Reject**.
4. **Reject** first (type your name in **Approving as**): nothing is created. **Propose calendar** again.
5. Type your name and press **Approve** (or press **Approve** on the Teams card). The proposal shows "Approved" and "N of N meetings created"; the meetings appear in Outlook for the roster as Teams meetings, owned by the agent's mailbox. A Graph failure part-way shows the error; **Approve** again creates the rest.
6. The **Schedule** is now enabled in Teams mode with each standup's join URL stored. At the next standup time the scheduler prepares the session ten minutes before and joins the calendar meeting one minute before, with no pasted link. (To try it today, change the schedule's start time to a few minutes from now and make the first approved meeting start then.)
7. Delete the meetings from the agent's calendar before the next run.
