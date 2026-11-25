# Gate runner summary

### round1-hosted-model (build e37ef09, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3368 | 5679 | 12 |
| Speech end to transcript | 0 | 257 | 12 |
| Analyser (rules and grounding) | 27 | 148 | 12 |
| Decision model call | 204 | 259 | 12 |
| Orchestration (floor, silence, reconcile) | 1649 | 3880 | 12 |
| Voice Live first audio | 1447 | 3703 | 12 |

Questions only (Clarify and Unblocker turns): p50 3368 ms, p95 5679 ms, n 9.
Read by the decision model: 10 of 12 turns; the rest used the rules.
API-reported session p50 per run: 3368 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
