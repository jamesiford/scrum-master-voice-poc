# Gate runner summary

### round3-hosted-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3211 | 5370 | 34 |
| Speech end to transcript | 0 | 191 | 34 |
| Analyser (rules and grounding) | 26 | 44 | 34 |
| Decision model call | 154 | 217 | 34 |
| Orchestration (floor, silence, reconcile) | 1551 | 2378 | 34 |
| Voice Live first audio | 1529 | 3134 | 34 |

Questions only (Clarify and Unblocker turns): p50 3399 ms, p95 5370 ms, n 25.
Read by the decision model: 29 of 34 turns; the rest used the rules.
API-reported session p50 per run: 3123, 3233, 3189 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 15, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
