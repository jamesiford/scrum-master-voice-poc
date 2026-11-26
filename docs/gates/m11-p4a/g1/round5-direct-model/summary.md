# Gate runner summary

### round5-direct-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3171 | 4153 | 36 |
| Speech end to transcript | 0 | 194 | 36 |
| Analyser (rules and grounding) | 26 | 42 | 36 |
| Decision model call | 156 | 287 | 36 |
| Orchestration (floor, silence, reconcile) | 1535 | 2118 | 36 |
| Voice Live first audio | 1262 | 2608 | 36 |

Questions only (Clarify and Unblocker turns): p50 3345 ms, p95 4153 ms, n 27.
Read by the decision model: 32 of 36 turns; the rest used the rules.
API-reported session p50 per run: 3080, 3131, 3582 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
