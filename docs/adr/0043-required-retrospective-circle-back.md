# ADR 0043: mandatory retrospective circle-back instruction

## Context and evidence

The prior real E2E retrospective passed5/6, then6/6 on an unchanged retry.
The agenda already returned deferred members before themes and recorded
`CircledBack`. The failure was spoken instruction fidelity: a welcome or
generic invitation could omit the three-point prompt, so the scripted
return/correction was not reliably elicited. Existing team prompt overrides
could preserve this behaviour after changing only the default file.

## Decision

Keep the deterministic Domain agenda and floor step. Append the required
`turn-retro-circle-back-required.md` contract after the team style template:
address the member, explicitly say "I'm circling back", and invite the
configured retrospective categories with up to three points. Require its
presence through the real mediator; missing instructions are explicit
errors, not silent advancement or a successful-looking default.

The model still provides actual Voice Live audio. This is a structured
mandatory instruction, not a prerecorded substitute or simulated completion.
The E2E assertion and GateRunner require recorded deferral/return plus the
correct member's spoken return and subsequent note before Themes.

## Consequences and measurement boundary

Real-mediator tests prove the required clause survives the older optional
style template. `eng/test-retro-fidelity.ps1` drives30 complete existing
Simulated Room retrospectives against zavaw3, using the real model and
refreshing API credentials between runs. Each result is saved independently;
no failed run is silently retried. Pending measurements are explicitly
unrun until the final count appears in `gates/r2-quality-M11.json`.

No new resource, permission, recording exception, default adapter or fixed
hourly cost. Voice/model/Cosmos usage remains variable. This proves scripted
instruction fidelity, not actual Teams/user rehearsal or eternal LLM
determinism; any failing run stays failed with its denominator.
