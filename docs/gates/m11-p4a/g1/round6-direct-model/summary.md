# Gate runner summary

### round6-direct-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3186 | 4269 | 36 |
| Speech end to transcript | 0 | 197 | 36 |
| Analyser (rules and grounding) | 26 | 44 | 36 |
| Decision model call | 157 | 238 | 36 |
| Orchestration (floor, silence, reconcile) | 1627 | 1843 | 36 |
| Voice Live first audio | 1389 | 2829 | 36 |

Questions only (Clarify and Unblocker turns): p50 3293 ms, p95 4269 ms, n 27.
Read by the decision model: 30 of 36 turns; the rest used the rules.
API-reported session p50 per run: 2985, 3291, 3186 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
