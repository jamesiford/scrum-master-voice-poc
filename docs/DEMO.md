# Demo runbook

## Before the demo (5 minutes)
1. `az login --tenant fb049398-a8b8-4ade-8543-1ae7a82f4869`.
2. Open the portal (local: `dotnet run --project src\Mcm.Scrum.AppHost --launch-profile http`; Azure: the API URL printed by `azd up`).
3. **Live** tab > **Reset demo** (clears sessions, follow-ups and cards; Azure DevOps is not touched).
4. **Team** tab > check the sprint goal. **Roster** tab > **Sync**.
5. Optional: re-seed Azure DevOps with `dotnet run --project tools\SeedAdo -- --tenant fb049398-a8b8-4ade-8543-1ae7a82f4869` (idempotent).
6. Teams: the app package `appPackage/mcm-scrum-agent.zip` is sideloaded and you have messaged the bot once, so it can send you cards.

## Scripted run (Simulated Room, about 3 minutes)
1. **Sessions** > **Prepare now** > open the pre-brief (burndown, flags, questions).
2. **Start in Simulated Room**. Tick **Autopilot** in the room, then press Start, to let the demo cast play the three-person standup (Daniel Park, Maya Lin and Admin).
3. Watch the **Live** tab: floor, current speaker, timebox, parking lot, latency.
4. When the standup ends: **Follow-ups** tab. Approve one (type your name first), reject another.
5. Open Azure DevOps and show the "Approved by ..." comment on the approved item and nothing on the rejected one.
6. **Reports** tab: sprint summary, monthly report, then ask "How is attendance?" (the same answer works in Teams chat).
7. **Planning** tab: load per person, carry-over, not-ready items and suggested owners.

## Real Teams meeting
1. Create a Teams meeting in the demo tenant and copy the join link.
2. **Live** tab > pick a prepared session > paste the link > **Start in Teams**.
3. Admit "MCM Scrum Agent" from the lobby. The bridge status line shows Joining, Lobby, Connected, Live.
4. Roster members who join are matched by name (a Teams user called "System Administrator" matches the roster member "Admin"). Anyone who does not join is marked absent. The standup begins 8 seconds after the first match; use **Begin** if it does not.
5. Anything that goes wrong is shown on the Live tab as a Bridge event (invalid link, lobby denied, removed).

## If something fails
| Symptom | Fix |
|---|---|
| "Voice Live is unavailable" | `az login` again; check the Foundry deployments exist. |
| Standup is slow to answer | Normal p50 is 2 to 3 seconds; say so, and show the latency number. |
| Cards missing in Teams | Message the bot once; cards are always in **Follow-ups** > **Show card outbox**. |
| Teams join fails | Run the Simulated Room instead; it is the fallback for any live demo. |
| Approve shows a write error | Check the managed identity is in the Azure DevOps project Contributors group; use **Retry write**. |