# Copilot instructions: scrum-master-voice-poc

These instructions apply to every Copilot session in this repository. The build spec is `docs/SPEC.md`. Read the relevant section before writing code, and implement one milestone (SPEC section 16) at a time.

## What we are building and why
- A **demo-first proof of concept** of a voice-led "junior Scrum Master" for Zava. It runs a daily standup by voice (Azure Voice Live), grounds questions in Azure DevOps, posts a recap and per-person follow-ups in Teams, writes approved changes back to Azure DevOps, and has a small React admin portal.
- **Ceremonies are defined in `docs/CEREMONIES.md`; new ceremony work reuses the three patterns there** (P1 live facilitation by voice, P2 prep pack, P3 close the loop with approval).
- Which document wins when they disagree: code, then `docs/AS-BUILT.md`, then the ADRs, then `docs/SPEC.md` (history and intent). See `docs/adr/0009-documentation-baseline.md` and `docs/DOC-DRIFT.md`.
- **Success = the demo storyboard in SPEC section 2 works reliably and can be reset and re-run.** Scope is demo-first; code quality is not. Build only what the milestones ask for, but build it to the engineering standards below so the PoC code can grow into the production system.
- Production *infrastructure* concerns (API Management, Service Bus, MCP server hosting, private networking, app roles, on-behalf-of, evaluation pipelines, multi-team scale) are **out of scope**. Do not build them. Where a production design would differ, leave a one-line `// PROD: <what changes>` comment and move on.

## Stack (do not substitute)
- Backend: C# / .NET 10, ASP.NET Core minimal APIs, nullable enabled, records for DTOs, commands and queries.
- Solution shape: Clean Architecture, four projects: `Zava.Scrum.Domain`, `Zava.Scrum.Application`, `Zava.Scrum.Infrastructure`, `Zava.Scrum.Api` (the single deployable host). See SPEC 6 and 6.1.
- Mediator: **hand-rolled**, in `Zava.Scrum.Application/Abstractions/Messaging` (SPEC 6.1.3). Do not add MediatR, Mediator (source-generated), Wolverine, Brighter or any other mediator or messaging package.
- Validation: FluentValidation. Architecture tests: NetArchTest.Rules 1.3.2.
- Local run: Aspire AppHost (`src/Zava.Scrum.AppHost`, Aspire 13.x; there is no `Makefile`). Central package versions are in `Directory.Packages.props`; `global.json` pins .NET SDK 10.0.100.
- Agents: Microsoft Agent Framework chat-client agents (`Microsoft.Agents.AI.Foundry`, `AIProjectClient.AsAIAgent`) over the Foundry v2 SDK (`Azure.AI.Projects` 2.x) with `DefaultAzureCredential`, behind the agent ports (`FoundryChat`, `FoundryAgentFactory` and the `Foundry*Agent` adapters; ADR 0009b). Code-defined agents only: no server-side agent resources. Every adapter returns `null` on any model failure and the caller falls back to code. `// PROD: Foundry hosted agents with Entra Agent ID`. Do not use Semantic Kernel planners or the classic Assistants API.
- Voice: Azure Voice Live over `ClientWebSocket`. Teams: Microsoft 365 Agents SDK for .NET 1.9 (not Bot Framework v4); the app manifest declares a bot only, so Microsoft 365 Copilot is not built.
- Bridge: Node.js 22+ (`engines`) + TypeScript (strict, ESM), `playwright-core` for headless Chromium, ACS Calling SDK. Portal: React 19 + Vite + TypeScript + Tailwind CSS v4 + lucide-react + MSAL.js; Container Apps Easy Auth protects the deployed apps.
- Azure DevOps via REST (`HttpClient`), not a third-party SDK. Deploy with `azd` + Bicep.

## The few rules that are not optional
1. **No Azure DevOps write without an approved `followups` record.** Enforce it in the `ApplyApprovedChangesHandler` (and as a domain invariant on `FollowUp.GetApprovedChanges`), with unit tests. This is a customer requirement and part of the demo.
2. **Never invent work item ids.** Drop any id not in the current sprint before it reaches the agent's speech or a card.
3. **Numbers come from code.** Flag rules, burndown and KPI maths are plain C# in the Domain layer with unit tests; models only phrase text.
4. **No secrets in the repo.** Use `DefaultAzureCredential` for Azure; PATs and bot secrets go in .NET user secrets locally or Container Apps secrets in Azure.
5. **Prompts live in `agents/instructions/*.md`**, not in C# strings, so they can be tuned during demos without a rebuild.

## Engineering standards (apply to every PR)

