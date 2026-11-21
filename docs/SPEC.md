# MCM AI Scrum Master Agent: PoC Build Specification

> **Purpose:** a self-contained, demo-first build spec to hand to GitHub Copilot (coding agent, Copilot app or agent mode in VS Code) to generate a working proof of concept.
>
> **Companions:** *MCM AI Scrum Master Agent - Solution Architecture* (the why and the options) and *MCM AI Scrum Master Agent - Production Specification* (how the real system is built and run).
>
> **Owner:** James Ford (Microsoft). **Version 0.3**, November 20, 2026. Status: draft for account-team review.

### What changed in v0.3

- **Engineering standards made explicit.** The PoC stays demo-first in scope but is built to production code standards: Clean Architecture (Domain, Application, Infrastructure, Api), CQRS through a **hand-rolled mediator** with pipeline behaviours, SOLID, DRY, and full XML/TSDoc documentation. See the new section 6.1 and the rewritten `copilot-instructions.md`.
- Repository layout (section 6), stack (section 5), components (section 7), milestone M0 (section 16), tests (section 19) and success criteria (section 21) updated to match. Still three deployables.

### What changed in v0.2

v0.1 mixed PoC and production concerns. v0.2 is rewritten around one question: **can an AI agent run MCM's daily standup by voice, grounded in Azure DevOps, and close the loop with follow-ups?** Specifically:

- **Demo first.** A demo storyboard (section 2) is now the north star, and the milestones are reordered so the voice standup works by M2 instead of M3, after a demo data seeder and the pre-brief.
- **Smaller footprint.** Three deployables instead of six. The orchestrator, voice gateway, bot, scheduler and Scrum tools now run in one .NET service. API Management, Service Bus, the Functions-hosted MCP server, Foundry IQ, private networking and app roles move to the production specification.
- **Lighter governance.** Evaluation CI gates, the audit container, on-behalf-of token flows and RFC 9457 error contracts are deferred. Two guardrails stay because they are business requirements or basic hygiene: no Azure DevOps write without a person's approval, and no secrets in source control.
- **Gaps closed:**
  - a demo data seeder;
  - scripted "demo cast" participants, so one presenter can run a full standup;
  - absent and late member handling;
  - a recap posted to the meeting chat and team channel;
  - a pre-brief card for the human Scrum Master;
  - the monthly three-to-six sprint report;
  - Microsoft 365 Copilot packaging;
  - a requirements traceability matrix;
  - a list of what is needed from MCM before each milestone.

---

## 0. How to use this spec with GitHub Copilot

1. Create an empty repo `mcm-scrum-agent` (GitHub or Azure Repos; the GitHub Copilot app works with Azure Repos in preview).
2. Commit this file as `docs/SPEC.md` and the companion `copilot-instructions.md` as `.github/copilot-instructions.md`.
3. Create one issue per milestone in section 16 (M0 to M8). Paste the milestone block into the issue and assign it to Copilot, or prompt agent mode:
   `Implement milestone M<n> from docs/SPEC.md. Follow .github/copilot-instructions.md. Stop when the milestone's demo check passes and summarise what you built and how to run it.`
4. Connect the Microsoft Learn MCP server (API shapes) and, optionally, the Azure DevOps Remote MCP Server (`https://mcp.dev.azure.com/{org}`) so Copilot can inspect the demo project while it codes.
5. Run each milestone's **demo check** yourself before starting the next. Order matters: M0 to M2 produce the core demo; everything after adds reach.

Anything marked **VERIFY** is an API detail that is new or in preview. Copilot must check current Microsoft Learn documentation before coding against it and leave a `// VERIFY: <url>` comment.

---

## 1. What the PoC must prove

| # | Hypothesis | Proven when |
|---|---|---|
| H1 | An agent can facilitate a multi-person standup by voice: calls on people by name, listens, keeps the timebox, recaps | A five-person standup completes in 15 minutes or less without a human driving it |
| H2 | The agent's questions are grounded in Azure DevOps and useful | Every work item the agent mentions exists in the sprint; the human Scrum Master rates the questions useful |
| H3 | The agent captures commitments, blockers and decisions accurately enough to act on | The recap and follow-ups match what was said, as judged by the human Scrum Master in the demo |
| H4 | The loop closes after the meeting | Each person gets one Teams follow-up; an approved change appears in Azure DevOps; a rejected one never does |
| H5 | The agent can do this inside a real Teams meeting | The agent joins a Teams meeting, is heard, hears participants and completes the standup |
| H6 | The experience is responsive enough for a live conversation | Median time from end of speech to agent audio is about one second for US participants; India latency is measured |

**Not proven by the PoC (production specification):** scale to ten teams, the production meeting bridge, enterprise identity and governance, formal evaluation, private networking, resilience and cost at volume.

---

## 2. Demo storyboard (north star)

Everything in this spec exists to make these scenes work. Build in the order of the milestones; each scene names the milestone that enables it.

| Scene | What the audience sees | Requirement | Milestone |
|---|---|---|---|
| 1. Morning prep | Ten minutes before standup, the human Scrum Master gets a Teams card: sprint goal, burndown status, and per person the stale, blocked, overdue and unassigned items with a suggested question | F2 | M1 (portal), M3 (Teams card) |
| 2. Standup opens | The agent greets the team, states the sprint goal and calls on the first person by name | F1 | M2 |
| 3. Grounded follow-up | A member says "still working on the payment retry story". The agent replies: "Story 4521 has been Active for three days. Still on track for Thursday?" | F1, F2 | M2 |
| 4. Blocker and parking lot | A member raises a blocker; two people start a side discussion; the agent says "Let's take that offline" and parks it | F1, F4 | M2 |
| 5. Absent member and timebox | The agent notes one member is absent and will follow up, warns at three minutes left, recaps blockers and parked topics, and closes | F1, F3 | M2 |
| 6. Recap and follow-ups | The recap appears in the meeting chat and team channel. Each person gets a 1:1 Teams card with their commitments and proposed Azure DevOps updates (Approve, Edit, Reject). The absent member is asked for their update | F3, F4 | M3 |
| 7. Approval writes back | The presenter approves a state change and a comment. Azure DevOps shows the change with "Approved by ..." in history | F3, N7 | M3 |
| 8. Real Teams meeting | Scenes 2 to 5 again, with the agent joined to a real Teams meeting | F1 | M4 |
| 9. Admin portal | Team, roster and schedule configuration, the live session console (mute, next speaker, end, "I'll take it from here"), the session timeline with transcript and events | F10 | M5 |
| 10. Ask the agent | In Teams chat and Microsoft 365 Copilot: "What is blocking sprint 42?", "Summarise yesterday's standup", "Show the last six sprints" | F7 | M6 |
| 11. Reporting | Sprint summary card in the team channel, plus a monthly report covering three to six sprints: velocity, say/do ratio, carry-over and blocker age | F7 | M6 |

**Fallback rule for every live demo:** if the Teams join fails, the same session runs in the Simulated Room (section 7.3) with no other change.

---

## 3. Requirements traceability

| ID | Requirement (from the November 13 meeting) | PoC coverage | Where |
|---|---|---|---|
| F1 | Join and host the daily standup by voice, calling on each person | **Full** | 7, 10, 11, M2, M4 |
| F2 | Check updates against Azure DevOps and ask follow-up questions | **Full** | 9, 10.2, M1, M2 |
| F3 | Follow up after the meeting by Teams chat or email | **Teams full**; email is a stretch goal | 10.3, 12, M3 |
| F4 | Capture actions and decisions and share them | **Full** | 10.1, 12, M2, M3 |
| F5 | Prepare sprint planning and review the backlog | Stretch | M8 |
| F6 | Facilitate sprint review and retrospective | Retrospective is a stretch goal; review is production only | M8 |
| F7 | KPIs and monthly report over three to six sprints | **Full** (fixture-backed history if the demo project is new) | 10.4, M6 |
| F8 | Recommend assignments and escalate risks | Escalation **full**; assignment recommendations stretch | 10.3, M3, M8 |
| F9 | Write user stories and acceptance criteria | Stretch (draft only) | M8 |
| F10 | Admin portal for teams, rosters, cadence and prompts | **Full** | 13, M5 |
| N5 | About one second voice latency | Measured and shown in the portal | 11.2 |
| N6 | English with Indian, Costa Rican and US accents | Tested with MCM voices if available | 11 |
| N7 | No Azure DevOps writes without approval | **Enforced in code** | 10.3 |
| N1 to N4 | Identity, monitoring, licensing, Workday | Production specification | n/a |

