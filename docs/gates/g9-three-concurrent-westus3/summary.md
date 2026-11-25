# Gate runner summary

### g9 (build c3083b7, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3400 | 4919 | 36 |
| Speech end to transcript | 0 | 216 | 36 |
| Analyser (rules and grounding) | 25 | 46 | 36 |
| Decision model call | 164 | 236 | 36 |
| Orchestration (floor, silence, reconcile) | 1700 | 2357 | 36 |
| Voice Live first audio | 1443 | 3024 | 36 |

Questions only (Clarify and Unblocker turns): p50 3523 ms, p95 4919 ms, n 27.
Read by the decision model: 30 of 36 turns; the rest used the rules.
API-reported session p50 per run: 3544, 3262, 3492 ms.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
