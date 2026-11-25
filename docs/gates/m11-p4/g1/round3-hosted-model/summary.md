# Gate runner summary

### round3-hosted-model (build e37ef09, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3479 | 5823 | 12 |
| Speech end to transcript | 0 | 220 | 12 |
| Analyser (rules and grounding) | 27 | 46 | 12 |
| Decision model call | 204 | 314 | 12 |
| Orchestration (floor, silence, reconcile) | 1768 | 4311 | 12 |
| Voice Live first audio | 1417 | 2539 | 12 |

Questions only (Clarify and Unblocker turns): p50 3667 ms, p95 5823 ms, n 9.
Read by the decision model: 10 of 12 turns; the rest used the rules.
API-reported session p50 per run: 3479 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
