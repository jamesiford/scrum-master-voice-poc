# Gate runner summary

### decision (build p3-wip, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3071 | 4755 | 32 |
| Speech end to transcript | 0 | 169 | 32 |
| Analyser (rules and grounding) | 25 | 26 | 32 |
| Decision model call | 154 | 271 | 32 |
| Orchestration (floor, silence, reconcile) | 1560 | 1913 | 32 |
| Voice Live first audio | 1524 | 2622 | 32 |

Questions only (Clarify and Unblocker turns): p50 3124 ms, p95 4133 ms, n 23.
Read by the decision model: 26 of 32 turns; the rest used the rules.
API-reported session p50 per run: 2812, 3215, 3124 ms.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
