# Documentation drift report

**Date:** November 26, 2026 (M11 final documentation checkpoint).

## Current M11 corrections

Earlier tables below record the November 22 baseline, not today's exhaustive
route/test inventory. Code wins; current implementation is AS-BUILT, acceptance
is `gates/M11-gates.md`, decisions are indexed in `adr/README.md`.

| Drift | Current evidence/correction |
| --- | --- |
| Payments-only architecture | Team registry/membership/routing/capacity are implemented; Payments is a demo seed, not a global runtime team |
| Hosted agents absent or orchestration hosted | Optional stateless relay with Agent ID exists; gateway/ceremony/channel handlers remain in API. Stable 4 retained; candidate 5 failed readiness/424, n=0 completed paired samples |
| Four default model deployments | `foundry.bicep` defaults to chat + one realtime + decision: three; rejected models were removed |
| Cards globally broadcast | `CardRouting.Select` uses owning-team bindings/purposes; unbound teams stay in outbox |
| Managed identity fixes approval writes | `IWorkItemWriter` uses the recorded approver's OBO context; that person's project access/consent is required |
| Typed approval names | Portal **Deciding as** comes from validated Entra identity; display names/request-body names never grant authority |
| Calendar app/secret workaround | Greenfield hook uses existing managed identity and mailbox-scoped Exchange RBAC; no new calendar app or mcm change |
| Native media equals saved-follow-up demo | Native hosting/renderer verified, Teams joins/attribution/board/events open; native meeting-derived persistence is prohibited |
| Copilot delivered end to end | Five skills and 1.1.0 package implemented; catalogue, real-user SSO/streaming/negative G5 are still open |
| Knowledge/Work IQ delivered | 8A/8B integrations unbuilt; exact existing consent/source/billing gates retained; G10/G11 not_run n=0 |
| Quality/telemetry wholly deferred | API/bridge content-free workbook verified (211 hop samples); G7 dates75% n8 fails, employee-content quality remains off, real-model CI variables empty |
| Earlier G8 closure proves final runtime | Old smaller-footprint proof is historical; final zavagf attempt failed preflight, both full-solution cycles n=0 |
| Failed native preflight means quota is exhausted | Actual D4s_v5 is NotAvailableForSubscription in eastus2; regional/DSv5 quota used0/limit100. Empty-family preflight message misdiagnoses SKU availability |
| Film features already covered by audit API/metrics | Dedicated audit page, Live per-hop panel/all-participant names and atomic roster/cast/Sprint 43 reset remain follow-ons |

This documentation step changes no runtime, Domain rules, golden expectations
or gate thresholds. ADR 0029 records the deployment acceptance boundary and
the exact preflight defect/gates; full suites run at the following checkpoint.

Generated while reconciling the documents with `main` at `36d25ad`. **The code wins** over every document (see ADR 0009). Paths are relative to the repository root. "Test" means a test that exercises the use case directly; a domain-only test is noted in the cell.

## A. Use cases (SPEC 6.1.4 and 6.1.5)

### A1. Use cases that exist in code

