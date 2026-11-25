# Gate runner summary

### sil-rt21-800b (build fe5f0e4, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3639 | 4990 | 33 |
| Speech end to transcript | 0 | 205 | 33 |
| Analyser (rules and grounding) | 32 | 63 | 33 |
| Decision model call | 160 | 400 | 33 |
| Orchestration (floor, silence, reconcile) | 1888 | 3126 | 33 |
| Voice Live first audio | 1571 | 2808 | 33 |

Questions only (Clarify and Unblocker turns): p50 3682 ms, p95 4990 ms, n 24.
Read by the decision model: 28 of 33 turns; the rest used the rules.
API-reported session p50 per run: 3442, 3715, 3559 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 15, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

