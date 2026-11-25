# Gate runner summary

### round1-hosted-rules (build e37ef09, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3405 | 3878 | 11 |
| Speech end to transcript | 0 | 209 | 11 |
| Analyser (rules and grounding) | 29 | 78 | 11 |
| Decision model call | 0 | 0 | 11 |
| Orchestration (floor, silence, reconcile) | 1633 | 2309 | 11 |
| Voice Live first audio | 1505 | 1982 | 11 |

Questions only (Clarify and Unblocker turns): p50 3405 ms, p95 3878 ms, n 8.
Read by the decision model: 0 of 11 turns; the rest used the rules.
API-reported session p50 per run: 3405 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 17 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