---

## 4. PoC architecture

![PoC architecture](MCM-Scrum-Agent-PoC-Architecture.png)

Three deployables:

1. **`Mcm.Scrum.Api`** (.NET 10): the orchestrator workflows, voice gateway WebSocket, Microsoft 365 Agents SDK bot endpoint, scheduler (hosted `BackgroundService`), Scrum tools (in process) and the portal API.
2. **`meeting-bridge`** (Node.js + TypeScript): the Simulated Room page and the ACS Teams interop bridge.
3. **`admin-portal`** (React static site): served by `Mcm.Scrum.Api` from `wwwroot` in Azure, or by Vite locally.

External dependencies:

- Microsoft Foundry (Voice Live, model deployments);
- Azure DevOps (REST);
- Azure Communication Services (Teams interop);
- Azure Bot Service (Teams and Microsoft 365 Copilot channel);
- Azure Cosmos DB (state);
- Azure AI Speech (demo cast voices, reused from the Foundry resource);
- Application Insights.

### PoC shortcuts and their production replacements

| Concern | PoC shortcut | Production (see Production Specification) |
|---|---|---|
| Services | One .NET service plus bridge plus portal | Separate orchestrator, voice gateway and session manager scaled independently; scheduler as Container Apps jobs |
| Agent runtime | MAF agents in process, calling Foundry model deployments | Foundry hosted agents with Entra Agent ID, published to Microsoft 365 |
| Tools | In-process C# tool classes | Scrum MCP server on Azure Functions behind API Management; Foundry Toolbox with tool policies |
| Azure DevOps access | REST with Entra token (`az login` / managed identity) or a PAT in user secrets | Agent identity with least privilege; on-behalf-of for user-initiated actions; Azure DevOps Remote MCP |
| Meeting bridge | Simulated Room plus ACS Teams interop | Decision between ACS interop and Graph media bot after the PoC; Work IQ real-time transcripts in parallel |
| Messaging | In-process channels | Service Bus and Event Grid |
| Knowledge | Working agreements as Markdown in the repo, injected into prompts | Foundry IQ knowledge base on Azure AI Search |
| Identity | Bot app registration; portal sign-in with any tenant user | App roles, Agent 365 registration, Conditional Access for agents |
| Networking | Public endpoints with Entra auth | VNet, private endpoints, Front Door WAF, egress controls |
| Quality | Human judgement in demos, a few unit tests, latency metric | Foundry evaluations, CI gates, online monitoring |
| History for KPIs | Azure DevOps Analytics if history exists, else a JSON fixture | Fabric OneLake plus Power BI semantic model |

---

## 5. Technology stack (pin these)

| Area | Choice |
|---|---|
| Backend | C# on **.NET 10**, ASP.NET Core minimal APIs; **Clean Architecture** (Domain, Application, Infrastructure, Api projects; one deployable) |
| CQRS / mediator | **Hand-rolled mediator** in the Application layer (section 6.1.3): `ICommand`, `IQuery`, `ISender`, `IPublisher`, pipeline behaviours. No third-party mediator package |
| Validation | FluentValidation, run by `ValidationBehavior` |
| Code quality | Central package management, `.editorconfig`, `TreatWarningsAsErrors`, XML docs required, `dotnet format`; ESLint + Prettier for TypeScript |
| Local orchestration | **Aspire** AppHost (runs the API, bridge and portal together with one command; **VERIFY** current Aspire version). If Aspire blocks progress, fall back to a `Makefile` or `docker compose` |
| Agents | **Microsoft Agent Framework (MAF)** for .NET, chat-client agents over Foundry model deployments via the **Foundry v2 SDK** (`Azure.AI.Projects` 2.x, Responses API) |
| Voice | **Azure Voice Live API** over WebSocket `wss://<foundry-resource>.services.ai.azure.com/voice-live/realtime`, GA API version (**VERIFY**, `2026-07-15` assumed). Use the .NET Voice Live client if available, else `ClientWebSocket` |
| Realtime model | Config-driven: `gpt-realtime` default; `GPT Live 1` and `Microsoft Realtime Flash 1` (preview) switchable for comparison (**VERIFY** model names) |
| Reasoning model | A GPT mini-class deployment for reconciliation, follow-ups and summaries (config-driven) |
| Teams and Copilot | **Microsoft 365 Agents SDK** for .NET (`Microsoft.Agents.*`), Azure Bot Service (single tenant), app package built with the Microsoft 365 Agents Toolkit |
| Azure DevOps | REST API 7.x via `HttpClient` (WIQL, work items, iterations, team members, Analytics OData) |
| Meeting bridge | **Node.js 22 + TypeScript**, Playwright headless Chromium hosting the ACS Calling Web SDK (pattern from Azure-Samples `voicelive-webapp/samples/teams-meeting-delegate`, **VERIFY**) |
| Demo cast voices | Azure AI Speech text to speech (neural voices, varied accents) |
| Admin portal | **React 19 + Vite + TypeScript + Fluent UI v9**, MSAL.js |
| State | **Azure Cosmos DB for NoSQL** (serverless) in Azure; in-memory or JSON-file repository locally |
| Hosting | **Azure Container Apps** (two apps: `api`, `bridge`) |
| Deploy | **azd** + Bicep (Azure Verified Modules where convenient) |
| Telemetry | OpenTelemetry to Application Insights (basic) |
| Tests | xUnit for Domain rules, state machine, handlers (hand-written fakes) and the mediator; NetArchTest.Rules architecture tests (**VERIFY** version; ArchUnitNET fallback); one Playwright end-to-end test of a scripted standup |

---

## 6. Repository layout

