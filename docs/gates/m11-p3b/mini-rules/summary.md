# Gate runner summary

### mini-rules (build fe5f0e4, 12 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3342 | 4712 | 92 |
| Speech end to transcript | 0 | 233 | 92 |
| Analyser (rules and grounding) | 39 | 87 | 92 |
| Decision model call | 0 | 0 | 92 |
| Orchestration (floor, silence, reconcile) | 1937 | 2821 | 92 |
| Voice Live first audio | 1370 | 2271 | 92 |

Questions only (Clarify and Unblocker turns): p50 3457 ms, p95 4629 ms, n 67.
Read by the decision model: 0 of 92 turns; the rest used the rules.
API-reported session p50 per run: 3115, 3457, 3515, 2858, 3212, 3200, 3254, 3284, 3492, 3177, 3745, 3635 ms.
Agent spoke over a person (talk-overs): 0 in 12 runs. People's transcript pieces: 12, 14, 14, 17, 12, 12, 12, 13, 13, 12, 13, 13 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

