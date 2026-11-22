# 0009. Documentation baseline

## Context
SPEC.md was written before the build and drifted from the code (roster size, card actions, agent stack, configuration keys). Several documents made competing claims, and Copilot instructions described choices the code no longer follows.

## Decision
When documents disagree, the higher one wins:
1. **The code and its tests.**
2. **docs/AS-BUILT.md**: what was actually built, including known limits.
3. **ADRs** in `docs/adr/`: why the build deviates from the SPEC.
4. **docs/SPEC.md**: the original intent and history; it is kept in sync where practical (use-case catalogue 6.1.4, API 14, configuration 17) but is not authoritative.

`docs/CEREMONIES.md` defines ceremony scope; new ceremony work reuses the three patterns there. `docs/DOC-DRIFT.md` records the differences found when this baseline was set. Never describe something as working unless code or a test shows it.

## Consequences
- A contradiction is resolved by fixing the lower document, not the code, unless the code is a bug.
- Deviations from the SPEC need an ADR, not a silent code change.
- The drift report is a snapshot; it is not maintained after this PR.

## Naming debt (M9)
`StandupSession` now also holds planning, review and retrospective sessions (`CeremonyKind`). It is not renamed in M9 to keep the diff reviewable; a later change should rename it to `CeremonySession` (and `StandupPrepared` and `StandupEnded` events with it) in one mechanical commit.
