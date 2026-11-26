# Gate runner summary

### round4-hosted-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3287 | 4396 | 36 |
| Speech end to transcript | 0 | 198 | 36 |
| Analyser (rules and grounding) | 27 | 59 | 36 |
| Decision model call | 155 | 317 | 36 |
| Orchestration (floor, silence, reconcile) | 1634 | 2046 | 36 |
| Voice Live first audio | 1506 | 2433 | 36 |

Questions only (Clarify and Unblocker turns): p50 3382 ms, p95 4396 ms, n 27.
Read by the decision model: 30 of 36 turns; the rest used the rules.
API-reported session p50 per run: 3354, 3094, 3142 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
