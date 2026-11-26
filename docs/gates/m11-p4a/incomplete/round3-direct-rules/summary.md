# Gate runner summary

### round3-direct-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2873 | 4508 | 35 |
| Speech end to transcript | 0 | 203 | 35 |
| Analyser (rules and grounding) | 26 | 39 | 35 |
| Decision model call | 0 | 0 | 35 |
| Orchestration (floor, silence, reconcile) | 1580 | 2602 | 35 |
| Voice Live first audio | 1308 | 2413 | 35 |

Questions only (Clarify and Unblocker turns): p50 2904 ms, p95 4508 ms, n 26.
Read by the decision model: 0 of 35 turns; the rest used the rules.
API-reported session p50 per run: 2928, 2839, 2904 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 17 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
