# Gate runner summary

### round1-direct-model (build e37ef09, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3544 | 4686 | 11 |
| Speech end to transcript | 0 | 210 | 11 |
| Analyser (rules and grounding) | 27 | 52 | 11 |
| Decision model call | 181 | 316 | 11 |
| Orchestration (floor, silence, reconcile) | 1790 | 2526 | 11 |
| Voice Live first audio | 1678 | 2261 | 11 |

Questions only (Clarify and Unblocker turns): p50 3544 ms, p95 4686 ms, n 8.
Read by the decision model: 9 of 11 turns; the rest used the rules.
API-reported session p50 per run: 3544 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 17 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