| Use case | Exists | Application file | Exposed by | Test |
|---|---|---|---|---|
| `UpdateTeamSettingsCommand` | yes | `src/Zava.Scrum.Application/Features/Teams/UpdateTeamSettings/UpdateTeamSettingsCommand.cs` | PUT /api/teams/{id} | No |
| `SyncRosterCommand` | yes | `src/Zava.Scrum.Application/Features/Teams/SyncRoster/SyncRosterCommand.cs` | POST /api/teams/{id}/members/sync | Yes: SyncRosterHandlerTests.cs |
| `UpdateScheduleCommand` | yes | `src/Zava.Scrum.Application/Features/Schedules/UpdateSchedule/UpdateScheduleCommand.cs` | PUT /api/teams/{teamId}/schedule | No (ScheduleEvaluator is tested in Domain.Tests) |
| `UpdatePromptCommand` | yes | `src/Zava.Scrum.Application/Features/Prompts/UpdatePrompt/UpdatePromptCommand.cs` | PUT /api/teams/{teamId}/prompts/{name} | No (FilePromptStoreTests.cs covers the store, not the command) |
| `GetTeamQuery` | yes | `src/Zava.Scrum.Application/Features/Teams/GetTeam/GetTeamQuery.cs` | GET /api/teams/{id} | No |
| `GetRosterQuery` | yes | `src/Zava.Scrum.Application/Features/Teams/GetRoster/GetRosterQuery.cs` | GET /api/teams/{id}/members | No |
| `GetScheduleQuery` | yes | `src/Zava.Scrum.Application/Features/Schedules/GetSchedule/GetScheduleQuery.cs` | GET /api/teams/{teamId}/schedule | No |
| `GetPromptQuery` | yes | `src/Zava.Scrum.Application/Features/Prompts/GetPrompt/GetPromptQuery.cs` | GET /api/teams/{teamId}/prompts/{name} | No |
| `ListPromptsQuery` | yes | `src/Zava.Scrum.Application/Features/Prompts/ListPrompts/ListPromptsQuery.cs` | GET /api/teams/{teamId}/prompts | No |
| `PrepareStandupCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/PrepareStandup/PrepareStandupCommand.cs` | POST /api/sessions/prepare; RunScheduleTickHandler | Yes: PrepareStandupHandlerTests.cs |
| `StartStandupCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/StartStandup/StartStandupCommand.cs` | POST /api/sessions/{id}/start; RunScheduleTickHandler | Yes: StandupFlowTests.cs |
| `JoinMeetingCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/JoinMeeting/JoinMeetingCommand.cs` | POST /api/sessions/{id}/start (mode acs); RunScheduleTickHandler | No |
| `ControlStandupCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/ControlStandup/ControlStandupCommand.cs` | POST /api/sessions/{id}/control | No |
| `RecordStandupEventCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/RecordStandupEvent/RecordStandupEventCommand.cs` | Voice gateway; tool record_event | Yes: StandupFlowTests.cs, EndStandupFollowUpsTests.cs |
| `RetractStandupEventCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/RetractStandupEvent/RetractStandupEventCommand.cs` | Voice gateway (corrections) | No (the domain method StandupSession.Retract is tested in StandupAgendaTests.cs) |
| `MarkAbsentCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/MarkAbsent/MarkAbsentCommand.cs` | Voice gateway; tool mark_absent | Yes: StandupFlowTests.cs |
| `ParkTopicCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/ParkTopic/ParkTopicCommand.cs` | Voice gateway; tool park_topic | Yes: StandupFlowTests.cs |
| `EndStandupCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/EndStandup/EndStandupCommand.cs` | Voice gateway; tool end_standup | Yes: EndStandupFollowUpsTests.cs |
| `GetNextSpeakerQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetNextSpeaker/GetNextSpeakerQuery.cs` | Voice gateway; tool get_next_speaker | Yes: StandupFlowTests.cs |
| `GetMemberBriefQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetMemberBrief/GetMemberBriefQuery.cs` | Tool get_member_brief | No |
| `ReconcileUpdateQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/ReconcileUpdate/ReconcileUpdateQuery.cs` | Voice gateway; tool reconcile_update | Yes: StandupFlowTests.cs |
| `GetTimeRemainingQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetTimeRemaining/GetTimeRemainingQuery.cs` | Tool get_time_remaining | Yes: StandupFlowTests.cs |
| `GetWorkItemDetailsQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetWorkItemDetails/GetWorkItemDetailsQuery.cs` | Tool get_work_item_details | No |
| `ListSessionsQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/ListSessions/ListSessionsQuery.cs` | GET /api/sessions?teamId= | No |
| `GetSessionQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetSession/GetSessionQuery.cs` | GET /api/sessions/{id}; voice gateway | No |
| `GetSessionEventsQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetSessionEvents/GetSessionEventsQuery.cs` | GET /api/sessions/{id}/events | No |
| `GetLiveStateQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetLiveState/GetLiveStateQuery.cs` | GET /api/sessions/{id}/live (SSE) | No |
| `RunScheduleTickCommand` | yes | `src/Zava.Scrum.Application/Features/Schedules/RunScheduleTick/RunScheduleTickCommand.cs` | ScheduleWorker (background service) | No (ScheduleEvaluator is tested in Domain.Tests) |
| `ProposeAdoChangeCommand` | yes | `src/Zava.Scrum.Application/Features/FollowUps/ProposeAdoChange/ProposeAdoChangeCommand.cs` | POST /api/followups/propose | No |
| `SendFollowUpsCommand` | yes | `src/Zava.Scrum.Application/Features/FollowUps/SendFollowUps/SendFollowUpsCommand.cs` | POST /api/sessions/{id}/followups; SendFollowUpsOnStandupEnded | Yes, indirectly: EndStandupFollowUpsTests.cs |
| `ApproveFollowUpCommand` | yes | `src/Zava.Scrum.Application/Features/FollowUps/ApproveFollowUp/ApproveFollowUpCommand.cs` | POST /api/followups/{id}/approve; Teams card action approve | Yes: ApplyApprovedChangesTests.cs |
| `RejectFollowUpCommand` | yes | `src/Zava.Scrum.Application/Features/FollowUps/RejectFollowUp/RejectFollowUpCommand.cs` | POST /api/followups/{id}/reject; Teams card action reject | Yes: ApplyApprovedChangesTests.cs |
| `ApplyApprovedChangesCommand` | yes | `src/Zava.Scrum.Application/Features/FollowUps/ApplyApprovedChanges/ApplyApprovedChangesCommand.cs` | POST /api/followups/{id}/apply; ApplyOnFollowUpApproved | Yes: ApplyApprovedChangesTests.cs |
| `ListFollowUpsQuery` | yes | `src/Zava.Scrum.Application/Features/FollowUps/ListFollowUps/ListFollowUpsQuery.cs` | GET /api/followups?teamId=&status= | No |
| `GetFollowUpQuery` | yes | `src/Zava.Scrum.Application/Features/FollowUps/GetFollowUp/GetFollowUpQuery.cs` | GET /api/followups/{id} | No |
| `ListEmailsQuery` | yes | `src/Zava.Scrum.Application/Features/FollowUps/ListEmails/ListEmailsQuery.cs` | GET /api/emails | No |
| `GetKpisQuery` | yes | `src/Zava.Scrum.Application/Features/Reports/GetKpis/GetKpisQuery.cs` | GET /api/reports/kpis | No (KpiCalculator is tested in Domain.Tests) |
| `GetSprintReportQuery` | yes | `src/Zava.Scrum.Application/Features/Reports/GetSprintReport/GetSprintReportQuery.cs` | GET /api/reports/sprint | Yes: ReportTests.cs |
| `GetMonthlyReportQuery` | yes | `src/Zava.Scrum.Application/Features/Reports/GetMonthlyReport/GetMonthlyReportQuery.cs` | GET /api/reports/monthly | No |
| `AnswerTeamQuestionQuery` | yes | `src/Zava.Scrum.Application/Features/Reports/AnswerTeamQuestion/AnswerTeamQuestionQuery.cs` | POST /api/ask; Teams chat (ScrumAgent) | Yes: ReportTests.cs |
| `GetPlanningPrepQuery` | yes | `src/Zava.Scrum.Application/Features/Planning/GetPlanningPrep/GetPlanningPrepQuery.cs` | GET /api/planning | No (PlanningPlanner is tested in Domain.Tests) |
| `GetRetrospectiveQuery` | yes | `src/Zava.Scrum.Application/Features/Retrospectives/GetRetrospective/GetRetrospectiveQuery.cs` | GET /api/retrospective | No |
| `DraftUserStoryQuery` | yes | `src/Zava.Scrum.Application/Features/Stories/DraftUserStory/DraftUserStoryQuery.cs` | POST /api/stories/draft | Yes: DraftUserStoryTests.cs |
| `ListParkedTopicsQuery` | yes | `src/Zava.Scrum.Application/Features/Stories/ListParkedTopics/ListParkedTopicsQuery.cs` | GET /api/stories/topics | No |
| `GetModelComparisonQuery` | yes | `src/Zava.Scrum.Application/Features/Models/GetModelComparison/GetModelComparisonQuery.cs` | GET /api/models | No (ModelCostEstimator is tested in Domain.Tests) |
| `ResetDemoCommand` | yes | `src/Zava.Scrum.Application/Features/Demo/ResetDemo/ResetDemoCommand.cs` | POST /api/demo/reset | No |
| `GetDemoStatusQuery` | yes | `src/Zava.Scrum.Application/Features/Demo/GetDemoStatus/GetDemoStatusQuery.cs` | GET /api/demo/status?teamId= | No |
| `GetHealthQuery` | yes | `src/Zava.Scrum.Application/Features/Health/GetHealth/GetHealthQuery.cs` | GET /api/health | Yes: GetHealthHandlerTests.cs |
| `CompleteSpeakerTurnCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/CompleteSpeakerTurn/CompleteSpeakerTurnCommand.cs` | Voice gateway | No |
| `RecordAgentLatencyCommand` | yes | `src/Zava.Scrum.Application/Features/Standups/RecordAgentLatency/RecordAgentLatencyCommand.cs` | Voice gateway | No |
| `GetFacilitatorInstructionsQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetFacilitatorInstructions/GetFacilitatorInstructionsQuery.cs` | Voice gateway (session setup) | No |
| `GetTurnInstructionQuery` | yes | `src/Zava.Scrum.Application/Features/Standups/GetTurnInstruction/GetTurnInstructionQuery.cs` | Voice gateway | Yes: StandupFlowTests.cs |
| `ListCardsQuery` | yes | `src/Zava.Scrum.Application/Features/Teams/ListCards/ListCardsQuery.cs` | GET /api/cards | No |
| `PreparePlanningCommand` (M9a) | yes | `src/Zava.Scrum.Application/Features/Planning/PreparePlanning/PreparePlanningCommand.cs` | POST /api/planning/prepare; Teams chat "prepare sprint planning" | Yes: PlanningFlowTests.cs |
| `GetPlanningPackQuery` (M9a) | yes | `src/Zava.Scrum.Application/Features/Planning/GetPlanningPack/GetPlanningPackQuery.cs` | GET /api/planning/pack | Yes: PlanningFlowTests.cs |
| `ApprovePlanCommand` (M9a) | yes | `src/Zava.Scrum.Application/Features/Planning/ApprovePlan/ApprovePlanCommand.cs` | POST /api/planning/packs/{id}/approve; Teams card action approve-plan | Yes: PlanningFlowTests.cs |
| `RejectPlanCommand` (M9a) | yes | `src/Zava.Scrum.Application/Features/Planning/RejectPlan/RejectPlanCommand.cs` | POST /api/planning/packs/{id}/reject; Teams card action reject-plan | Yes: PlanningFlowTests.cs |
| `UpdateMemberAvailabilityCommand` (M9a) | yes | `src/Zava.Scrum.Application/Features/Teams/UpdateMemberAvailability/UpdateMemberAvailabilityCommand.cs` | PUT /api/teams/{id}/members/{memberId}/availability | Yes: PlanningFlowTests.cs |
| `PrepareRetrospectiveCommand` (M9b) | yes | `src/Zava.Scrum.Application/Features/Retrospectives/PrepareRetrospective/PrepareRetrospectiveCommand.cs` | POST /api/retrospective/prepare | Yes: RetrospectiveFlowTests.cs |
| `ReconcileRetroUpdateQuery` (M9b) | yes | `src/Zava.Scrum.Application/Features/Retrospectives/ReconcileRetroUpdate/ReconcileRetroUpdateQuery.cs` | Voice gateway (not an endpoint) | Yes: RetrospectiveFlowTests.cs |
| `NameRetroThemesCommand` (M9b) | yes | `src/Zava.Scrum.Application/Features/Retrospectives/NameRetroThemes/NameRetroThemesCommand.cs` | Voice gateway (not an endpoint) | Yes: RetrospectiveFlowTests.cs |
| `CreateRetroOutcomeCommand` (M9b) | yes | `src/Zava.Scrum.Application/Features/Retrospectives/CreateRetroOutcome/CreateRetroOutcomeCommand.cs` | `RetrospectiveEnded` -> `FinishOnRetrospectiveEnded` | Yes: RetrospectiveFlowTests.cs |
| `GetRetroBoardQuery` (M9b) | yes | `src/Zava.Scrum.Application/Features/Retrospectives/GetRetroBoard/GetRetroBoardQuery.cs` | Voice gateway board push (not an endpoint) | Yes: RetrospectiveFlowTests.cs |
| `GetSprintCalendarQuery` (M9d) | yes | `src/Zava.Scrum.Application/Features/Calendars/GetSprintCalendar/GetSprintCalendarQuery.cs` | GET /api/calendar; GET /api/calendar/{iteration}.ics | Yes: SprintCalendarFlowTests.cs |
| `ListSprintCalendarsQuery` (M9d) | yes | `src/Zava.Scrum.Application/Features/Calendars/ListSprintCalendars/ListSprintCalendarsQuery.cs` | GET /api/calendar/proposals | No (a projection of the repository) |
| `ProposeSprintCalendarCommand` (M9d) | yes | `src/Zava.Scrum.Application/Features/Calendars/ProposeSprintCalendar/ProposeSprintCalendarCommand.cs` | POST /api/calendar/propose | Yes: SprintCalendarFlowTests.cs |
| `ApproveSprintCalendarCommand` (M9d) | yes | `src/Zava.Scrum.Application/Features/Calendars/ApproveSprintCalendar/ApproveSprintCalendarCommand.cs` | POST /api/calendar/proposals/{id}/approve; Teams card action approve-calendar | Yes: SprintCalendarFlowTests.cs |
| `RejectSprintCalendarCommand` (M9d) | yes | `src/Zava.Scrum.Application/Features/Calendars/RejectSprintCalendar/RejectSprintCalendarCommand.cs` | POST /api/calendar/proposals/{id}/reject; Teams card action reject-calendar | Yes: SprintCalendarFlowTests.cs |
| `CreateCeremonyPackCommand` (M9) | yes | `src/Zava.Scrum.Application/Features/Ceremonies/CreateCeremonyPack/CreateCeremonyPackCommand.cs` | POST /api/ceremonies/{kind}/pack | Yes: CreateCeremonyPackTests.cs |
| `ResolveUnblockerCommand` (M9) | yes | `src/Zava.Scrum.Application/Features/Standups/ResolveUnblocker/ResolveUnblockerCommand.cs` | Voice gateway (answer to "Who can help unblock this?") | Yes: UnblockAndRecapTests.cs |
| `SetRecapChannelCommand` (M9) | yes | `src/Zava.Scrum.Application/Features/Teams/SetRecapChannel/SetRecapChannelCommand.cs` | PUT /api/teams/{id}/recap-channel; Teams bot when added in a channel | Yes: UnblockAndRecapTests.cs |

