# Gate runner summary

### round3-direct-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3172 | 4604 | 36 |
| Speech end to transcript | 0 | 208 | 36 |
| Analyser (rules and grounding) | 26 | 29 | 36 |
| Decision model call | 158 | 251 | 36 |
| Orchestration (floor, silence, reconcile) | 1547 | 2146 | 36 |
| Voice Live first audio | 1339 | 2685 | 36 |

Questions only (Clarify and Unblocker turns): p50 3270 ms, p95 4604 ms, n 27.
Read by the decision model: 30 of 36 turns; the rest used the rules.
API-reported session p50 per run: 3110, 3211, 3079 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
