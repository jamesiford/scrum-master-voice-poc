# Gate runner summary

### round6-direct-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3055 | 5270 | 34 |
| Speech end to transcript | 0 | 200 | 34 |
| Analyser (rules and grounding) | 25 | 38 | 34 |
| Decision model call | 0 | 0 | 34 |
| Orchestration (floor, silence, reconcile) | 1576 | 2350 | 34 |
| Voice Live first audio | 1364 | 3276 | 34 |

Questions only (Clarify and Unblocker turns): p50 3143 ms, p95 5270 ms, n 25.
Read by the decision model: 0 of 34 turns; the rest used the rules.
API-reported session p50 per run: 3027, 3056, 2964 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 15 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
