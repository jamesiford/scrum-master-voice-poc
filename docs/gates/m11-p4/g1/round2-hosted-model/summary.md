# Gate runner summary

### round2-hosted-model (build e37ef09, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3167 | 5244 | 12 |
| Speech end to transcript | 0 | 269 | 12 |
| Analyser (rules and grounding) | 27 | 95 | 12 |
| Decision model call | 158 | 337 | 12 |
| Orchestration (floor, silence, reconcile) | 1543 | 3000 | 12 |
| Voice Live first audio | 1499 | 3146 | 12 |

Questions only (Clarify and Unblocker turns): p50 3355 ms, p95 5244 ms, n 9.
Read by the decision model: 10 of 12 turns; the rest used the rules.
API-reported session p50 per run: 3167 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
