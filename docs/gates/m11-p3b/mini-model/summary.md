# Gate runner summary

### mini-model (build fe5f0e4, 6 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 4253 | 6456 | 47 |
| Speech end to transcript | 0 | 230 | 47 |
| Analyser (rules and grounding) | 57 | 448 | 47 |
| Decision model call | 156 | 277 | 47 |
| Orchestration (floor, silence, reconcile) | 2446 | 4508 | 47 |
| Voice Live first audio | 1523 | 2687 | 47 |

Questions only (Clarify and Unblocker turns): p50 4414 ms, p95 6456 ms, n 33.
Read by the decision model: 43 of 47 turns; the rest used the rules.
API-reported session p50 per run: 4032, 4127, 3467, 4636, 5882, 4476 ms.
Agent spoke over a person (talk-overs): 0 in 6 runs. People's transcript pieces: 14, 13, 13, 13, 14, 12 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

