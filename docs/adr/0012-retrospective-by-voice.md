# 0012. Retrospective by voice (M9b)

## Context
M9 built a retrospective pack (a code-computed draft and a one-card follow-up) but the retrospective itself was not led by voice. CEREMONIES C4 asks for the voice engine to be reusable: a different ceremony on the same floor control, with action items written back after approval. The rules do not change: no Azure DevOps write without an approved follow-up (F7), ids come from Azure DevOps, numbers and sorting come from code, prompts live in `agents/instructions`.

## Decision
- **One gateway, one floor control.** The retrospective is a session with `Kind=Retrospective` run by the existing `VoiceGateway` and `FloorControl`. `FloorControl` gains one state, `Announcing`, and one input, `AnnounceStep`, for the steps where the agent speaks to everyone (the themes read-back). Nothing else in the state machine changes.
- **The order of steps sits behind `ICeremonyAgenda`.** `StandupAgenda` is the previous `StandupSession.NextSpeaker` logic moved without a behaviour change (`NextSpeaker` delegates to it, so every existing test is unchanged). `RetroAgenda` composes it for the Round, then yields a themes `Announcement`, then Actions turns, then `null` (close). The phase is derived from `Phase` events, so the aggregate gains no state.
- **Sorting is rule based.** `RetroItemClassifier` and `RetroInterpreter` (Domain) turn speech into notes, retractions and at most one question; `ThemeGrouper` groups by shared keywords. A model only names the groups and phrases the turns. This keeps the board and the follow-up reproducible and table-testable.
- **Events and the gateway.** New `StandupEventType` values `RetroItem`, `ActionItem`, `Theme`, `Phase` (appended, so stored sessions load unchanged) and `Category` and `OwnerMemberId` on `StandupEvent`. The gateway picks `ReconcileRetroUpdateQuery` or `ReconcileUpdateQuery` by kind; both return `ReconcileResult`, so recording and retraction are shared. A clarifying question reuses the floor's Clarify turn; the answer retracts the unsorted note and re-records it with its category.
- **A retrospective raises `RetrospectiveEnded`, not `StandupEnded`.** `StandupSession.End` chooses the event by kind, so the standup recap, the per-member follow-ups and the pre-brief card do not fire. `FinishOnRetrospectiveEnded` hangs up and sends `CreateRetroOutcomeCommand`, which creates one Scrum Master follow-up with `CreateWorkItem` changes (`RetroOutcomePlanner`) and posts the summary. It is idempotent.
- **Settings behind a port.** `IRetroSettings` (`Retro:Minutes`, `Retro:Format`), implemented by `RetroOptions`; handlers never read configuration.
- **Bridge.** The cast script and the board are selected by the session kind (`hello.kind`); `demo/cast/payments-retro.yaml`, `Demo__RetroCastFile`, a retro view in `BoardRenderer` and `npm run e2e:retro`.
- **DEMO owners are tags.** `ProposedChange.Create` takes the roster key; with `Demo:HonorOwnerTags` the writer adds the `demo-owner-{key}` tag to the new task instead of `System.AssignedTo`. `// PROD: AssignedTo only.`
- **Naming debt unchanged.** The aggregate is still `StandupSession` (ADR 0009).

## Consequences
- The standup behaves as before: the standup e2e passes unchanged.
- The retro only sorts what contains the category keywords; the clarifying question covers the rest, and an answer that is not a category leaves the note unsorted (it appears in no column) rather than looping.
- Themes are literal shared words, so they are modest; the model's names cannot change which notes belong together.
- Checked live in the Simulated Room (`npm run e2e:retro`; approving the follow-up created tasks with owners and the attribution). A Teams-meeting run, the Teams card actions and a live **Reject** are not checked.
