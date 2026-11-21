# 0003. M2 deviations: gateway-driven turns, read-only model tools, text-injected cast, grouped message records

## Context
M2 builds the voice standup in the Simulated Room against real Azure Voice Live (`gpt-realtime-2.1`) and Azure Speech. Running it end to end showed where the SPEC's sketch (10.1, 7.2, 8, 11) needed changes, and where the repository rules needed an exception.

1. SPEC 10.1 lets the facilitator model drive the conversation with tools (`get_next_speaker`, `record_event`, `park_topic`, `end_standup`, ...). With those tools offered, the model recorded the same blocker twice, parked topics itself (with a nicer title but a second event), and called `end_standup` before giving its recap. Floor control is meant to own the floor (SPEC 11.3), so the model must not also own the flow.
2. SPEC 7.3 has the bridge synthesise cast lines and stream them as audio. Streaming synthetic audio through Voice Live speech-to-text adds seconds and mis-transcribes, and it makes cast timing depend on STT.
3. SPEC 11.3 times silence from the end of an utterance. Speech-to-text delivers its transcript after the speaker has stopped and may split one answer into several segments, so utterance events alone triggered the reconcile too early.
4. SPEC 9.3 / 10.2 wait for the reconcile before moving on. Every reconcile is a 1.5 to 2.5 s model call, which dominated end-of-speech to first-audio latency.
5. Repository rules ask for one public type per file.

## Decision
1. **Gateway-driven turns.** `FloorControl` decides when the agent speaks; the gateway turns each `AgentSpeak(reason)` into a `response.create` whose instructions come from `agents/instructions/turn-*.md` (rendered by `GetTurnInstructionQuery`). The facilitator persona (`facilitator.md`) is repeated with every turn because per-response instructions replace the session ones. The model is offered only read-only tools (`IScrumTools.FacilitatorDefinitions`: `get_member_brief`, `get_time_remaining`). All eight tools stay implemented and dispatchable, so exposing them over MCP later is unchanged. Events, parking and ending are recorded by the gateway through commands.
2. **Cast lines are text, with real timing.** The bridge synthesises cast speech (Azure Speech REST, Entra token, raw PCM16 24 kHz) and the room plays it, but sends the gateway the *text* (`utterance`) plus `speaking` start/stop messages, and the gateway adds it to the conversation as a user message. The presenter's live microphone, and the presenter's autopilot lines, are real audio through Voice Live speech-to-text.
3. **Activity-based silence.** `FloorControl` takes `SpeechActive(member, active)` inputs (from Voice Live `speech_started/stopped` and the room's `speaking` messages). Anyone talking holds back the silence timer, and the gateway waits (up to 2.5 s) for outstanding transcripts before reconciling. A side discussion therefore cannot be mistaken for the holder finishing, and it still counts toward the parking-lot threshold.
4. **Do not wait when no question is possible.** When the member has nothing flagged (`NextSpeakerDto.HasFlaggedItems`) or their one question was already asked, floor control advances at once (`RunReconcile` with `Background = true`) and the reconcile only records events. Reconcile results are computed while the member talks (speculative, per transcript segment) and reused if the text has not changed.
5. **Grouped records are allowed for protocol messages.** Small message records that are only meaningful together (`FloorInput`, `FloorAction`, `VoiceEvent`) share a file per family instead of one file each. All other types keep one public type per file.

## Consequences
- The demo flow is deterministic enough to run as an automated test (`npm run e2e`): the model phrases, code decides.
- Latency: measured end of speech to first agent audio is about 2.0 to 2.9 s p50 (p95 3.5 to 6 s) against the SPEC target of about 1 s. The floor is Voice Live itself (about 0.7 to 1.6 s from `response.create` to first audio, measured separately), plus 0.8 s silence, plus about 2 s of model time on turns that ask a question. Levers left: a shorter silence timer, a faster reasoning deployment, and skipping the model for the question.
- Late-joiner handling (SPEC 11.3) is not implemented; it needs real participants (M4).
- The model can still misclassify events ("Nothing blocking me" as a blocker); that one is filtered in code, others are visible in the portal timeline.
