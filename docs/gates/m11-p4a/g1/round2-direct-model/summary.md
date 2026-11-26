# Gate runner summary

### round2-direct-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3063 | 5225 | 34 |
| Speech end to transcript | 0 | 200 | 34 |
| Analyser (rules and grounding) | 27 | 33 | 34 |
| Decision model call | 158 | 231 | 34 |
| Orchestration (floor, silence, reconcile) | 1549 | 1999 | 34 |
| Voice Live first audio | 1467 | 3404 | 34 |

Questions only (Clarify and Unblocker turns): p50 3307 ms, p95 5225 ms, n 25.
Read by the decision model: 29 of 34 turns; the rest used the rules.
API-reported session p50 per run: 2988, 3063, 3168 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 15 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
