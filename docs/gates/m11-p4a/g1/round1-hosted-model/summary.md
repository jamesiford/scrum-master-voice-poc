# Gate runner summary

### round1-hosted-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3235 | 6288 | 35 |
| Speech end to transcript | 0 | 198 | 35 |
| Analyser (rules and grounding) | 26 | 37 | 35 |
| Decision model call | 156 | 269 | 35 |
| Orchestration (floor, silence, reconcile) | 1555 | 2047 | 35 |
| Voice Live first audio | 1506 | 4547 | 35 |

Questions only (Clarify and Unblocker turns): p50 3295 ms, p95 6087 ms, n 26.
Read by the decision model: 29 of 35 turns; the rest used the rules.
API-reported session p50 per run: 3295, 3164, 3235 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 17, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
