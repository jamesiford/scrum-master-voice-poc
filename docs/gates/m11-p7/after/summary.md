# Gate runner summary

### verify (build 0d07e83, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3338 | 5508 | 12 |
| Speech end to transcript | 0 | 198 | 12 |
| Analyser (rules and grounding) | 27 | 40 | 12 |
| Decision model call | 144 | 236 | 12 |
| Orchestration (floor, silence, reconcile) | 1576 | 3798 | 12 |
| Voice Live first audio | 1459 | 1908 | 12 |

Questions only (Clarify and Unblocker turns): p50 3642 ms, p95 5508 ms, n 9.
Read by the decision model: 10 of 12 turns; the rest used the rules.
API-reported session p50 per run: 3338 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
