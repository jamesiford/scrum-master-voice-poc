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
4. When the standup ends: **Follow-ups** tab. The page shows **Deciding as** your signed-in account (your Entra identity is what is recorded). Press **Approve and write** on one, **Reject** another. The comment in Azure DevOps ends with `Correlation: zsa-...`; open `/api/audit/zsa-...` (or the full trace id on the session) to see the chain.
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
| Standup is slow to answer | Measured p50 is 3 to 4 seconds (22 Nov), p95 about 5; say the number on screen. |
| `ado.auth_failed: No Azure credential available` on a local run | `DefaultAzureCredential` stops at the managed-identity probe on some machines. Start the stack with `$env:AZURE_TOKEN_CREDENTIALS='AzureCliCredential'` and `az login`. |
| Cards missing in Teams | Message the bot once; cards are always in **Follow-ups** > **Show card outbox**. |
| Recap or follow-ups missing after the standup ends | Fixed and verified in a real Teams meeting (ADR 0016: the hang-up cancelled the end-of-standup handlers). If it recurs, look for `StandupEnded raised for session` and `Sent N follow-up card(s)` in the API log for the session id. |
| Teams join fails | Run the Simulated Room instead; it is the fallback for any live demo. |
| Approve shows a write error | Check the managed identity is in the Azure DevOps project Contributors group; use **Retry write**. |

## M9 features (ceremony packs, unblock loop, carry-over)
Needs `az login`, the stack running and, for packs, a reachable Azure DevOps project. Nothing below writes to Azure DevOps until a person approves.

1. **Re-seed** Azure DevOps (`dotnet run --project tools\SeedAdo -- --tenant <tenant-id> --org https://dev.azure.com/<your-organisation>`) so the at-risk task "Prepare reconciliation report for audit" exists.
2. **At-risk flag:** **Sessions** > **Prepare now** > **View**: Daniel Park's brief flags that task. In a standup the agent asks about overdue items first, then at-risk, then stale.
3. **Unblock loop:** run a standup (Simulated Room), say a blocker ("I am blocked on the gateway timeouts"), and answer "Who can help unblock this?" with a teammate's name. After **End standup**, **Follow-ups** shows an unblock request for that teammate (and the outbox shows an email when `Features:EmailFollowUps` is on).
4. **Carry-over:** run the standup again after a day (or a second standup): the pre-brief lists the still-blocked item with its unblocker, and the agent opens with "Yesterday you said you'd finish ...". A commitment still open two standups later raises a Scrum Master escalation.
5. **Review and retrospective packs** (no portal button yet):
   `curl -X POST http://localhost:5080/api/ceremonies/SprintReview/pack -H "Content-Type: application/json" -d "{\"teamId\":\"team-payments\"}"` (also `Retrospective`). The card appears in **Follow-ups** > **Show card outbox** and in Teams. Press **Approve and write** (you are recorded from your sign-in): comments are added to the completed stories, or the retro action tasks are created, each with "Approved by {name} in the portal, proposed by Zava Scrum Agent from the {date} {ceremony}."
6. **Recap routing:** add the bot to a team channel and it becomes the recap channel (`PUT /api/teams/team-payments/recap-channel` sets it by hand). The recap then goes to that channel and the meeting chat instead of every conversation.
7. **Graph email (optional):** set `Features:EmailFollowUps=true`, `Email:Mode=Graph` and `Email:Sender=<agent mailbox>`; absent, no-response and unblock members receive an email as well as the card.

## Sprint planning (M9a)
Needs the stack running and a seeded sandbox (re-run `SeedAdo` first: it adds the next iteration "Sprint 43" and 10 backlog items, and puts back anything a plan moved). The velocity history is the illustrative fixture and the portal says so.

