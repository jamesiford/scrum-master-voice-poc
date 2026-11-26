# Gate runner summary

### round6-hosted-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2976 | 4130 | 36 |
| Speech end to transcript | 0 | 205 | 36 |
| Analyser (rules and grounding) | 26 | 44 | 36 |
| Decision model call | 0 | 0 | 36 |
| Orchestration (floor, silence, reconcile) | 1560 | 2187 | 36 |
| Voice Live first audio | 1403 | 2347 | 36 |

Questions only (Clarify and Unblocker turns): p50 3113 ms, p95 4130 ms, n 27.
Read by the decision model: 0 of 36 turns; the rest used the rules.
API-reported session p50 per run: 2976, 2828, 2902 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