### A2. Names the old SPEC listed that do not exist

| Name in the old SPEC | Exists | What the code does instead |
|---|---|---|
| `GetPreBriefQuery` | no | The pre-brief is part of `GetSessionQuery` (`SessionDto`). |
| `EscalateBlockerCommand`, `BlockerEscalated`, `NotifyScrumMasterHandler` | no | Escalation is folded into `SendFollowUpsCommand` (ADR 0004). |
| Tools `propose_ado_change`, `escalate_blocker`, `get_kpis` | no | `ScrumTools` has 9 tools; `ProposeAdoChangeCommand` is reachable only via `POST /api/followups/propose`. |
| `proposedAdoChanges` in the reconcile result (SPEC 10.2) | no | `ReconcileSuggestion` carries `Retracts` instead. |

### A3. Event handlers (notifications)

| Handler | Reacts to | Test |
|---|---|---|
| `SendPreBriefCardOnStandupPrepared` | standup prepared | No |
| `PostRecapOnStandupEnded` | standup ended | Indirectly (`EndStandupFollowUpsTests.cs` asserts the recap card) |
| `SendFollowUpsOnStandupEnded` | standup ended | Indirectly (`EndStandupFollowUpsTests.cs`) |
| `LeaveMeetingOnStandupEnded` | standup ended | No |
| `ApplyOnFollowUpApproved` | follow-up approved | Yes (`ApplyApprovedChangesTests.cs`) |
| `UpdateFollowUpCardHandler` | approved, rejected, applied | No |