### Clean Architecture: the dependency rule
Dependencies point inwards only: `Api -> Infrastructure -> Application -> Domain`, and `Api -> Application`.
- **Domain** (`Zava.Scrum.Domain`): entities, value objects, enums, domain events, domain services and pure rules (flag rules, KPI maths, the floor-control state machine, follow-up approval invariants). No references to any other project or to ASP.NET, Azure, HTTP, JSON, MAF, Voice Live or ADO packages. No I/O, no `DateTime.UtcNow` (take an `IClock` or a timestamp parameter).
- **Application** (`Zava.Scrum.Application`): use cases as commands and queries with their handlers, validators, DTOs and **ports** (interfaces) for everything outside the process: `IWorkItemReader`, `IWorkItemWriter`, `ITeamsMessenger`, `IVoiceSessionFactory`, `IBacklogRiskAgent`, `IReconcileAgent`, `IReportingAgent`, `IDraftingAgent`, `IPromptStore`, `IEmailSender`, `IIterationReader`, `IBacklogReader`, `IPlanningPackRepository`, `IModelCatalog`, repositories, `IClock`. References Domain only (plus FluentValidation and `Microsoft.Extensions.*.Abstractions`).
- **Infrastructure** (`Zava.Scrum.Infrastructure`): adapters that implement the ports: Azure DevOps REST client and writer, repositories over `ICollectionStore` (JSON files or Cosmos), Voice Live session factory, Foundry chat agents, Teams messengers (Agents SDK proactive messaging and an outbox), prompt store, clock. One `AddInfrastructure(IServiceCollection, IConfiguration)` extension registers them.
- **Api** (`Zava.Scrum.Api`): the composition root and thin presentation adapters: minimal API endpoints, the bot activity handler, the bridge WebSocket endpoint, the scheduler `BackgroundService`, SSE. Adapters translate input into a command or query, call `ISender`, and translate the result back. **No business logic in endpoints, the bot handler, the WebSocket loop or the scheduler.**
- The dependency rule is enforced by architecture tests in `Zava.Scrum.ArchitectureTests` (SPEC 19). A PR that breaks them is not done.

### CQRS with the hand-rolled mediator
- Every use case is either a **command** (changes state, returns `Result` or `Result<TId>`, never a read model) or a **query** (no side effects, returns a read DTO). Never both.
- One use case = one folder in `Application/Features/<Area>/<UseCase>/` containing `<UseCase>Command.cs` or `<UseCase>Query.cs`, `<UseCase>Handler.cs`, optional `<UseCase>Validator.cs` and the response DTO. Use the catalogue in SPEC 6.1.4; add new use cases there in the same PR.
- Handlers are `internal sealed`, do one thing, and depend only on ports and Domain. Handlers never call other handlers; share logic through a Domain service or publish a domain event.
- Commands and queries are `sealed record`s, immutable, and serialisable.
- Side effects that follow a state change (for example "follow-up approved -> apply ADO change -> update card") are raised as **domain events** on the aggregate and dispatched through `IPublisher` after the repository save. Notification handlers must be idempotent.
- Cross-cutting concerns live in **pipeline behaviours**, registered in this order: `LoggingBehavior` (structured log + latency in ms), `ValidationBehavior` (FluentValidation, returns `Result.Invalid`), `ExceptionMappingBehavior` (unexpected exceptions -> `Error.Unexpected`, logged once). Do not repeat try/catch, logging or validation in handlers.
- Logical CQRS only: commands and queries share the same store in the PoC. No separate read database, event sourcing or outbox. `// PROD:` note where this would change.
- The mediator is hand-rolled (SPEC 6.1.3). Keep it under ~200 lines, reflection only at startup (assembly scan in `AddApplication()`), cached delegates at runtime, unit tested. Do not grow it into a framework: no streaming requests, no pre/post processors beyond behaviours, no priority ordering.

### Errors and results
- Expected failures (not found, invalid, conflict, not approved, ADO rejected) use `Result` / `Result<T>` with a typed `Error(Code, Message, ErrorType)`. Exceptions are for bugs and infrastructure faults only.
- One endpoint helper, `result.ToHttpResult()`, maps `ErrorType` to status codes and the PoC error body `{ "error": "...", "detail": "..." }`. Do not hand-write status code mapping in endpoints.

### SOLID
- **S:** one reason to change per class. Handlers coordinate; rules live in Domain; adapters translate. If a class needs "and" to describe it, split it.
- **O:** add behaviour with new handlers, behaviours, strategies or flag rules (each flag rule implements `IFlagRule`), not by editing switch statements.
- **L:** every port implementation should pass the same shared contract test. As built, repositories are `JsonFile*` classes over `ICollectionStore`, whose `FileCollectionStore` and `CosmosCollectionStore` are the two implementations; see Known gaps for the contract test.
- **I:** small, role-specific ports (`IWorkItemReader` and `IWorkItemWriter`, not one `IAdoService`). No interface with methods a consumer does not use.
- **D:** depend on abstractions owned by the inner layer; constructor injection only; no service locator, no `IServiceProvider` in handlers or domain code; no `static` mutable state.