1. **Reset:** **Live console** > **Reset demo** (clears packs, sessions, follow-ups and cards; Azure DevOps is not touched).
2. **Planning** tab. Optional: set **Maya Lin**'s availability to 50 and leave the field; her capacity halves the next time you prepare the pack, and the plan warns if she is overloaded.
3. Press **Prepare pack**. The page shows the draft goal, the velocity range, capacity bars per person, the proposed selection (carry-over first, then ready backlog items in order, suggested owners marked), **Not planned** with a reason on each excluded item (no story points, no acceptance criteria, too large, beyond capacity, not started), and a warning when the plan is over- or under-committed.
4. The card **Sprint planning: Sprint 43** arrives in **Follow-ups** > **Show card outbox** and in Teams, with **Approve plan** and **Reject**.
5. **Reject** first: nothing changes in Azure DevOps. **Prepare pack** again.
6. Untick one item if you like, type your name and press **Approve plan** (or press **Approve plan** on the Teams card). The page shows "Plan written to Azure DevOps" with links to the work items. In Azure DevOps the kept items are in `Sprint 43`, the unowned ones have the suggested owner (a `demo-owner-*` tag in the sandbox) and each has an "Approved by {name} in the portal (or Teams), proposed by Zava Scrum Agent from the {date} sprint planning." comment.
7. Chat: in Teams, a Scrum Master (an Entra object id in `Identity:AdminObjectIds`) can type "prepare sprint planning" to the bot; anyone else is told only the Scrum Master can.
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
4. **Reject** first: nothing is created. **Propose calendar** again.
5. Type your name and press **Approve** (or press **Approve** on the Teams card). The proposal shows "Approved" and "N of N meetings created"; the meetings appear in Outlook for the roster as Teams meetings, owned by the agent's mailbox. A Graph failure part-way shows the error; **Approve** again creates the rest.
6. The **Schedule** is now enabled in Teams mode with each standup's join URL stored. At the next standup time the scheduler prepares the session ten minutes before and joins the calendar meeting one minute before, with no pasted link. (To try it today, change the schedule's start time to a few minutes from now and make the first approved meeting start then.)
7. Delete the meetings from the agent's calendar before the next run.

## Gate runner (latency and traceability)
`tools/GateRunner` plays the Simulated Room cast (`demo/cast/payments-standup.yaml`, autopilot) through the real API in Edge and records, for every agent reply that follows someone speaking, the time from the end of their speech to the first audio, split into: speech end to transcript, analyser, decision model, orchestration (floor control, the wait for silence, the reconcile) and Voice Live first audio. Each run is saved as JSON; `summary.md` gives p50, p95 and n per hop (nearest-rank, no interpolation) and the build id. Every later change re-runs it and pastes the summary into the pull request and `docs/gates/M11-gates.md`.

1. Start the API and the bridge (`dotnet run --project src\Zava.Scrum.Api --urls http://localhost:5080`; `npm run start --prefix src\meeting-bridge`). The configuration under test is set on the API: the default is the decision model (`UtteranceAnalysis__Engine=Decision`); for the rules-only run set `UtteranceAnalysis__Mode=Rules` before starting it.
2. `dotnet run --project tools\GateRunner -- --label decision --runs 3 --out docs\gates\baseline-M10` (use `--label rules` for the other configuration; `--build <id>` names a deployed image tag, default is the git commit).
3. `dotnet run --project tools\GateRunner -- --summarise docs\gates\baseline-M10` rebuilds the summary from the saved JSON.
4. `--check-audit` runs the traceability check (G3) after each run: it proposes and approves one change through the API as the caller, waits for the write, and fails unless `GET /api/audit/{correlationId}` shows the session, the approval record with the approver's object id and the write record with an Azure DevOps revision. The caller needs the Approver and Reader roles (a local Development API gives every role); the check writes one comment on work item 1.
5. The runner prints `docs/gates/teams-rehearsal.md` at the end: a ten-step checklist a person ticks in a real Teams meeting.

To drive a deployed API behind Easy Auth, pass `--api <url> --token <bearer>` (or `GATE_TOKEN`); the bridge room must be reachable from the machine running the runner. A run takes about six and a half minutes.

To stand the whole environment up from nothing, and to check it afterwards, see `docs/DEPLOY.md` (`azd up`, then `pwsh infra/hooks/verify.ps1`). Run the gate runner against the deployed API with a token from `az account get-access-token --scope api://<PORTAL_CLIENT_ID>/access_as_user`; `docs/gates/baseline-M10-deployed/` holds the deployed M10 baseline.

## Teams, onboarding and roles (M11 prompt 3A)
The portal serves every team. The switcher in the sidebar lists only the teams you belong to and remembers your choice; an Admin sees all of them and gets an **Administration** group (Fleet, Teams, Onboard a team).

