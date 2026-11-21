# 0007. M8 stretch features, shared board, Teams audio

## Context
After M7 the remaining M8 stretch items were built, the agent gained a shared screen in Teams meetings, and agent audio in Teams was fixed.

## Decision
- **Agent audio in Teams:** the ACS calling SDK ignores a custom audio stream when the browser has no microphone (azure-sdk-for-js#29174). The bridge launches Chromium with `--use-fake-device-for-media-stream` so a device exists. Confirmed working in a real meeting.
- **Shared board:** `VoiceGateway` pushes a `board` message (current speaker's items, covered or not, per-person coverage) whenever it changes. `teams.ts` draws it on a canvas (`board.ts`), and shares `canvas.captureStream()` with `call.startScreenSharing(new LocalVideoStream(stream))` (raw screen share, documented in the SDK types). "Covered" means the reconcile step linked an item id to something the speaker said. Sharing is best effort and reported as an `Info` call state if the meeting disallows it.
- **Retrospective:** `GetRetrospectiveQuery`. `RetrospectiveBuilder` (Domain) builds start, stop and continue from sprint items, standup events and readiness gaps; the model only writes a short summary (`retrospective.md`). Text, not voice-led: a voice retrospective needs a different floor-control model.
- **Story Writer:** `DraftUserStoryQuery` returns a draft (`story-writer.md`, template fallback). It never writes to Azure DevOps; creating an item would need an approved follow-up (F7).
- **Email follow-ups:** `IEmailSender` behind `Features:EmailFollowUps`. `SendFollowUpsHandler` also emails the member. DEMO: `OutboxEmailSender` records emails instead of sending. PROD: ACS Email (needs a verified domain) or Graph `sendMail`.
- **Model comparison:** `GetModelComparisonQuery` combines `ModelComparison:Models` list prices (placeholders, VERIFY) with latency measured for the current model; cost per standup is pure arithmetic in `ModelCostEstimator`.
- **Foundry voice agent mode:** set `VoiceLive:AgentName` and `VoiceLive:AgentProjectName` and `VoiceLiveSessionFactory` connects with agent query parameters instead of a model. VERIFY: the parameter names; not exercised against a live agent.
- **Work IQ transcripts (preview): not built.** Work IQ needs delegated user sign-in (no app-only), a Foundry Work IQ MCP connection, and the Teams admin setting `-EnableWorkIQTranscriptAccess` (takes effect 14 Nov 2026). The real-time delivery contract is not documented well enough to code against, and the ACS audio path already gives us live transcription. Revisit when it is generally available.

## Consequences
New use cases are in SPEC 6.1.4 and the routes in SPEC 14. The portal has Retrospective, Story Writer and Models pages.
