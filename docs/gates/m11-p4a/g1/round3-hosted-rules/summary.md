# Gate runner summary

### round3-hosted-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3032 | 5733 | 35 |
| Speech end to transcript | 0 | 197 | 35 |
| Analyser (rules and grounding) | 25 | 38 | 35 |
| Decision model call | 0 | 0 | 35 |
| Orchestration (floor, silence, reconcile) | 1541 | 3874 | 35 |
| Voice Live first audio | 1530 | 2453 | 35 |

Questions only (Clarify and Unblocker turns): p50 3166 ms, p95 5733 ms, n 26.
Read by the decision model: 0 of 35 turns; the rest used the rules.
API-reported session p50 per run: 3041, 2954, 3104 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 17 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
