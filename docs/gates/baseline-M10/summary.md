# Gate runner summary

### decision (build 84498ad, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3827 | 4907 | 35 |
| Speech end to transcript | 0 | 202 | 35 |
| Analyser (rules and grounding) | 24 | 36 | 35 |
| Decision model call | 282 | 393 | 35 |
| Orchestration (floor, silence, reconcile) | 1520 | 2440 | 35 |
| Voice Live first audio | 1862 | 3362 | 35 |

Questions only (Clarify and Unblocker turns): p50 3850 ms, p95 4907 ms, n 26.
Read by the decision model: 29 of 35 turns; the rest used the rules.
API-reported session p50 per run: 3995, 3357, 3644 ms.

### rules (build 84498ad, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3116 | 3905 | 35 |
| Speech end to transcript | 0 | 202 | 35 |
| Analyser (rules and grounding) | 21 | 27 | 35 |
| Decision model call | 0 | 0 | 35 |
| Orchestration (floor, silence, reconcile) | 1614 | 2023 | 35 |
| Voice Live first audio | 1582 | 2400 | 35 |

Questions only (Clarify and Unblocker turns): p50 3173 ms, p95 3905 ms, n 26.
Read by the decision model: 0 of 35 turns; the rest used the rules.
API-reported session p50 per run: 3166, 3116, 3072 ms.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