## B. Routes (SPEC 14)

Every row of the SPEC 14 table was rewritten in this PR from `src/Zava.Scrum.Api/Endpoints/*.cs` and `Program.cs`. Rows that were missing from the old SPEC 14 are marked **new**.

| Method | Route | Endpoint file | Sends |
|---|---|---|---|
| GET | `/api/teams/{id}` | `TeamsEndpoints.cs` | `GetTeamQuery` |
| PUT | `/api/teams/{id}` | `TeamsEndpoints.cs` | `UpdateTeamSettingsCommand` |
| GET | `/api/teams/{id}/members` | `TeamsEndpoints.cs` | `GetRosterQuery` |
| POST | `/api/teams/{id}/members/sync` | `TeamsEndpoints.cs` | `SyncRosterCommand` |
| GET, PUT | `/api/teams/{teamId}/schedule` | `ScheduleEndpoints.cs` | `GetScheduleQuery`, `UpdateScheduleCommand` |
| GET | `/api/teams/{teamId}/prompts` (**new**) | `ScheduleEndpoints.cs` | `ListPromptsQuery` |
| GET, PUT | `/api/teams/{teamId}/prompts/{name}` | `ScheduleEndpoints.cs` | `GetPromptQuery`, `UpdatePromptCommand` |
| PUT | `/api/teams/{id}/recap-channel` (**new, M9**) | `TeamsEndpoints.cs` | `SetRecapChannelCommand` |
| POST | `/api/ceremonies/{kind}/pack` (**new, M9**; review and retrospective only) | `CeremoniesEndpoints.cs` | `CreateCeremonyPackCommand` |
| POST | `/api/retrospective/prepare` (**new, M9b**) | `InsightsEndpoints.cs` | `PrepareRetrospectiveCommand` |
| GET | `/api/calendar`, `/api/calendar/{iteration}.ics` (**new, M9d**; the brief said the `.ics` route already existed, it did not) | `CalendarEndpoints.cs` | `GetSprintCalendarQuery` |
| GET, POST | `/api/calendar/proposals`, `/api/calendar/propose`, `/api/calendar/proposals/{id}/approve`, `/reject` (**new, M9d**) | `CalendarEndpoints.cs` | `ListSprintCalendarsQuery`, `ProposeSprintCalendarCommand`, `ApproveSprintCalendarCommand`, `RejectSprintCalendarCommand` |
| POST | `/api/planning/prepare` (**new, M9a**) | `PlanningEndpoints.cs` | `PreparePlanningCommand` |
| GET | `/api/planning/pack` (**new, M9a**) | `PlanningEndpoints.cs` | `GetPlanningPackQuery` |
| POST | `/api/planning/packs/{id}/approve`, `/reject` (**new, M9a**) | `PlanningEndpoints.cs` | `ApprovePlanCommand`, `RejectPlanCommand` |
| PUT | `/api/teams/{id}/members/{memberId}/availability` (**new, M9a**) | `TeamsEndpoints.cs` | `UpdateMemberAvailabilityCommand` |
| POST | `/api/sessions/prepare` | `SessionsEndpoints.cs` | `PrepareStandupCommand` |
| GET | `/api/sessions?teamId=` | `SessionsEndpoints.cs` | `ListSessionsQuery` |
| GET | `/api/sessions/{id}` | `SessionsEndpoints.cs` | `GetSessionQuery` |
| GET | `/api/sessions/{id}/events` | `SessionsEndpoints.cs` | `GetSessionEventsQuery` |
| POST | `/api/sessions/{id}/start` | `SessionsEndpoints.cs` | `StartStandupCommand`, then `JoinMeetingCommand` for `mode: acs` |
| POST | `/api/sessions/{id}/control` | `SessionsEndpoints.cs` | `ControlStandupCommand` (begin, mute, unmute, skip, takeover, release, end) |
| GET | `/api/sessions/{id}/live` | `SessionsEndpoints.cs` | `GetLiveStateQuery` (SSE) |
| POST | `/api/sessions/{id}/followups` (**new**) | `FollowUpsEndpoints.cs` | `SendFollowUpsCommand` |
| GET | `/api/followups?teamId=&status=` | `FollowUpsEndpoints.cs` | `ListFollowUpsQuery` |
| GET | `/api/followups/{id}` (**new**) | `FollowUpsEndpoints.cs` | `GetFollowUpQuery` |
| POST | `/api/followups/propose` (**new**) | `FollowUpsEndpoints.cs` | `ProposeAdoChangeCommand` |
| POST | `/api/followups/{id}/approve`, `/reject` | `FollowUpsEndpoints.cs` | `ApproveFollowUpCommand`, `RejectFollowUpCommand` |
| POST | `/api/followups/{id}/apply` (**new**) | `FollowUpsEndpoints.cs` | `ApplyApprovedChangesCommand` |
| GET | `/api/cards?sessionId=` (**new**) | `FollowUpsEndpoints.cs` | `ListCardsQuery` |
| GET | `/api/reports/kpis`, `/sprint`, `/monthly` | `ReportsEndpoints.cs` | `GetKpisQuery`, `GetSprintReportQuery`, `GetMonthlyReportQuery` |
| POST | `/api/ask` | `ReportsEndpoints.cs` | `AnswerTeamQuestionQuery` |
| GET | `/api/planning` | `PlanningEndpoints.cs` | `GetPlanningPrepQuery` |
| GET | `/api/retrospective` | `InsightsEndpoints.cs` | `GetRetrospectiveQuery` |
| GET, POST | `/api/stories/topics`, `/api/stories/draft` | `InsightsEndpoints.cs` | `ListParkedTopicsQuery`, `DraftUserStoryQuery` |
| GET | `/api/models`, `/api/emails` | `InsightsEndpoints.cs` | `GetModelComparisonQuery`, `ListEmailsQuery` |
| POST | `/api/demo/reset` | `DemoEndpoints.cs` | `ResetDemoCommand` |
| GET | `/api/demo/status?teamId=` (**new**) | `DemoEndpoints.cs` | `GetDemoStatusQuery` |
| GET | `/api/health` (**new**) | `HealthEndpoints.cs` | `GetHealthQuery` |
| POST | `/api/messages` | `Program.cs` | Bot endpoint (`ScrumAgent`) |
| WS | `/ws/bridge/{sessionId}?token=` | `BridgeEndpoints.cs` | `VoiceGateway` (no mediator on the audio path) |
| GET | `/healthz` | `Program.cs` | Health probe |

