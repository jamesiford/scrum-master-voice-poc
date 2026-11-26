# Gate runner summary

### round2-direct-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2979 | 4604 | 33 |
| Speech end to transcript | 0 | 195 | 33 |
| Analyser (rules and grounding) | 26 | 58 | 33 |
| Decision model call | 0 | 0 | 33 |
| Orchestration (floor, silence, reconcile) | 1534 | 2455 | 33 |
| Voice Live first audio | 1290 | 2214 | 33 |

Questions only (Clarify and Unblocker turns): p50 3034 ms, p95 4604 ms, n 25.
Read by the decision model: 0 of 33 turns; the rest used the rules.
API-reported session p50 per run: 2738, 3156, 2979 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 18, 17, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
