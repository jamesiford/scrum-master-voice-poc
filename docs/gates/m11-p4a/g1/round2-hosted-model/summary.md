# Gate runner summary

### round2-hosted-model (build d822389, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3119 | 4504 | 36 |
| Speech end to transcript | 0 | 170 | 36 |
| Analyser (rules and grounding) | 26 | 31 | 36 |
| Decision model call | 152 | 274 | 36 |
| Orchestration (floor, silence, reconcile) | 1460 | 1748 | 36 |
| Voice Live first audio | 1479 | 3152 | 36 |

Questions only (Clarify and Unblocker turns): p50 3243 ms, p95 3867 ms, n 27.
Read by the decision model: 30 of 36 turns; the rest used the rules.
API-reported session p50 per run: 2792, 3098, 3404 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