```
mcm-scrum-agent/
├─ azure.yaml                      # azd: services api, bridge
├─ infra/main.bicep                # Foundry, ACA env + 2 apps, ACR, Cosmos, ACS, Bot, App Insights, identity
├─ Directory.Build.props           # nullable, warnings as errors, analyzers, XML docs
├─ Directory.Packages.props        # central package versions
├─ .editorconfig
├─ src/
│  ├─ Mcm.Scrum.AppHost/           # Aspire: runs api, bridge, portal locally
│  ├─ Mcm.Scrum.Domain/            # no dependencies
│  │  ├─ Teams/ Members/ Standups/ FollowUps/ Reporting/   # aggregates, value objects, events
│  │  ├─ Flags/                    # IFlagRule + one class per rule
│  │  ├─ Kpis/                     # KpiCalculator (burndown, carry-over, blocker age)
│  │  ├─ Voice/FloorControl.cs     # pure state machine
│  │  └─ Common/                   # Entity, AggregateRoot, IDomainEvent, Result, Error
│  ├─ Mcm.Scrum.Application/
│  │  ├─ Abstractions/Messaging/   # hand-rolled mediator: ICommand, IQuery, ISender, IPublisher, Mediator
│  │  ├─ Abstractions/Ports/       # IWorkItemReader/Writer, ITeamsMessenger, IVoiceSessionFactory, agents, repos, IClock
│  │  ├─ Behaviors/                # LoggingBehavior, ValidationBehavior, ExceptionMappingBehavior
│  │  ├─ Features/<Area>/<UseCase>/  # Command|Query, Handler, Validator, DTO (catalogue in 6.1.4)
│  │  ├─ Tools/ScrumTools.cs       # IScrumTools: Voice Live tools -> commands/queries
│  │  └─ DependencyInjection.cs    # AddApplication()
│  ├─ Mcm.Scrum.Infrastructure/
│  │  ├─ Ado/                      # AdoRestClient, WIQL, AdoFields constants, Analytics
│  │  ├─ Agents/                   # MAF agents (BacklogRisk, FollowUp, Reporting), FilePromptStore
│  │  ├─ Voice/                    # VoiceLiveClient, session factory
│  │  ├─ Teams/                    # Agents SDK proactive messenger, Adaptive Card templates
│  │  ├─ Persistence/              # JsonFile* and Cosmos* repositories
│  │  └─ DependencyInjection.cs    # AddInfrastructure()
│  ├─ Mcm.Scrum.Api/
│  │  ├─ Endpoints/                # <Feature>Endpoints.cs, ResultExtensions.ToHttpResult()
│  │  ├─ Bot/                      # Agents SDK activity handler (adapter only)
│  │  ├─ Voice/                    # bridge WebSocket endpoint, voice gateway loop
│  │  ├─ Scheduling/               # StandupScheduler BackgroundService
│  │  └─ Program.cs                # composition root
│  ├─ tests/
│  │  ├─ Mcm.Scrum.Domain.Tests/
│  │  ├─ Mcm.Scrum.Application.Tests/      # handlers with fakes in TestDoubles/, mediator tests
│  │  ├─ Mcm.Scrum.Infrastructure.Tests/   # repository contract tests, ADO mapping tests
│  │  └─ Mcm.Scrum.ArchitectureTests/
│  ├─ meeting-bridge/               # Node/TS: src/domain, src/services, src/routes (/room, ACS bridge, cast player)
│  └─ admin-portal/                # React + Fluent UI: src/features/*, src/api (typed client), src/hooks
├─ agents/instructions/*.md        # facilitator, backlog-risk, follow-up, reporting prompts
├─ knowledge/working-agreements.md # injected into prompts in the PoC
├─ demo/
│  ├─ seed-ado.json                # demo backlog definition
│  ├─ cast/*.yaml                  # scripted participants for the Simulated Room
│  └─ kpi-history.json             # fixture for 6 past sprints if the ADO project has no history
├─ tools/SeedAdo/                  # console app: creates the demo team, sprint and work items
├─ appPackage/                     # Teams / Microsoft 365 Copilot app manifest and icons
├─ docs/SPEC.md, docs/adr/
└─ .github/copilot-instructions.md
```

### 6.1 Architecture and coding standards

The PoC is demo-first in **scope**, not in **code quality**. It is built to the same engineering standards as the production system so that the code that wins the demo can be carried forward. `.github/copilot-instructions.md` holds the full rule set; this section is the design it applies to.

#### 6.1.1 Clean Architecture layers

| Project | Holds | May reference | Must not reference |
|---|---|---|---|
| `Mcm.Scrum.Domain` | Aggregates (`Team`, `Member`, `StandupSession`, `FollowUp`, `Schedule`), value objects (`WorkItemId`, `SprintRef`, `Flag`), domain events, pure rules: `IFlagRule` implementations, `KpiCalculator`, `FloorControl` state machine, approval invariants | nothing (BCL only) | any other project; ASP.NET, Azure, HTTP, JSON, MAF, Voice Live, ADO packages |
| `Mcm.Scrum.Application` | Commands, queries, handlers, validators, DTOs; ports (`IWorkItemReader`, `IWorkItemWriter`, `ITeamsMessenger`, `IVoiceSessionFactory`, `IBacklogRiskAgent`, `IFollowUpAgent`, `IReportingAgent`, `IPromptStore`, repositories, `IClock`); `IScrumTools`; the hand-rolled mediator and pipeline behaviours; `Result`/`Error` | Domain, FluentValidation, `Microsoft.Extensions.*.Abstractions` | Infrastructure, Api, any SDK |
| `Mcm.Scrum.Infrastructure` | Adapters implementing the ports: `AdoRestClient`, `JsonFile*Repository` and `Cosmos*Repository`, `VoiceLiveClient`, MAF agents, `AgentsSdkTeamsMessenger`, `FilePromptStore`, `SystemClock`; `AddInfrastructure()` | Application, Domain, Azure/MAF/Agents SDK packages | Api |
| `Mcm.Scrum.Api` | Composition root; minimal API endpoints per feature; bot activity handler; bridge WebSocket endpoint and voice gateway loop; scheduler `BackgroundService`; SSE; `ToHttpResult()` mapping | all of the above | (business logic of any kind) |

Presentation adapters (endpoints, bot handler, WebSocket loop, scheduler, Voice Live tool dispatch) translate input into a command or query, call `ISender`, and translate the `Result` back. The audio hot path (forwarding PCM frames between the bridge and Voice Live) is the one deliberate exception: it bypasses the mediator for latency, but every *decision* it needs (floor control, tool calls) goes through Domain or a query.

#### 6.1.2 Request flow

```
Teams card / portal / scheduler / Voice Live tool call
      │  (Api adapter: map input -> command or query)
      ▼
ISender.Send(request)
      │  LoggingBehavior -> ValidationBehavior -> ExceptionMappingBehavior
      ▼
Handler (Application)  ── uses ports ──►  Infrastructure adapters (ADO, Cosmos/JSON, MAF, Agents SDK)
      │  loads aggregate, calls Domain behaviour, saves
      ▼
Domain events (e.g. FollowUpApproved)  ──►  IPublisher  ──►  INotificationHandler(s)
      │
      ▼
Result<T>  ──►  adapter maps to HTTP / card update / tool result / spoken response
```

#### 6.1.3 Hand-rolled mediator (contract)

The mediator is a small piece of our own code in `Mcm.Scrum.Application/Abstractions/Messaging`. No third-party mediator package is used. Target size: under ~200 lines plus tests.

```csharp
/// <summary>Marker for a request that changes state. Returns a <see cref="Result"/>.</summary>
public interface ICommand : IRequest<Result>;
/// <summary>Marker for a request that changes state and returns a value (usually an id).</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
/// <summary>Marker for a side-effect-free read.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
public interface IRequest<TResponse>;

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

/// <summary>Cross-cutting step wrapped around every request, in registration order.</summary>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}

public interface IDomainEvent { DateTimeOffset OccurredAt { get; } }
public interface INotificationHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken ct);
}

public interface ISender    { Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default); }
public interface IPublisher { Task Publish(IDomainEvent domainEvent, CancellationToken ct = default); }
```

Implementation rules:

- `Mediator : ISender, IPublisher` resolves `IRequestHandler<TRequest,TResponse>` and the `IPipelineBehavior<,>` chain from DI and composes them. Build the closed generic wrapper per request type once and cache it in a `ConcurrentDictionary<Type, …>`; no reflection per call after the first.
- `AddApplication()` scans the Application assembly once at startup and registers handlers (scoped), validators and notification handlers. Behaviours are registered explicitly, in order: `LoggingBehavior`, `ValidationBehavior`, `ExceptionMappingBehavior`.
- Exactly one handler per request (startup fails fast otherwise); zero or more handlers per domain event, run sequentially, each idempotent. A failing notification handler is logged and does not roll back the command (`// PROD:` outbox).
- Aggregates collect events in `DomainEvents`; repositories' `SaveAsync` is followed by `IPublisher.Publish` for each event (done in a small `DomainEventDispatcher` the handlers call, or in the repository decorator; pick one and record it in an ADR).
- Unit tests cover: dispatch to the right handler, behaviour order, validation short-circuit, exception mapping, notification fan-out, and the missing/duplicate handler startup check.

#### 6.1.4 Use-case catalogue

