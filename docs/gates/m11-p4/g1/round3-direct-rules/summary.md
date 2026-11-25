# Gate runner summary

### round3-direct-rules (build e37ef09, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2993 | 4283 | 12 |
| Speech end to transcript | 0 | 241 | 12 |
| Analyser (rules and grounding) | 26 | 56 | 12 |
| Decision model call | 0 | 0 | 12 |
| Orchestration (floor, silence, reconcile) | 1545 | 2526 | 12 |
| Voice Live first audio | 1433 | 2430 | 12 |

Questions only (Clarify and Unblocker turns): p50 3241 ms, p95 4283 ms, n 9.
Read by the decision model: 0 of 12 turns; the rest used the rules.
API-reported session p50 per run: 2993 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
