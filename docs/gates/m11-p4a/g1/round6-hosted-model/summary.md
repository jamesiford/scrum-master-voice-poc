# Gate runner summary

### round6-hosted-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3256 | 4349 | 33 |
| Speech end to transcript | 0 | 192 | 33 |
| Analyser (rules and grounding) | 27 | 43 | 33 |
| Decision model call | 160 | 249 | 33 |
| Orchestration (floor, silence, reconcile) | 1693 | 2759 | 33 |
| Voice Live first audio | 1307 | 2525 | 33 |

Questions only (Clarify and Unblocker turns): p50 3583 ms, p95 4349 ms, n 24.
Read by the decision model: 28 of 33 turns; the rest used the rules.
API-reported session p50 per run: 3020, 3323, 3033 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 15 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
