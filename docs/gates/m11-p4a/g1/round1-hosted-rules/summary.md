# Gate runner summary

### round1-hosted-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3028 | 4246 | 36 |
| Speech end to transcript | 0 | 198 | 36 |
| Analyser (rules and grounding) | 26 | 35 | 36 |
| Decision model call | 0 | 0 | 36 |
| Orchestration (floor, silence, reconcile) | 1528 | 2039 | 36 |
| Voice Live first audio | 1573 | 2479 | 36 |

Questions only (Clarify and Unblocker turns): p50 3287 ms, p95 4246 ms, n 27.
Read by the decision model: 0 of 36 turns; the rest used the rules.
API-reported session p50 per run: 2862, 3028, 3115 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
