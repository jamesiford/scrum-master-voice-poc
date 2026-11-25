# Gate runner summary

### round3-direct-model (build e37ef09, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3261 | 5492 | 12 |
| Speech end to transcript | 0 | 198 | 12 |
| Analyser (rules and grounding) | 26 | 42 | 12 |
| Decision model call | 167 | 400 | 12 |
| Orchestration (floor, silence, reconcile) | 1631 | 1983 | 12 |
| Voice Live first audio | 1498 | 3279 | 12 |

Questions only (Clarify and Unblocker turns): p50 3315 ms, p95 5492 ms, n 9.
Read by the decision model: 10 of 12 turns; the rest used the rules.
API-reported session p50 per run: 3261 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
