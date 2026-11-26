# Gate runner summary

### round4-direct-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3271 | 4324 | 34 |
| Speech end to transcript | 0 | 201 | 34 |
| Analyser (rules and grounding) | 26 | 53 | 34 |
| Decision model call | 159 | 236 | 34 |
| Orchestration (floor, silence, reconcile) | 1586 | 2382 | 34 |
| Voice Live first audio | 1424 | 2284 | 34 |

Questions only (Clarify and Unblocker turns): p50 3366 ms, p95 4324 ms, n 25.
Read by the decision model: 29 of 34 turns; the rest used the rules.
API-reported session p50 per run: 3242, 2930, 3454 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 15 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