Routes in the old SPEC 14 that **do not exist in code**: `PUT /api/teams/{id}/members`; a `next` control action.

## C. Other drift found and corrected

| Area | Document said | Code says |
|---|---|---|
| Roster | Five people, several real assignees (SPEC 1, 2, M2, 9.2) | Three members (Daniel Park, Maya Lin, Admin); `demo-owner-*` tags (ADR 0002) |
| Card actions | Approve, Edit, Reject (SPEC 2, 12; `ITeamsMessenger.cs` doc comment) | Approve and Reject only |
| Writes | General change proposals (SPEC 9.4) | `ChangeOp` supports Comment, State and Tag; `FollowUpPlanner` proposes comments only |
| Recap and pre-brief | Routed to meeting chat and channel | `TeamsBotMessenger` sends to every stored conversation (ADR 0005); `// PROD:` route to meeting chat, channel and Scrum Master |
| Microsoft 365 Copilot | Copilot Q&A (SPEC 2 scene 10, 12, M6) | M11 8 implements five membership/OBO read-only skills and custom-engine manifest 1.1.0 (ADR 0025); live G5 remains gated by catalogue upload and consenting users |
| Shared board | "Verified" (SPEC 16) | Not confirmed in a live meeting; AS-BUILT wording kept |
| Agents stack | Microsoft Agent Framework, `Azure.AI.Projects` | Neither is referenced; Foundry chat over `HttpClient` with `DefaultAzureCredential` (ADR 0002) |
| Portal stack | Fluent UI v9, MSAL.js | Tailwind CSS v4, lucide-react, Container Apps Easy Auth |
| Ports | `IFollowUpAgent` | `IReconcileAgent`, `IDraftingAgent`, `IBacklogRiskAgent`, `IReportingAgent` |
| Required tests | Repository contract tests on JSON and Cosmos; OpenTelemetry | Contract test runs against the JSON-file store only; no Cosmos repository class; no OpenTelemetry code (now listed as known gaps) |
| Latency N5 | Target 1 s | Target 1 s, measured 3.7 s p50 in the 23 Nov Teams take |
| Config keys | Several keys listed | SPEC 17 rewritten from the options classes; unused keys listed there as removed |
| `knowledge/working-agreements.md` | Used by the agent | Not read by any code |
| M9a (after M9) | M9 documented a sprint planning pack through `POST /api/ceremonies/SprintPlanning/pack` | That route now refuses `SprintPlanning`; planning has its own pack, commands and routes (SPEC 10.6, ADR 0011) |
| Agent runtime and telemetry (after this reconciliation) | Foundry chat over `HttpClient`; no OpenTelemetry (ADR 0002, 0009) | Microsoft Agent Framework chat-client agents over `Azure.AI.Projects` 2.x and basic OpenTelemetry (ADR 0009b); SPEC 5, 17 and AS-BUILT 8 updated |
| M9 (after this reconciliation) | The documents described the daily scrum, comment-only writes, outbox email, DM-broadcast recap | `CeremonyKind`, six `ChangeOp`s, ceremony packs, Graph email option, unblock loop, commitment carry-over, `AtRiskRule` and recap routing were added; SPEC 6.1.4, 9.4, 10.3, 10.5, 12, 14, 15, 16, 17, AS-BUILT 11, DEMO and ADR 0010 describe them. Not exercised in a live meeting |