Workflow names used elsewhere in this spec (for example `PrepareStandup`, `ApplyApprovedChanges`) are the commands below. Keep this table in sync with the code.

| Area | Commands (change state) | Queries (read only) | Domain events -> handlers |
|---|---|---|---|
| Teams | `UpdateTeamSettingsCommand`, `SyncRosterCommand`, `UpdateScheduleCommand`, `UpdatePromptCommand` | `GetTeamQuery`, `GetRosterQuery`, `GetScheduleQuery`, `GetPromptQuery` | |
| Standups | `PrepareStandupCommand` (creates session + pre-brief), `StartStandupCommand`, `ControlStandupCommand`, `RecordStandupEventCommand`, `MarkAbsentCommand`, `ParkTopicCommand`, `EndStandupCommand` | `GetPreBriefQuery`, `GetNextSpeakerQuery`, `GetMemberBriefQuery`, `ReconcileUpdateQuery`, `GetTimeRemainingQuery`, `ListSessionsQuery`, `GetSessionQuery`, `GetSessionEventsQuery` | `StandupPrepared` -> `SendPreBriefCardHandler`; `StandupEnded` -> `PostRecapHandler`, `SendFollowUpsHandler` |
| Follow-ups | `ProposeAdoChangeCommand`, `EscalateBlockerCommand`, `SendFollowUpsCommand`, `ApproveFollowUpCommand`, `RejectFollowUpCommand`, `ApplyApprovedChangesCommand` | `ListFollowUpsQuery`, `GetFollowUpQuery` | `FollowUpApproved` -> `ApplyApprovedChangeHandler` (sends `ApplyApprovedChangesCommand`), `UpdateFollowUpCardHandler`; `BlockerEscalated` -> `NotifyScrumMasterHandler` |
| Reports and Q&A | | `GetKpisQuery`, `GetSprintReportQuery`, `GetMonthlyReportQuery`, `AnswerTeamQuestionQuery` (Teams/Copilot Q&A) | |
| Demo | `ResetDemoCommand` | `GetDemoStatusQuery` | |
| Health | | `GetHealthQuery` (M0; proves the pipeline, served at `GET /api/health`) | |

Implemented so far: M0 `GetHealthQuery`; M1 `GetTeamQuery`, `UpdateTeamSettingsCommand`, `GetRosterQuery`, `SyncRosterCommand`, `PrepareStandupCommand`, `ListSessionsQuery`, `GetSessionQuery`; M2 commands `StartStandupCommand`, `EndStandupCommand`, `RecordStandupEventCommand`, `MarkAbsentCommand`, `ParkTopicCommand`, `CompleteSpeakerTurnCommand` (added: advances the speaking order), `RecordAgentLatencyCommand` (added: stores the latency metric) and queries `GetNextSpeakerQuery`, `GetMemberBriefQuery`, `ReconcileUpdateQuery`, `GetTimeRemainingQuery`, `GetSessionEventsQuery`, `GetTurnInstructionQuery` (added: renders a turn prompt from `agents/instructions/turn-*.md`), `GetFacilitatorInstructionsQuery` (added). `GetPreBriefQuery` is not needed: the pre-brief is part of `GetSessionQuery`. `ControlStandupCommand` arrives with the portal Live console in M5; until then the Simulated Room's own buttons talk to the gateway directly.

