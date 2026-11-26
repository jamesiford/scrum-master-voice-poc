# Gate runner summary

### round3-direct-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3198 | 5284 | 29 |
| Speech end to transcript | 0 | 131 | 29 |
| Analyser (rules and grounding) | 26 | 39 | 29 |
| Decision model call | 158 | 215 | 29 |
| Orchestration (floor, silence, reconcile) | 1562 | 2948 | 29 |
| Voice Live first audio | 1338 | 2667 | 29 |

Questions only (Clarify and Unblocker turns): p50 3198 ms, p95 5284 ms, n 20.
Read by the decision model: 26 of 29 turns; the rest used the rules.
API-reported session p50 per run: 3229, 2925, 3242 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 15, 15, 15 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
