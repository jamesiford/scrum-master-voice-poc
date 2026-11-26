# Gate runner summary

### round1-direct-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3111 | 3976 | 32 |
| Speech end to transcript | 0 | 198 | 32 |
| Analyser (rules and grounding) | 26 | 27 | 32 |
| Decision model call | 160 | 248 | 32 |
| Orchestration (floor, silence, reconcile) | 1510 | 1886 | 32 |
| Voice Live first audio | 1456 | 2128 | 32 |

Questions only (Clarify and Unblocker turns): p50 3427 ms, p95 3842 ms, n 23.
Read by the decision model: 27 of 32 turns; the rest used the rules.
API-reported session p50 per run: 3111, 3541, 3056 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 17, 15, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
