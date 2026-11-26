# Gate runner summary

### round3-hosted-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3114 | 4522 | 34 |
| Speech end to transcript | 0 | 187 | 34 |
| Analyser (rules and grounding) | 26 | 37 | 34 |
| Decision model call | 157 | 367 | 34 |
| Orchestration (floor, silence, reconcile) | 1504 | 1911 | 34 |
| Voice Live first audio | 1326 | 3036 | 34 |

Questions only (Clarify and Unblocker turns): p50 3169 ms, p95 4388 ms, n 25.
Read by the decision model: 29 of 34 turns; the rest used the rules.
API-reported session p50 per run: 3143, 3036, 3114 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 15 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