- **Roles.** Admin (the Entra app role) manages the registry and sees every team. In each team a person is a **team lead** (approve, schedule, edit prompts, manage members), a **member** or a **reader** (read only). A person in no team sees "You are not in a team yet". Add people on the Team page by Entra object id, or set an Entra group on the team and press **Sync from Entra group**.
- **Onboard a team.** Administration > Onboard a team: name and id, the Azure DevOps organisation (press **Load projects**, then pick the project and team), **Run test** (every step must pass), create and sync the roster, schedule, an optional Teams channel id, then **Enable**. The team stays a draft until the last step. Adding a new Azure DevOps organisation first needs the human gate in `docs/DEPLOY.md`.
- **Bind a Teams channel.** In a channel the bot is installed in but no team is bound to, the bot answers with a card and nothing else. An Admin or that team's lead types the team id and presses **Bind**. Or add the channel id on the team's edit panel.
- **Three demo teams.** `dotnet run --project tools\SeedAdo` seeds Payments, Identity and Catalogue (one project each). The API registers them from `Demo:Teams`. For the film set `Demo:PaymentsOnly=true` and seed with `--payments-only`: one team, as before.
- **Prompts.** The Prompts page edits the selected team's override and says whether the default, a deployment override or the team's own is in effect.
- **Pause and archive.** Pause stops a team's ceremonies from starting; archive makes it read-only. Neither deletes anything.
- **Capacity.** If more standups are due than the host allows, the extra ones wait and the schedule shows why; after the grace the team's recap channel is told.
- **Three at once.** `dotnet run --project tools\GateRunner -- --label g9 --teams team-payments,team-b,team-c --api <url> --token <bearer> --out docs\gates\g9 --check-audit` runs three Simulated Room standups together (it registers the extra teams as copies of the first). `--summarise <dir> --only-team <id>` filters the summary by team.

## Comparing a realtime model or the silence setting (M11 prompt 3B)
`docs/gates/m11-p3b/bench.ps1` sets the model, the analyser mode, the silence setting and the cast on the deployed API and bridge, then runs the gate harness with three Simulated Room standups at once (one per team) and saves the results. Example: `pwsh docs/gates/m11-p3b/bench.ps1 -Configs rt21-model,rt21-rules`. The valid analyser modes are `Model` (the decision model reads each utterance) and `Rules`. `Demo__CastFile=demo/cast/payments-standup-pauses.yaml` on the bridge plays the cast with mid-sentence pauses (`[pause N]` in a line). Compare only batches run next to each other: the same configuration moved by about 25 % between time windows. `GateRunner` prints the number of times the agent spoke over a person and the transcript pieces per run. If `prepare` returns 500 after many runs, see the Cosmos item limit in `docs/gates/m11-p3b/README.md` and use `POST /api/demo/reset`.

## Approving as yourself (M11 prompt 5A)

An approval is written to Azure DevOps as the person who approved it, so the revision shows their name, not the service identity.

1. Sign in to the portal as a person who holds the Approver role and is a Contributor of the Azure DevOps project, and approve a follow-up. In Azure DevOps the comment's author is that person, and the text ends `Correlation: zsa-<id>`.
2. In Teams the first Approve shows a sign-in prompt once (single sign-on); press Approve again. If Teams single sign-on is not set up, Approve says so and nothing is written.
3. If the write fails because your token was not accepted (consent, multi-factor) or you are not in the project, the follow-up stays approved and unapplied with the reason; sign in again and press Retry. Someone else pressing Retry is refused.
4. `pwsh infra/hooks/verify.ps1` fails unless the comment author is the approver.

### Entra sign-in log queries for an approval (5A)

An approval produces two sign-in entries for the approver's account: the interactive sign-in to the portal (or Teams single sign-on), and a non-interactive one for the on-behalf-of exchange.

- Entra admin centre > Monitoring > Sign-in logs > **User sign-ins (non-interactive)**: filter Application = `Zava Scrum Portal (<env>)`, Resource = `Azure DevOps` (499b84ac-1321-427f-aa17-267ca6975798). The row at the approval time is the exchange; a failure code here (50076 multi-factor, 53003 Conditional Access, 65001 consent) explains a refused approval.
- Log Analytics (if sign-in logs are exported), approver and exchange:

```kusto
union SigninLogs, AADNonInteractiveUserSignInLogs
| where TimeGenerated > ago(1h) and UserPrincipalName =~ "<approver upn>"
| where AppDisplayName startswith "Zava Scrum Portal" and (ResourceDisplayName == "Azure DevOps" or ResourceDisplayName startswith "Zava Scrum Portal")
| project TimeGenerated, Type, AppDisplayName, ResourceDisplayName, ResultType, ResultDescription, ConditionalAccessStatus, CorrelationId
| order by TimeGenerated desc
```

