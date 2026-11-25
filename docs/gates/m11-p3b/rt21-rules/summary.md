# Gate runner summary

### rt21-rules (build fe5f0e4, 12 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3301 | 4288 | 143 |
| Speech end to transcript | 0 | 202 | 143 |
| Analyser (rules and grounding) | 35 | 85 | 143 |
| Decision model call | 0 | 0 | 143 |
| Orchestration (floor, silence, reconcile) | 1873 | 2541 | 143 |
| Voice Live first audio | 1458 | 2404 | 143 |

Questions only (Clarify and Unblocker turns): p50 3426 ms, p95 4603 ms, n 107.
Read by the decision model: 0 of 143 turns; the rest used the rules.
API-reported session p50 per run: 3189, 3142, 3033, 3235, 3195, 3021, 3159, 3307, 3327, 3301, 3705, 3547 ms.
Agent spoke over a person (talk-overs): 0 in 12 runs. People's transcript pieces: 19, 19, 19, 19, 19, 19, 19, 19, 19, 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

