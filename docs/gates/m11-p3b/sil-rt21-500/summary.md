# Gate runner summary

### sil-rt21-500 (build fe5f0e4, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3633 | 4727 | 30 |
| Speech end to transcript | 0 | 203 | 30 |
| Analyser (rules and grounding) | 31 | 65 | 30 |
| Decision model call | 160 | 251 | 30 |
| Orchestration (floor, silence, reconcile) | 1767 | 2919 | 30 |
| Voice Live first audio | 1504 | 2821 | 30 |

Questions only (Clarify and Unblocker turns): p50 3682 ms, p95 4727 ms, n 23.
Read by the decision model: 23 of 30 turns; the rest used the rules.
API-reported session p50 per run: 3274, 3652, 3333 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 13, 17, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