## D. Open questions

1. docs/CEREMONIES.md and docs/REQUIREMENTS-AUDIT.md are now in place (renamed from the untracked catalogue and audit files). The audit's requirement statuses were re-checked against the M9 code on 22 November 2026 (section 1.1 and the R5, R6, R10 to R12, R14, R15, R17, R18, R20, R21, R25 rows); the CEREMONIES statuses were not re-verified.
2. Real identifiers still in tracked files outside the scope of item 7: `infra/main.bicep` parameter defaults (resource names), `appPackage/manifest.json` (bot id), `src/meeting-bridge/src/config.ts` (default `SPEECH_ENDPOINT`), `tools/SeedAdo/Program.cs` (default org) and a doc comment in `AdoOptions.cs`. Should these be placeholders too?
3. Deployment: `Foundry__ProjectEndpoint`, `Ado__Org` and the Connections tenant settings now come from Bicep parameters `foundryProjectName` and `adoOrgUrl` (azd env `FOUNDRY_PROJECT_NAME`, `ADO_ORG_URL`). `azd provision` must run before the next `azd deploy`. Not deployed in this PR.
4. Shared board in a live Teams meeting: seen working on 23 November 2026 (the recorded take).
5. Voice latency is above the 1 s target.
6. Teams admin-center install of the app failed; sideloading is the workaround.
