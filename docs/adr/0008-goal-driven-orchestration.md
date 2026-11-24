# 0008. Goal-driven, reactive standup orchestration

## Context
The standup was a linear loop (call on, listen, clarify, next). Real meetings are not linear: teammates chime in, someone steps away, one person gives another's update, people ask what they should cover, ask about items, correct themselves, or stay silent.

## Decision
The goal is fixed (every member's items covered, nobody forgotten). What happens in the room changes who is asked next and what the agent says, never the goal. Decisions are made in code; the model only phrases the reply.
- **Interpretation (Domain, pure):** `UtteranceClassifier` classifies each utterance in context of the speaker and the floor holder: update, step-away (self or about a teammate), proxy (offer to give someone's update, or the holder handing over), what-to-report, item question, correction, out-of-turn update, back, chatter. It uses rules, so it adds no latency and is table-tested. `ItemMentions` links speech to items.
- **Agenda (Domain):** `StandupSession` derives deferral from events (`Deferred`, `CircledBack`, `NoResponse`, `Proxy`, `Correction`). `NextSpeaker` skips deferred members and returns them last (circle back). `Retract` marks a corrected statement; `ActiveEvents` hides it from follow-ups, recaps and reports while the audit trail keeps it.
- **Floor control:** new `DeferCurrent` input, `DeferSpeaker` action and `Deferring` state. Silence before the first word is "present but no response" (different from absent): the member is deferred and asked once more; silent twice is final and gets a follow-up that says they were there.
- **Reactions (Api gateway):** out-of-turn updates get a one-time polite redirect; step-away skips the member (now or in advance); a proxy's speech is attributed to the member whose turn it is; what-to-report is answered from `ReportingPriorities` (blocked, overdue, stale first); a correction asks the reconcile agent which earlier statements (by number, only the speaker's own) were taken back. Follow-ups from proxied updates still need the owner's approval (F7).
- **Speaker attribution:** in Teams the mixed audio has no speaker, so the member whose live "speaking" event overlaps the speech (else the floor holder) is the speaker. Previously every Teams utterance was attributed to the first present member.
- **Board:** shows per-member status (away, no response, update by someone).

## Consequences
New command `RetractStandupEventCommand`, new `NextSpeakerDto.CircleBack`, new turn templates (`turn-step-away`, `turn-no-response`, `turn-circle-back`). Classifier phrases are English and rule based; PROD: add a model-assisted classifier behind the same interface for ambiguous speech (**done in M10, see ADR 0014**: a decision model reads the utterance first and these rules stay as the fallback). A scripted messy standup (`files/scenario.mjs` in the session folder) exercised every path against the real gateway.
