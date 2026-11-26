# Gate runner summary

### round3-direct-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2827 | 4145 | 36 |
| Speech end to transcript | 0 | 194 | 36 |
| Analyser (rules and grounding) | 26 | 54 | 36 |
| Decision model call | 0 | 0 | 36 |
| Orchestration (floor, silence, reconcile) | 1511 | 2311 | 36 |
| Voice Live first audio | 1237 | 2401 | 36 |

Questions only (Clarify and Unblocker turns): p50 2983 ms, p95 4145 ms, n 27.
Read by the decision model: 0 of 36 turns; the rest used the rules.
API-reported session p50 per run: 2703, 2983, 2846 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
