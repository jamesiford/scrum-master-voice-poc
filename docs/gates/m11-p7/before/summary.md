# Gate runner summary

### p7-before (build source-6b3c735-deployed-m11-6b-1b7a614, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3230 | 3958 | 12 |
| Speech end to transcript | 0 | 192 | 12 |
| Analyser (rules and grounding) | 27 | 27 | 12 |
| Decision model call | 152 | 167 | 12 |
| Orchestration (floor, silence, reconcile) | 1520 | 1967 | 12 |
| Voice Live first audio | 1564 | 2292 | 12 |

Questions only (Clarify and Unblocker turns): p50 3350 ms, p95 3958 ms, n 9.
Read by the decision model: 10 of 12 turns; the rest used the rules.
API-reported session p50 per run: 3230 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
