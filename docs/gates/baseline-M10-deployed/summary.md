# Gate runner summary

### decision (build 84498ad, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2994 | 4081 | 36 |
| Speech end to transcript | 0 | 214 | 36 |
| Analyser (rules and grounding) | 25 | 28 | 36 |
| Decision model call | 147 | 242 | 36 |
| Orchestration (floor, silence, reconcile) | 1446 | 1889 | 36 |
| Voice Live first audio | 1440 | 2584 | 36 |

Questions only (Clarify and Unblocker turns): p50 3050 ms, p95 4081 ms, n 27.
Read by the decision model: 30 of 36 turns; the rest used the rules.
API-reported session p50 per run: 2979, 2904, 3050 ms.

### rules (build 84498ad, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2828 | 3876 | 36 |
| Speech end to transcript | 0 | 189 | 36 |
| Analyser (rules and grounding) | 29 | 39 | 36 |
| Decision model call | 0 | 0 | 36 |
| Orchestration (floor, silence, reconcile) | 1580 | 2042 | 36 |
| Voice Live first audio | 1381 | 1894 | 36 |

Questions only (Clarify and Unblocker turns): p50 2944 ms, p95 3876 ms, n 27.
Read by the decision model: 0 of 36 turns; the rest used the rules.
API-reported session p50 per run: 2828, 2791, 2944 ms.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
