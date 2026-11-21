# 0006. M4 to M6 decisions: Teams bridge, scheduler, reports, Cosmos

## Context
M4 (real Teams meeting), M5 (schedule, prompts, live console, demo reset), M6 (KPIs, reports, Q&A) and the Cosmos store were built after M3.

## Decision
- **Teams bridge (M4):** the agent joins as an ACS external user from a headless Chromium page (`meeting-bridge/src/web/teams.ts`), served by the bridge. Meeting audio goes to the gateway as PCM16 24 kHz frames; agent audio plays into a `MediaStreamDestination` used as the call's virtual microphone. The API starts it through `JoinMeetingCommand` -> `IMeetingBridge` (HTTP, `x-bridge-key`). The page reports lobby, connected and failure states as `callState` messages, which the gateway records as `Bridge` session events, so the portal shows them. Participants are matched to the roster by first name. Verified up to ACS rejecting a fake meeting link; a real meeting has not been tried.
- **Scheduler (M5):** `ScheduleWorker` sends `RunScheduleTickCommand` every 15 s. The rule is the pure `ScheduleEvaluator` (prepare T-10, start T-1, give up 30 min after). `RunScheduleTickHandler` is a process manager that sends existing commands through `ISender`, not handlers calling handlers.
- **Live control:** `LiveSessionRegistry` (`ILiveSessions`) holds running gateways in memory, so `ControlStandupCommand` and the SSE `/live` feed work on one API replica. PROD: route to the owning instance.
- **Reports (M6):** KPIs are pure Domain maths over `demo/kpi-history.json` (labelled illustrative). The model gets figures as ready-formatted text and writes only the narrative; if it is unavailable the report uses a code-written narrative. Azure DevOps Analytics is not read because the project has no past sprints (SPEC 9.5).
- **Cosmos:** every repository sits on `ICollectionStore` (one JSON document per collection) with file and Cosmos implementations. Class names still say `JsonFile*`. A tenant policy forces public access off on Cosmos and Storage; the resource group carries `SecurityControl=Ignore` so Cosmos is public with Entra-only auth (`disableLocalAuth`). PROD: private endpoint.
- **ACS** uses a connection string held as a Container Apps secret. PROD: Entra with a role assignment.

## Consequences
- Prompt edits made in the portal live on the container disk and are lost on restart.
- No automated Cosmos contract test exists; it was checked by running the deployed API against Cosmos.