### DRY (without over-abstracting)
- One source of truth for: configuration keys (options classes in SPEC 17), prompt text (`agents/instructions`), ADO field reference names (`AdoFields` constants), card templates (`Infrastructure/Teams/Cards/*.json`), error codes (`TeamErrors`, `StandupErrors`, `PromptErrors` static classes per area), and the bridge protocol message types (mirrored once per language in `VoiceGateway.cs` and `src/meeting-bridge/src/web/protocol.ts`; no sync test yet, see Known gaps).
- Extract shared code on the third repetition, not the first. Prefer a small helper or a Domain method over a generic base class. Do not create generic repositories, base handlers or "manager" classes.

### Organisation and naming
- Folder = namespace, file-scoped namespaces, one public type per file, file name = type name.
- Feature folders in Application (`Features/Standups`, `FollowUps`, `Reports`, `Teams`, `Schedules`, `Prompts`, `Planning`, `Retrospectives`, `Stories`, `Models`, `Demo`, `Health`); in Api, endpoints are grouped per feature in `Endpoints/<Feature>Endpoints.cs` (most use `MapGroup`).
- Names say intent: `StartStandupCommand`, `GetNextSpeakerQuery`, `FollowUpApproved` (event), `IWorkItemReader`. No `Helper`, `Util`, `Manager` or `Common` dumping grounds.
- `Directory.Build.props` sets `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `GenerateDocumentationFile=true`. `Directory.Packages.props` pins all package versions (central package management). `.editorconfig` is the style authority; run `dotnet format` before committing.
- TypeScript (bridge, portal): `strict`, ESLint + Prettier, the same layering in miniature (`domain/` pure logic, `services/` I/O adapters behind interfaces, `ui/` or `routes/` presentation). Portal: feature folders, one typed API client generated or hand-written once in `src/api/`, hooks for data access, no fetch calls inside components.

### Documentation and comments
- **XML doc comments on every public and internal type and member** in Domain and Application, and on every public type in Infrastructure and Api. Missing docs are build warnings, which are errors. `<summary>` says what and why; add `<param>`, `<returns>`, `<exception>` and `<remarks>` where they add information.
- TSDoc on every exported function, class, type and React component in the bridge and portal.
- **Comments explain why, not what**: business rules ("ADO writes require an approver: customer requirement F7"), protocol quirks, latency trade-offs, and the reason for any non-obvious choice. Reference the SPEC section or requirement id (F1-F10) where relevant. Remove commented-out code.
- Marker comments are standard and searchable: `// PROD: <what changes in production>`, `// VERIFY: <doc url>`, `// DEMO: <shortcut that exists only for the demo>`. Never leave bare `TODO`s; use one of these or open an issue.
- Each project except `Zava.Scrum.AppHost` and `src/tests` has a short `README.md`: purpose, what it may reference, how to run its tests. `docs/adr/NNNN-<title>.md` records any deviation from the SPEC or these standards (one page: context, decision, consequences).
- Keep `docs/SPEC.md` section 6.1.4 (use-case catalogue) and section 14 (API) in sync with the code in the same PR.

## Demo reliability (treat as features)
- Keep the **Simulated Room** working at every milestone; it is the fallback for any live demo.
- The **demo cast** (scripted TTS participants) must let one presenter run a full three-person standup (Daniel Park, Maya Lin, Admin).
- Provide `/api/demo/reset` (a `ResetDemoCommand`) and make the seeder idempotent so the demo can be re-run in under five minutes.
- Surface failures plainly in the portal (for example "Waiting in Teams lobby", "Voice Live disconnected - retrying") instead of failing silently.
- `LoggingBehavior` and the voice gateway log one structured line per agent turn with `sessionId`, `memberId`, state transition and latency in ms. (Basic OpenTelemetry to Application Insights is wired in `Zava.Scrum.Api/Telemetry`; see Known gaps for what it does not cover.)

## Voice specifics
- The voice gateway owns the floor (SPEC 11.3). The facilitator speaks only when the state machine allows it; cancel unsolicited responses with `response.cancel`.
- The floor-control state machine is a pure Domain class with table-driven tests, no I/O. The gateway (Api adapter + Infrastructure Voice Live client) feeds it events and executes the actions it returns.
- Voice Live tool calls are dispatched to `IScrumTools` (Application), whose methods send commands and queries through `ISender`. Tools contain no business logic of their own.
- Audio on the bridge is PCM16, 24 kHz, mono, 20 ms frames. Do not resample anywhere else.
- Pre-compute `ReconcileUpdateQuery` for the current speaker on each transcript segment while they talk, and reuse it when the text is unchanged; latency is a demo metric (measured p50 is 2 to 3 s against a 1 s target). The audio hot path (frame forwarding) does **not** go through the mediator.

