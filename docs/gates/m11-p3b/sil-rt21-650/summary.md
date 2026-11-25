# Gate runner summary

### sil-rt21-650 (build fe5f0e4, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3494 | 5536 | 32 |
| Speech end to transcript | 0 | 202 | 32 |
| Analyser (rules and grounding) | 26 | 39 | 32 |
| Decision model call | 165 | 256 | 32 |
| Orchestration (floor, silence, reconcile) | 1766 | 3696 | 32 |
| Voice Live first audio | 1462 | 3500 | 32 |

Questions only (Clarify and Unblocker turns): p50 3501 ms, p95 5536 ms, n 25.
Read by the decision model: 24 of 32 turns; the rest used the rules.
API-reported session p50 per run: 3501, 3316, 3635 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 18, 16 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

