# 0016: End-of-standup consequences must not run on the connection's token

## Context
After a standup ended, the recap card and per-person follow-ups sometimes never appeared (finding 1.20). The end path is: `EndStandupHandler` saves the session and publishes `StandupEnded`; the handlers `LeaveMeetingOnStandupEnded`, `PostRecapOnStandupEnded` and `SendFollowUpsOnStandupEnded` then run in DI registration order (assembly type order, not a contract).

## Diagnosis
Found by reading the code path; each step now logs with the session id as correlation id (`StandupEnded raised for session {SessionId}`, `Notification {Event} -> {Handler}`, `Sent {Count} follow-up card(s)`, and a warning when a handler is cancelled).

- `VoiceGateway.EndAsync` sent `EndStandupCommand` with `_run`, a token linked to the bridge WebSocket's `RequestAborted` (`BridgeEndpoints`).
- `LeaveMeetingOnStandupEnded` hangs up the call. The bridge then closes its socket, so `RequestAborted` fires and `_run` is cancelled while the remaining handlers are still running.
- `Mediator` deliberately rethrows `OperationCanceledException`, so one cancelled handler skipped every handler after it. A handler's order is not defined, so the loss depended on whether Leave ran before the others.
- The session was already saved as `Ended`, so the gateway's closing `EndStandupCommand` (token `None`) published nothing: `StandupSession.End` is a no-op the second time. The follow-ups were lost for good, with no log line.

Not the cause: swallowed handler exceptions (those are logged), the scheduler, and follow-up generation itself (covered by the existing test).

## Decision
`EndStandupHandler` publishes its events with `CancellationToken.None`. The state change is committed, so its consequences must complete whatever happened to the caller's token. No retry was added. The mediator still rethrows cancellation, but now logs a warning naming the handler so a skipped handler is visible.

## Consequences
- Handler order no longer matters for correctness.
- `EndStandupFollowUpsTests.End_WhenLeavingTheCallCancelsTheCallersToken_StillSendsRecapAndFollowUps` goes through the real mediator with a bridge that cancels the caller's token on leave and a messenger that refuses cancelled tokens. It fails on the old handler and passes now.
- Measured: not yet verified in a real Teams meeting. Finding 1.20 stays open until that run is pasted into `docs/AS-BUILT.md`.
- PROD: an outbox would make delivery survive a process stop between save and publish.