## When an API detail is uncertain
- Voice Live, the Agents SDK manifest for Copilot, the ACS raw-audio APIs and Aspire move quickly. Check Microsoft Learn (Learn MCP server) first and leave `// VERIFY: <doc url>` next to any field or header you could not confirm. Each such dependency already sits behind a port, so a fix stays inside one Infrastructure adapter.
- If something marked VERIFY does not work as written, choose the simplest documented alternative that keeps the demo scene working, and say so in the PR (and an ADR if it changes the design).

## Code style
- Constructor injection; options pattern bound to the configuration keys in SPEC 17 with `ValidateOnStart`. Async all the way with `CancellationToken` on every async method; no `.Result` or `.Wait()`.
- Guard clauses at the top of methods; early returns over nested `if`s; methods short enough to read without scrolling (aim for under 30 lines).
- Immutable by default: `record`s, `init` setters, `IReadOnlyList<T>` on public surfaces. Aggregates expose behaviour methods (`followUp.Approve(approver, clock)`), not public setters.
- Use `TimeProvider`/`IClock`, never `DateTime.Now`.

## Tests
- Required:
  - Domain unit tests: flag rules, KPI maths, floor-control state machine (table-driven), follow-up approval invariants;
  - Application handler tests with fake ports (no mocking framework needed; hand-written fakes in `TestDoubles/` are preferred), including the approval guard on ADO writes;
  - mediator tests (dispatch, behaviour order, notification fan-out);
  - architecture tests for the dependency rule and naming conventions (SPEC 19);
  - one end-to-end test of a scripted Simulated Room standup (`npm run e2e --prefix src/meeting-bridge`, Playwright).
- Not required: coverage targets, evaluation datasets or CI quality gates.
- Test names: `Method_State_ExpectedResult`; Arrange / Act / Assert blocks.

## Pull requests
- One milestone or less per PR, titled `M<n>: <summary>`.
- The description lists the milestone's demo check, the exact commands to run it, any `VERIFY`, `PROD` or `DEMO` notes added, and any ADR written.
- Definition of done: demo check passes, `dotnet build` with zero warnings, `dotnet test` green (including architecture tests), `dotnet format --verify-no-changes` clean, lint clean for TypeScript, public APIs documented, SPEC 6.1.4 and 14 updated.

## Known gaps (do not treat as rules the code already meets)
- **Repository contract tests:** `SessionRepositoryContract` covers `ISessionRepository` against the JSON-file store only. There is no Cosmos run and no contract for the other repositories.
- **OpenTelemetry and Application Insights:** basic only (ASP.NET Core requests, outgoing HTTP, one span per use case from `LoggingBehavior`, Agent Framework model calls). It is on only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set, has not been checked in a live Application Insights resource, and has no custom metrics, sampling or bridge traces. The gateway logs one structured line per agent turn through `ILogger`.
- **Bridge protocol sync test:** none; `VoiceGateway.cs` and `protocol.ts` are kept in step by hand.
- **TypeScript docs:** TSDoc is by convention; ESLint (`typescript-eslint` strict) has no `jsdoc` rules. ESLint does ban `TODO` and `FIXME` comments in TypeScript (`no-warning-comments`); C# relies on code review.
- **Portal tests:** none. The bridge has a unit test for the cast planner and the end-to-end test.
- **Agent runtime:** Microsoft Agent Framework chat-client agents are used (ADR 0009b); Foundry hosted agents are not.
- **Not built:** Microsoft 365 Copilot, Work IQ transcripts, Edit and Acknowledge/Reassign card actions, email sending unless `Email:Mode=Graph` (unverified live), and sprint meetings in Outlook unless `Calendar:Organizer` is set (`ICalendarWriter`, unverified live; ADR 0013).

## Ceremonies (M9)
- `CeremonyKind` is on the session aggregate (still named `StandupSession`; naming debt in ADR 0009). New ceremony work reuses the follow-up, approval guard, card renderer, outbox and `IWorkItemWriter`; a pack is built by a Domain builder (`Domain/Ceremonies`), sent through `CreateCeremonyPackCommand`, and written only after `FollowUp.GetApprovedChanges` succeeds. See ADR 0010. A retrospective run by voice (M9b, ADR 0012) uses the standup's gateway and `FloorControl`; the order of steps sits behind `ICeremonyAgenda` (`StandupAgenda`, `RetroAgenda`) and its sorting rules are Domain code, not model calls.
- Sprint planning (M9a) is the exception to "a pack is a follow-up from `CreateCeremonyPackCommand`": it has its own `PlanningPack` aggregate (`PreparePlanningCommand`, `ApprovePlanCommand`), and approval creates the Scrum Master follow-up so the write still goes through `FollowUp.GetApprovedChanges`. See ADR 0011.
