# Gate runner summary

### round5-direct-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2990 | 4880 | 32 |
| Speech end to transcript | 0 | 166 | 32 |
| Analyser (rules and grounding) | 25 | 45 | 32 |
| Decision model call | 0 | 0 | 32 |
| Orchestration (floor, silence, reconcile) | 1625 | 3195 | 32 |
| Voice Live first audio | 1312 | 2892 | 32 |

Questions only (Clarify and Unblocker turns): p50 3054 ms, p95 4880 ms, n 23.
Read by the decision model: 0 of 32 turns; the rest used the rules.
API-reported session p50 per run: 3054, 2973, 2974 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 15, 15, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