M3b to M6 add: JoinMeetingCommand (Teams join via the bridge), ControlStandupCommand, GetLiveStateQuery, GetScheduleQuery, UpdateScheduleCommand, RunScheduleTickCommand, ListPromptsQuery, GetPromptQuery, UpdatePromptCommand, ResetDemoCommand, GetDemoStatusQuery, GetKpisQuery, GetSprintReportQuery, GetMonthlyReportQuery, AnswerTeamQuestionQuery, and domain events FollowUpRejected and FollowUpApplied (handled by UpdateFollowUpCardHandler). See ADR 0005 and 0006. M8 adds: GetPlanningPrepQuery, GetRetrospectiveQuery, DraftUserStoryQuery, ListParkedTopicsQuery, GetModelComparisonQuery, ListEmailsQuery (ADR 0007), GetWorkItemDetailsQuery (tool get_work_item_details; teammates can ask about an item's due date, state or owner). Routes: GET /api/planning, GET /api/retrospective, GET /api/stories/topics, POST /api/stories/draft, GET /api/models, GET /api/emails.

The facilitator model is offered only the read-only tools `get_member_brief` and `get_time_remaining` (ADR 0003). The other tools in 7.2 are implemented and dispatchable through `IScrumTools`, but the gateway, not the model, records events, parks topics and ends the standup.

Voice Live tool names (section 7.2) map one-to-one onto these: for example `reconcile_update` -> `ReconcileUpdateQuery`, `mark_absent` -> `MarkAbsentCommand`, `propose_ado_change` -> `ProposeAdoChangeCommand`.

M3 adds `ProposeAdoChangeCommand`, `SendFollowUpsCommand`, `ApproveFollowUpCommand`, `RejectFollowUpCommand`, `ApplyApprovedChangesCommand` and queries `ListFollowUpsQuery`, `GetFollowUpQuery`, `ListCardsQuery` (card outbox). Differences from the table above (ADR 0004): `EscalateBlockerCommand` is folded into `SendFollowUpsCommand` (escalation is a follow-up kind from `FollowUpPlanner`), so there is no `BlockerEscalated` event; the event handlers are `SendPreBriefCardOnStandupPrepared`, `PostRecapOnStandupEnded`, `SendFollowUpsOnStandupEnded` and `ApplyOnFollowUpApproved`; `UpdateFollowUpCardHandler` is deferred until the Teams bot exists.

#### 6.1.5 Code standards summary

| Standard | How it shows up in this repo | Checked by |
|---|---|---|
| Clean Architecture | Four projects, dependency rule in 6.1.1 | `Mcm.Scrum.ArchitectureTests` (NetArchTest.Rules) |
| CQRS + mediator | One folder per use case; commands return `Result`, queries return DTOs; behaviours for logging, validation and errors | Architecture tests (naming, handler visibility, no handler-to-handler calls), mediator unit tests |
| SOLID | Role-specific ports, `IFlagRule` strategies, behaviour methods on aggregates, constructor injection only | Code review, architecture tests (Domain has no outward references) |
| DRY | Single sources for config keys, prompts, ADO field names, card templates, error codes, bridge message types; extract on the third repeat | Code review; bridge protocol sync test |
| Well documented | XML docs on all Domain and Application members and public Infrastructure/Api types; TSDoc on exported TypeScript; README per project; ADRs for deviations | `GenerateDocumentationFile` + `TreatWarningsAsErrors` (CS1591), ESLint `jsdoc` rules |
| Well organised | Folder = namespace, one type per file, feature folders, central package management, `.editorconfig` | `dotnet format --verify-no-changes`, ESLint + Prettier |
| Well commented | Comments explain why, cite SPEC section or requirement id; `// PROD:`, `// VERIFY:`, `// DEMO:` markers; no bare TODOs or commented-out code | Code review; a lint rule banning `TODO` |


---

## 7. Components

### 7.1 `Mcm.Scrum.Api`
- **Use cases** are commands and queries dispatched through the hand-rolled mediator (section 6.1). The former workflow names map to them: `PrepareStandupCommand`, `StartStandupCommand`/`EndStandupCommand`, `SendFollowUpsCommand`, `ApplyApprovedChangesCommand`, `GetSprintReportQuery`, `GetMonthlyReportQuery`. Multi-step agent reasoning inside a handler may use an MAF workflow behind a port; the handler stays the unit of work.
- The bullets below are **Api-layer adapters**: they map input to a command or query and contain no business logic.
- **Voice gateway:** WebSocket `/ws/bridge/{sessionId}` (protocol in section 8), one Voice Live session per standup, floor control (section 11.3), tool dispatch to in-process tools.
- **Bot:** `/api/messages` for Teams and Microsoft 365 Copilot (section 12).
- **Scheduler:** a `BackgroundService` that checks schedules every 30 seconds. At T-10 minutes it runs `PrepareStandup` and sends the pre-brief card to the human Scrum Master. At T-1 minute it starts the session with the configured bridge.
- **Portal API:** section 14.

### 7.2 Scrum tools (in process)
`get_next_speaker`, `get_member_brief`, `reconcile_update`, `record_event`, `park_topic`, `mark_absent`, `get_time_remaining`, `end_standup`, `propose_ado_change`, `escalate_blocker`, `get_kpis`. Implement them in the Application layer behind an `IScrumTools` interface; each tool sends the matching command or query (section 6.1.4) through `ISender` and holds no logic of its own. This keeps the tools exposable as an MCP server later without changing callers.

### 7.3 `meeting-bridge`
Implements one protocol (section 8) in three modes:
- **`simulated`** (build first): `/room/{sessionId}` is a browser page. It shows the roster with avatars, the current speaker, live captions, a timebox bar and the parking lot. The presenter picks "who am I" and talks with push-to-talk or open mic. It needs no ACS and runs locally.
- **Demo cast** (part of `simulated`): `demo/cast/*.yaml` scripts the other participants. When the agent calls on a cast member, the bridge synthesises their line with Azure AI Speech and streams it as that participant. One presenter can then demo a five-person standup. Script lines can branch on keywords in the agent's question.

```yaml
# demo/cast/payments-standup.yaml
members:
  - memberId: m-011
    voice: en-IN-NeerjaNeural
    turns:
      - say: "Yesterday I finished the refund API tests. Today I'm picking up the retry story. No blockers."
      - when: ["on track", "Thursday"]
        say: "It slipped because the sandbox was down. I should finish tomorrow."
  - memberId: m-012
    voice: en-US-AndrewNeural
    absent: true
  - memberId: m-013
    voice: es-CR-MariaNeural    # English lines, Costa Rican accent profile
    turns:
      - say: "I'm blocked on bug 4602. I need database access from the platform team."
```

- **`acs`**: Playwright launches headless Chromium on an internal page using the ACS Calling Web SDK. The page joins the Teams meeting by `joinUrl` as "MCM Scrum Agent", captures incoming meeting audio and dominant-speaker events, and plays agent audio as its microphone stream (**VERIFY** the raw-audio APIs in the sample). It reports participants and lobby state.

### 7.4 `admin-portal`
Section 13.

---

## 8. Bridge protocol (bridge <-> Voice gateway)

WebSocket `wss://{api}/ws/bridge/{sessionId}?mode=acs|simulated`, authenticated with a short-lived session token returned by `POST /api/sessions/{id}/start`.

- **Binary frames:** PCM16 little-endian, 24 kHz, mono, 20 ms frames (960 bytes). Inbound is meeting audio; outbound is agent audio.
- **Text frames (JSON):**

```jsonc
{ "type": "hello", "sessionId": "...", "mode": "acs", "version": 1 }
{ "type": "participants", "items": [ { "participantId": "...", "displayName": "Priya S.", "aadObjectId": "<guid>|null" } ] }
{ "type": "dominantSpeaker", "participantId": "...", "ts": "..." }
{ "type": "localSpeaker", "memberId": "m-014" }                  // simulated room / demo cast
{ "type": "lobby", "state": "waiting|admitted|denied" }
{ "type": "agentAudioState", "state": "speaking|idle" }
{ "type": "caption", "role": "agent|participant", "memberId": "m-014|null", "text": "..." }
{ "type": "ui", "state": { "currentSpeaker": "m-014", "remainingSec": 420, "parkingLot": ["..."] } }  // gateway -> simulated room
{ "type": "control", "action": "mute|unmute|leave" }            // gateway -> bridge
```

**M2 additions to the protocol** (the Simulated Room needs them; ADR 0003): `hello` carries `standupMinutes` and `members[]`; `participants.items[]` carries `memberId` and `present`; the room sends `{ "type": "speaking", "memberId", "active" }` when a cast member starts or stops, `{ "type": "utterance", "memberId", "text", "durationMs" }` when a cast line ends (cast lines are injected as text; the presenter's real microphone audio goes through speech-to-text), `{ "type": "command", "action": "begin|takeover|release|mute|unmute|skip|end" }`, and `agentAudioState: idle` when its playback queue drains. The gateway adds `control: flush` (drop queued agent audio) and `info` / `error` messages with a plain `message` for the portal banner.

```jsonc
{ "type": "error", "code": "LOBBY_TIMEOUT|JOIN_FAILED|MEDIA_FAILED", "message": "..." }
```

**Speaker attribution:**

1. Match `aadObjectId` to `members.aadObjectId`.
2. Otherwise, fuzzy-match the display name.
3. In the Simulated Room, use `localSpeaker`.
4. In a standup, the person called on is the default attribution for speech during their turn. If someone else speaks for longer than three seconds, the agent confirms who it was.

---

## 9. Azure DevOps integration

### 9.1 Access
- Preferred: an Entra token for resource `499b84ac-1321-427f-aa17-267ca6975798/.default` via `DefaultAzureCredential`. Locally that is the developer's `az login`; in Azure it is the app's managed identity, which must be added to the Azure DevOps organisation.
- Fallback when the org is not Entra-backed or the identity cannot be added in time: a PAT (Work Items Read & Write, Analytics Read) in .NET user secrets locally or a Container Apps secret in Azure. Behind `Ado:AuthMode = Entra | Pat`.

### 9.2 Demo data seeder (`tools/SeedAdo`)
Creates, idempotently, in a sandbox project:

- a team "Payments (Demo)" with an area path;
- the current two-week iteration with a sprint goal in the iteration description;
- about 18 work items from `demo/seed-ado.json`, designed to trigger every flag.

The flag-triggering items are:

- two stories Active with `StateChangeDate` more than two days ago. Either seed two or more days before the demo, or add a tag such as `demo-age-3` that the flag rules treat as three extra days in state when `Demo:HonorAgeTags=true`;
- one bug tagged `Blocked`;
- one task past its `TargetDate`;
- two unassigned items;
- healthy items for contrast.

Assignees come from a configured list of real users in the sandbox org. The roster maps their display names to the demo cast.

### 9.3 Reads
WIQL (parameterise project, team and area; map field names through `teams.ado.fieldMap` so Agile, Scrum, CMMI and custom templates work):

```sql
SELECT [System.Id],[System.Title],[System.WorkItemType],[System.State],[System.AssignedTo],
       [Microsoft.VSTS.Common.StateChangeDate],[System.ChangedDate],[Microsoft.VSTS.Scheduling.StoryPoints],
       [Microsoft.VSTS.Scheduling.RemainingWork],[Microsoft.VSTS.Scheduling.TargetDate],[System.Tags]
FROM WorkItems
WHERE [System.TeamProject] = @project
  AND [System.IterationPath] = @CurrentIteration('[{project}]\{team}')
  AND [System.WorkItemType] IN ('User Story','Product Backlog Item','Bug','Task')
  AND [System.State] NOT IN ('Closed','Removed','Done')
```

**Flag rules (pure C#, unit tested):**

| Flag | Rule |
|---|---|
| `stale` | In progress and `StateChangeDate < today - staleDays` |
| `blocked` | Tag `Blocked`, or `Blocked = Yes` |
| `overdue` | `TargetDate < today` |
| `unassigned` | No assignee |
| `no_recent_update` | In progress and `ChangedDate < today - 1` |
| `burndown_risk` | Remaining points divided by days left is greater than 1.3 times the average daily completion so far |

### 9.4 Writes (only after approval)
`ApplyApprovedChanges` supports three operations: add a comment, change state, add or remove a tag. Each write adds a comment "Approved by {name} in Teams, proposed by MCM Scrum Agent from the {date} standup." Writes are refused unless the `followups` record is `approved`. This check lives in code, not in a prompt.

### 9.5 KPI history
`get_kpis(teamId, sprints)` reads iteration history from Azure DevOps Analytics OData (`WorkItemSnapshot` / `Iterations`) when at least three past sprints exist. Otherwise it reads `demo/kpi-history.json`, and the portal labels the data "illustrative".

---

## 10. Agents

The prompts live in `agents/instructions/*.md` and are rendered with team values. The working agreements from `knowledge/working-agreements.md` are appended to each prompt.

### 10.1 Facilitator (Voice Live, model-direct)
Tools: those in section 7.2 that a facilitator needs. Starter `facilitator.md`:

```
You are the MCM Scrum Agent, a friendly, concise junior Scrum Master running the daily standup for team {teamName}.
A human Scrum Master ({humanSmName}) is present. If they speak to you or redirect, defer to them immediately.
Flow:
1. Open in one or two sentences: greet the team and state the sprint goal "{sprintGoal}" and days left ({daysLeft}).
2. Call get_next_speaker and invite that person by first name. Cover: what they finished, what is next, any blockers.
3. Listen. Speak only when the system gives you the floor.
4. After each person call reconcile_update. If it returns a suggestedQuestion, ask it (at most one question per person),
   naming the work item number and a short title. Never invent work item numbers.
5. Record commitments, blockers and decisions with record_event.
6. If two or more people discuss one topic for more than about 60 seconds, say "Let's take that offline" and call park_topic.
7. If get_next_speaker returns an absent member, say "{name} isn't here today, I'll follow up with them" and call mark_absent.
8. When told time is nearly up, say there are about three minutes left and speed up.
9. Close with a recap of no more than three sentences: blockers, parked topics, and that follow-ups are coming in Teams.
Never change estimates, assignments or states yourself; offer to propose an update for the person to confirm.
Speak in short sentences. Do not read lists aloud. Warm, professional tone.
```

### 10.2 Backlog & Risk (MAF agent, read only)
- `PrepareStandup` builds the `PreBrief` in code (WIQL plus flag rules). The agent only writes the `suggestedQuestion` per member and a two-sentence team summary.
- `reconcile_update(memberId, utterance)` sends the member's brief and the transcribed update to the reasoning model with structured output:

```jsonc
{ "consistent": true, "mentionedWorkItemIds": [4521], "risks": ["4521 stale 3 days"],
  "suggestedQuestion": "Story 4521 has been Active for three days. Still on track for Thursday?",
  "events": [ { "type": "commitment", "text": "Finish retry story tomorrow", "workItemIds": [4521] } ],
  "proposedAdoChanges": [ { "workItemId": 4521, "op": "comment", "value": "Slipped: sandbox outage; ETA tomorrow", "rationale": "Said in standup" } ] }
```

- Any `workItemId` not in the member's brief or the sprint is dropped in code before it is returned. Pre-compute this per member while the previous person is speaking to hide latency.

**`PreBrief` contract:**

```jsonc
{ "teamId": "...", "iterationPath": "...", "sprintGoal": "...", "daysLeft": 6,
  "burndown": { "committedPoints": 40, "completedPoints": 14, "remainingPoints": 26, "atRisk": true },
  "members": [ { "memberId": "m-011", "items": [ { "id": 4521, "title": "Payment retry", "type": "User Story",
      "state": "Active", "daysInState": 3, "flags": ["stale"] } ], "suggestedQuestion": "..." } ],
  "teamRisks": ["2 items unassigned", "Bug 4602 blocked for 4 days"], "summary": "..." }
```

### 10.3 Follow-up (MAF agent)
- Input: session events and the pre-brief. Output: `followups` documents.
- Per person, send at most one message containing:
  - their commitments;
  - open questions;
  - proposed Azure DevOps changes, each with Approve, Edit and Reject buttons.
- Absent members get "We missed you at standup. Reply with your update or approve these suggested changes."
- **Escalation:** a blocker older than `escalateBlockerAfterDays` triggers a card to the human Scrum Master (and, optionally, the manager) with Acknowledge and Reassign buttons. Reassign proposes a change that still needs approval.
- Email follow-up (F3) is a stretch goal behind `Features:EmailFollowUps`.

### 10.4 Reporting (MAF agent)
- **Code computes:**
  - say/do ratio (completed divided by committed points);
  - velocity (average of the last three to six sprints);
  - carry-over count;
  - average blocker age;
  - average story cycle time;
  - standup attendance;
  - follow-up closure within 24 hours;
  - agent-run standups;
  - average standup duration.
- **The model writes:** the narrative of a sprint summary (Adaptive Card plus Markdown) and of a monthly report covering three to six sprints (Markdown and an HTML page in the portal, plus a card to the channel).
- **Q&A:** in Teams and Copilot chat, the Reporting agent answers questions using the same tools (`get_kpis`, current pre-brief, session events).

---

## 11. Voice design

### 11.1 Voice Live session (**VERIFY** every field against the current reference)

```jsonc
{ "type": "session.update",
  "session": {
    "modalities": ["text", "audio"],
    "instructions": "<rendered facilitator.md>",
    "voice": { "name": "en-US-Ava:DragonHDLatestNeural", "type": "azure-standard" },
    "input_audio_format": "pcm16", "output_audio_format": "pcm16", "input_audio_sampling_rate": 24000,
    "input_audio_noise_reduction": { "type": "azure_deep_noise_suppression" },
    "input_audio_echo_cancellation": { "type": "server_echo_cancellation" },
    "input_audio_transcription": { "model": "azure-speech", "language": "en-US" },
    "turn_detection": { "type": "azure_semantic_vad", "silence_duration_ms": 600, "remove_filler_words": true,
                        "create_response": false, "interrupt_response": true },
    "tools": [ /* section 7.2 */ ], "tool_choice": "auto" } }
```

`create_response: false` lets the gateway, not the VAD, decide when the agent speaks. If the service ignores it, cancel unsolicited responses with `response.cancel` when the agent does not hold the floor. Use semantic VAD v2 if it is exposed as an option.

### 11.2 Latency
The gateway measures end of speech (`input_audio_buffer.speech_stopped`) to the first agent audio byte for every agent turn. It stores p50 and p95 per session and shows them live in the portal. Target p50 is about 1,000 ms for US participants. Pre-computing `reconcile_update` (section 10.2) is the main lever.

### 11.3 Floor control (state machine in the gateway)

```
Opening -> CallingOn(m) -> Listening(m) -> Reconciling(m) -> [Clarifying(m) -> Listening(m)] -> Next
        -> ... -> Recap -> Closed
Any state -> HumanOverride (SM says "I'll take it from here" or portal button) -> Listening only
Any state -> Paused (portal mute)
```

- The agent speaks only in `Opening`, `CallingOn`, `Clarifying`, `Recap`, when addressed by name ("Scrum Agent, ..."), or on a portal command.
- `Listening(m)` -> `Reconciling(m)` after the floor holder stops and 1.2 seconds of silence pass. This is configurable; Indian and Costa Rican teams may need longer.
- **Cross-talk:** speech from anyone other than the floor holder is recorded but does not trigger a response. More than 60 seconds of multi-speaker discussion triggers the parking-lot line at the next pause.
- **Barge-in:** if the floor holder or the human Scrum Master speaks while the agent is talking, the gateway sends `response.cancel` and truncates the audio.
- **Timebox:** at 80 % of `standupMinutes` the agent warns; at 100 % the gateway moves to `Recap`.
- **Late joiner:** if an absent member appears in `participants` before `Recap`, they go to the end of the speaking order.

Implement it as a pure, table-tested class with no I/O.

---

## 12. Teams and Microsoft 365 Copilot

- One bot (Agents SDK) and one app package in `appPackage/`. Scopes are personal, team and groupChat. The manifest also declares the agent for Microsoft 365 Copilot as a custom engine agent (**VERIFY** the current manifest schema). Sideload it for the demo users with the Agents Toolkit.
- Proactive messages use conversation references captured when the app is installed. The seeder prints the install link, and the portal shows who has the app installed.

**Cards:**

| Card | Recipient |
|---|---|
| Pre-brief | Human Scrum Master |
| Per-person follow-up with Approve, Edit and Reject per change | Each member |
| Absent-member request | Absent members |
| Blocker escalation | Human Scrum Master and, optionally, the manager |
| Recap | Meeting chat and team channel |
| Sprint summary | Team channel |
| Monthly report | Team channel |

**Recap to the meeting chat:** post it to the meeting's chat thread if the bot is added to the meeting chat. Otherwise post it to the configured team channel.

**Chat commands** (natural language through the Reporting agent):

- "what is blocking sprint {n}"
- "summarise the last standup"
- "my open items"
- "show the last six sprints"
- "start standup now" (human Scrum Master only)

---

## 13. Admin portal (demo scope)

| Page | Must have |
|---|---|
| Team | Azure DevOps org, project, team and area; time zone; standup length; stale days; escalation days; human Scrum Master; Teams channel |
| Roster | Members pulled from the Azure DevOps team; Teams identity mapping; pronunciation; speaking order; active or absent today |
| Schedule | Standup time, days and Teams join URL; bridge mode (`simulated` or `acs`); "Prepare now" and "Start now" buttons |
| Prompts | Edit the four instruction files per team (simple text editor, saved with a version number) |
| Live console | Current state, current speaker, time remaining, captions, events as they are recorded, latency p50; buttons: Mute agent, Next speaker, Skip, Take over, End |
| Sessions | List; detail with transcript, events, pre-brief, follow-ups and their status |
| Reports | Sprint summary and monthly report; KPI charts with the "illustrative" label when fixture-backed |

Sign-in uses MSAL against MCM's or the demo tenant; any signed-in user may edit in the PoC.

---

## 14. API (Mcm.Scrum.Api)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/health` | M0 health read model (`GetHealthQuery`); `/healthz` is the plain liveness probe |
| GET/PUT | `/api/teams/{id}` | Team settings |
| GET/PUT | `/api/teams/{id}/members`, POST `/sync` | Roster; sync from the Azure DevOps team |
| GET/PUT | `/api/teams/{id}/schedule` | Schedule and join URL |
| GET/PUT | `/api/teams/{id}/prompts/{name}` | Prompt text |
| POST | `/api/sessions/prepare` | `{teamId}`; returns the new session id (M1); read it with `GET /api/sessions/{id}` to get the pre-brief |
| POST | `/api/sessions/{id}/start` | `{mode, joinUrl?, standupMinutes?}`; returns `{bridgeToken, roomUrl}` (M2: `simulated` only) |
| POST | `/api/sessions/{id}/control` | `{action: mute|unmute|next|skip|takeover|end}` |
| GET | `/api/sessions?teamId=`, `/api/sessions/{id}`, `/api/sessions/{id}/events` | History and timeline (`/events` since M2; `GET /api/sessions/{id}` also returns events, attendance, latency p50/p95 and questions asked) |
| GET | `/api/sessions/{id}/live` | Server-sent events for the live console |
| GET | `/api/followups?teamId=&status=`, `/api/followups/{id}` | Follow-ups (M3) |
| POST | `/api/followups/propose` | `{sessionId, memberId, workItemId, op, value, rationale?}`; creates a pending follow-up. Ids outside the sprint are refused |
| POST | `/api/followups/{id}/approve`, `/reject` | `{approver}`; also used by the card actions. Approve triggers the Azure DevOps write |
| POST | `/api/followups/{id}/apply` | Retries the write of an approved follow-up; refused with `followup.not_approved` otherwise |
| POST | `/api/sessions/{id}/followups` | Plans and sends follow-ups for a session (idempotent; normally triggered by `StandupEnded`) |
| GET | `/api/cards?sessionId=` | Cards sent through the outbox messenger, with Adaptive Card JSON (M3) |
| GET | `/api/reports/sprint`, `/api/reports/monthly` | Reports |
| POST | `/api/demo/reset` | Reset demo state (sessions, follow-ups) without touching Azure DevOps |
| POST | `/api/messages` | Bot endpoint |
| WS | `/ws/bridge/{sessionId}` | Bridge |
| GET | `/healthz` | Probe |

Each route is a thin endpoint in `Endpoints/<Feature>Endpoints.cs` that sends exactly one command or query from section 6.1.4 and returns `result.ToHttpResult()`.

---

## 15. Data model

Containers, all in database `scrum`, each with partition key `/teamId`:

| Container | Contents |
|---|---|
| `teams` | Team settings |
| `members` | Roster and identity mapping |
| `sessions` | One per standup |
| `events` | Session events |
| `followups` | Follow-ups and approval state |
| `prompts` | Prompt versions |

Locally, the same role-specific repository ports (`ITeamRepository`, `ISessionRepository`, `IFollowUpRepository`) are implemented over JSON files under `.data/`; `Store:Mode` selects the implementation in `AddInfrastructure()`.

```jsonc
// teams
{ "id": "team-payments", "name": "Payments (Demo)", "timezone": "America/Los_Angeles", "standupMinutes": 15,
  "ado": { "org": "...", "project": "...", "team": "...", "areaPath": "...", "fieldMap": {} },
  "behaviours": { "staleDays": 2, "escalateBlockerAfterDays": 2, "silenceMs": 1200, "language": "en-US" },
  "humanScrumMasterId": "m-001", "channel": { "teamId": "...", "channelId": "..." } }
// members
{ "id": "m-011", "teamId": "...", "displayName": "Priya S.", "aadObjectId": "<guid>", "adoUniqueName": "...",
  "pronunciation": "PREE-ya", "speakingOrder": 1, "active": true, "conversationRef": { } }
// sessions
{ "id": "...", "teamId": "...", "mode": "simulated|acs", "joinUrl": "...", "state": "prepared|running|completed|failed",
  "preBrief": { }, "attendance": { "m-011": "present", "m-012": "absent" },
  "metrics": { "durationSec": 0, "p50LatencyMs": 0, "p95LatencyMs": 0, "questionsAsked": 0 } }
// events
{ "id": "...", "teamId": "...", "sessionId": "...", "ts": "...", "type": "transcript|commitment|blocker|decision|parking_lot|absent|tool_call",
  "memberId": "m-011", "workItemIds": [4521], "text": "..." }
// followups
{ "id": "...", "teamId": "...", "sessionId": "...", "memberId": "...", "kind": "followup|absent|escalation",
  "message": "...", "proposedAdoChanges": [ { "workItemId": 4521, "op": "comment|state|tag", "value": "...", "rationale": "..." } ],
  "status": "pending|sent|approved|rejected|applied", "approvedBy": null }
```

---

## 16. Milestones (one issue each; demo check = acceptance)

**M0 Skeleton and local run.**
- Build: the solution and Aspire AppHost with the four Clean Architecture projects and test projects (section 6); `Directory.Build.props`, `Directory.Packages.props` and `.editorconfig`; the hand-rolled mediator with `LoggingBehavior`, `ValidationBehavior` and `ExceptionMappingBehavior`, `Result`/`Error` and `ToHttpResult()`, with unit tests; architecture tests for the dependency rule; `Mcm.Scrum.Api` with health checks and the JSON-file repository; the bridge serving an empty `/room` page; the portal shell with sign-in; the configuration layout from section 17.
- Demo check: one command starts all three locally and the portal shows "API healthy" (served through a `GetHealthQuery` to prove the pipeline end to end); `dotnet build` has zero warnings and `dotnet test` is green, including architecture tests.

**M1 Demo data and pre-brief.**
- Build:
  - `tools/SeedAdo` and `demo/seed-ado.json`;
  - `AdoClient` with Entra or PAT auth, WIQL and the flag rules with unit tests;
  - `PrepareStandup` with the Backlog & Risk suggested questions;
  - the portal Team, Roster and Sessions pages showing the pre-brief.
- Demo check: after seeding, "Prepare now" shows every seeded flag on the right person, with a sensible question each; no invented work item ids.

**M2 Voice standup in the Simulated Room (core demo).**
- Build:
  - the Voice Live session and gateway WebSocket;
  - the floor-control state machine with table tests;
  - the facilitator tools;
  - `reconcile_update` with pre-computation;
  - the Simulated Room UI;
  - the demo cast player;
  - session events and latency metrics.
- Demo check: the presenter plus four cast members complete storyboard scenes 2 to 5 in 15 minutes or less. The agent calls everyone by name, asks at least two grounded questions, parks one topic, handles the absent member, warns on time and recaps. Latency p50 is shown.

**M3 Follow-ups, approvals and recap in Teams.**
- Build:
  - the bot and app package;
  - proactive cards (pre-brief, follow-up, absent, escalation, recap);
  - Approve, Edit and Reject wired to `ApplyApprovedChanges`;
  - a dev tunnel for local Teams testing.
- Demo check: storyboard scenes 1, 6 and 7. An approved change shows in Azure DevOps history with the approver's name; a rejected one is never written.

**M4 Real Teams meeting via ACS.**
- Build: ACS interop in the bridge (join by URL, lobby states, two-way audio, participants and dominant speaker); speaker attribution; error handling with a clear portal message.
- Demo check: storyboard scene 8 in a Teams meeting in the demo tenant (then MCM's), with the agent admitted from the lobby or by policy.

**M5 Admin portal and scheduling.**
- Build: the Schedule, Prompts and Live console pages; scheduler auto-prepare at T-10 and auto-start at T-1; take over and end controls; `/api/demo/reset`.
- Demo check: storyboard scene 9; a scheduled standup starts on its own.

**M6 Reporting and "ask the agent".**
- Build:
  - KPI maths with unit tests (Analytics or fixture);
  - the sprint summary and monthly report;
  - the portal Reports page;
  - chat Q&A in Teams and Microsoft 365 Copilot.
- Demo check: storyboard scenes 10 and 11; KPI numbers match a hand calculation on the fixture.

**M7 Deploy to Azure.**
- Build: `azd up` with Bicep for the resources in section 18, Container Apps for `api` and `bridge`, the portal served from `api`, and the bot endpoint updated to the Container Apps URL.
- Demo check: the full storyboard runs from Azure; `azd up` prints the portal, Simulated Room and app package paths.

**M8 Stretch (pick by audience):**
- sprint planning prep (capacity, carry-over and readiness against the Definition of Ready) as a portal page and a card;
- a retrospective in the Simulated Room using start-stop-continue;
- Story Writer drafting a user story and acceptance criteria from a parked decision (draft only, needs approval);
- email follow-ups;
- assignment recommendations for unassigned items;
- a cost and latency comparison across realtime models;
- the Facilitator bound to a Foundry voice agent (`FoundryAgent` mode) instead of model-direct, to inform the production design;
- a Work IQ real-time transcript listener when the preview is available.

---

## 17. Configuration

```
Foundry__ProjectEndpoint, Foundry__ReasoningDeployment, Foundry__SpeechRegion
VoiceLive__Endpoint, VoiceLive__ApiVersion, VoiceLive__Model, VoiceLive__Voice
Ado__Org, Ado__AuthMode=Entra|Pat, Ado__Pat (user secrets only)
Store__Mode=Json|Cosmos, Cosmos__Endpoint
Acs__Endpoint, Bridge__DisplayName="MCM Scrum Agent"
Bot__AppId, Bot__TenantId, Bot__AuthType=FederatedCredential|ClientSecret (secret in user secrets locally)
Demo__CastFile=demo/cast/payments-standup.yaml, Demo__KpiFixture=demo/kpi-history.json, Demo__HonorAgeTags=true
Features__EmailFollowUps=false, Features__WorkIqTranscripts=false
APPLICATIONINSIGHTS_CONNECTION_STRING
```

Use `DefaultAzureCredential` for Azure resources. Secrets go only in user secrets (local) or Container Apps secrets (Azure), never in the repo.

---

## 18. Azure resources (PoC)

One resource group in a region with Voice Live and the chosen realtime model (**VERIFY**, for example East US 2 or Sweden Central):

- Foundry resource and project with the realtime and reasoning deployments (Speech is included);
- Container Apps environment with apps `api` and `bridge` (the bridge needs about 2 vCPU and 4 GiB for headless Chromium);
- Container Registry;
- Cosmos DB serverless;
- Azure Communication Services;
- Azure Bot (single tenant, Teams channel);
- Application Insights;
- one user-assigned managed identity with Cognitive Services User and Azure AI User on Foundry (**VERIFY** role names) and Cosmos DB Built-in Data Contributor.

---

## 19. Tests (keep it light)

- Domain unit tests for the flag rules, KPI maths, the floor-control state machine (table-driven) and the follow-up approval invariants.
- Application handler tests with hand-written fakes for the ports, including the guard on Azure DevOps writes in `ApplyApprovedChangesCommandHandler`.
- Mediator tests: dispatch, behaviour order, validation short-circuit, exception mapping, notification fan-out, missing or duplicate handler detection.
- Architecture tests (`Mcm.Scrum.ArchitectureTests`): Domain references nothing; Application does not reference Infrastructure or Api; Infrastructure does not reference Api; handlers are `internal sealed` and end in `Handler`; commands end in `Command` and queries in `Query`; no handler depends on `ISender` (no handler-to-handler calls).
- Repository contract tests run against both the JSON-file and Cosmos implementations.
- One Playwright test that runs a scripted standup in the Simulated Room using the demo cast, and asserts that the session completes with events, an absent mark and follow-ups created.
- No evaluation pipeline in the PoC. The human Scrum Master's judgement in the demo is the quality bar; collect transcripts for production evaluations.

---

## 20. What we need from MCM (and when)

| Needed | By | Fallback if not available |
|---|---|---|
| Sandbox Azure DevOps project (or permission to use the Microsoft demo org) and process template | M1 | Microsoft demo org with the Agile template |
| Rajeev's documented Scrum Master steps and two or three recorded standups | M2 | Storyboard based on the November 13 discussion |
| Pilot team name, time zone, roster and human Scrum Master | M3 | Demo cast and demo tenant |
| Teams app sideloading for pilot users; a channel for recaps | M3 | Demo tenant |
| Teams meeting policy for ACS joiners: lobby admit or bypass, and the bot-detection setting | M4 | Human Scrum Master admits the agent from the lobby; or run in the demo tenant |
| Azure subscription and region | M7 | Microsoft-hosted demo subscription |
| Consent and notice wording for transcription | Before any MCM-live demo | Demo with Microsoft participants only |

---

## 21. PoC success criteria (overall)

1. Storyboard scenes 2 to 7 run end to end in the Simulated Room and scene 8 runs in a real Teams meeting, each in 15 minutes or less.
2. Every work item the agent mentions exists in the team's current sprint.
3. The human Scrum Master judges the recap and follow-ups accurate for the demo standups.
4. Each attendee gets at most one follow-up; approved changes are written with the approver's name; rejected changes are never written.
5. Median voice latency is about one second for US participants; India latency is measured and reported.
6. The demo can be reset and re-run in under five minutes.
7. The code meets the engineering standards in section 6.1: zero build warnings, architecture tests green, public APIs documented, every use case in the catalogue, so the PoC is a credible starting point for the production build.

Everything else (scale, the production bridge, identity, governance, evaluation, networking, operations) is in the *Production Specification*.