## The hosted voice route, pre-warm and canary (M11 prompt 4)

The voice can run through a Foundry hosted agent (a relay in front of Voice Live) or directly. Both give the same standup; the hosted route is selected by `VoiceLive__RelayEndpoint` on the API (the `voiceRoute` Bicep parameter, `Direct` or `Hosted`).

1. **See which route a session used.** Prepare a standup (Prepare now, or wait for the scheduler's pre-brief). `GET /api/sessions/{id}` shows `hostWarmUp`: `{ route, warmed, milliseconds, note }`. On the hosted route `warmed` is true and `milliseconds` is the cold start the standup no longer pays (about 3.5 to 4 s); on the direct route it says there is no host to warm.
2. **Switch the route for a demo** without a redeploy: `az containerapp update -g <rg> -n <api app> --set-env-vars "VoiceLive__RelayEndpoint=wss://<foundry>.services.ai.azure.com/api/projects/<project>/agents/zava-voice-relay/endpoint/protocols/invocations_ws"` for hosted, or `--remove-env-vars VoiceLive__RelayEndpoint` for direct. Wait for `/healthz`.
3. **Canary a new agent version.** The platform does not allow a percentage split on one agent endpoint, so a canary is two agents and the API chooses per session (ADR 0021). First pin the stable agent so a new version does not go live by itself: `pwsh infra/hooks/canary.ps1 -Pin <version>`. Then:
   - `pwsh infra/hooks/agent-host.ps1 -Canary` builds the candidate into the `zava-voice-relay-canary` agent;
   - `pwsh infra/hooks/canary.ps1 -Canary -Percent 10 -Teams team-payments` sends 10 % of that team's new sessions to it (omit `-Teams` for every team; the route must be hosted);
   - `pwsh infra/hooks/canary.ps1 -Promote` makes the canary image a new stable version, pins it and stops the canary;
   - `pwsh infra/hooks/canary.ps1 -Rollback` is the one-step rollback (stops the canary; with no canary it pins the previous stable version);
   - `pwsh infra/hooks/canary.ps1 -Show` prints the pin and the canary settings.
   The API restarts onto a new revision for each change, so a change takes effect in about a minute. A session keeps its agent: the choice is a hash of the session id, and `hostWarmUp.route` shows `hosted-canary` for the sessions that went to the candidate.4. **Kill the host.** A standup survives it: the voice reconnects to the same hosted session and the session is read back from Cosmos. The one-off cold start after an idle stop (15 minutes) is about 4 s longer than a warm reconnect; pre-warm avoids it.
5. **Which route to use.** Direct is the default. The hosted route held the latency gate on the point estimates (ADR 0021, "G1 settled") but not with margin, so it is an option for demonstrating the hosted agent. Switch with `azd env set VOICE_ROUTE Hosted` then `azd provision`, or with the `az containerapp update` command in step 2; go back by removing the setting.
6. **Measure the hop.** `dotnet run --project tools/VoiceRelayBench -- --mode paired ...` (header comment in the file) alternates direct and relayed turns; `pwsh docs/gates/m11-p4/g1.ps1` runs the gate harness on both routes with the decision model on and off.

### Agent identity sign-ins (5B)

The hosted agent signs in as its own Entra Agent ID. Evidence, after a standup or a warm-up:

- Entra admin centre > Monitoring > Sign-in logs > **Service principal sign-ins**: filter Service principal = the agent (`<foundry>-<project>-zava-voice-relay-AgentIdentity`). The Agent column shows type `agenticAppInstance` and the blueprint as parent.
- Graph: `az rest --resource https://graph.microsoft.com --method get --url "https://graph.microsoft.com/beta/auditLogs/signIns?`$filter=signInEventTypes/any(t: t eq 'servicePrincipal') and servicePrincipalId eq '<AGENT_HOST_PRINCIPAL_ID>'&`$top=5"` (the filter without the event type times out).
- Log Analytics (if exported): `AADServicePrincipalSignInLogs | where ServicePrincipalId == "<AGENT_HOST_PRINCIPAL_ID>" | project TimeGenerated, ResourceDisplayName, ResultType, IPAddress`.
- `pwsh infra/hooks/agent-identity.ps1` prints the identity, its (absent) credentials, the blueprint, the sponsor and the latest sign-ins.
