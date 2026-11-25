# Gate runner summary

### sil-rt21-800 (build fe5f0e4, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3423 | 4535 | 34 |
| Speech end to transcript | 0 | 226 | 34 |
| Analyser (rules and grounding) | 24 | 40 | 34 |
| Decision model call | 170 | 238 | 34 |
| Orchestration (floor, silence, reconcile) | 1662 | 2152 | 34 |
| Voice Live first audio | 1383 | 2764 | 34 |

Questions only (Clarify and Unblocker turns): p50 3635 ms, p95 4504 ms, n 25.
Read by the decision model: 28 of 34 turns; the rest used the rules.
API-reported session p50 per run: 3516, 3293, 3362 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 15, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

