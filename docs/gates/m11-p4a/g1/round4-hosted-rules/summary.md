# Gate runner summary

### round4-hosted-rules (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 2948 | 4864 | 34 |
| Speech end to transcript | 0 | 209 | 34 |
| Analyser (rules and grounding) | 25 | 30 | 34 |
| Decision model call | 0 | 0 | 34 |
| Orchestration (floor, silence, reconcile) | 1592 | 2689 | 34 |
| Voice Live first audio | 1317 | 2801 | 34 |

Questions only (Clarify and Unblocker turns): p50 3018 ms, p95 4864 ms, n 25.
Read by the decision model: 0 of 34 turns; the rest used the rules.
API-reported session p50 per run: 2582, 2994, 3315 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 17 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
