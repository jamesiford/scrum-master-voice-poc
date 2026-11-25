# Gate runner summary

### rt21-rules-ctrl (build fe5f0e4, 3 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 4135 | 5803 | 36 |
| Speech end to transcript | 0 | 216 | 36 |
| Analyser (rules and grounding) | 66 | 631 | 36 |
| Decision model call | 0 | 0 | 36 |
| Orchestration (floor, silence, reconcile) | 2681 | 4086 | 36 |
| Voice Live first audio | 1669 | 2664 | 36 |

Questions only (Clarify and Unblocker turns): p50 4436 ms, p95 5803 ms, n 27.
Read by the decision model: 0 of 36 turns; the rest used the rules.
API-reported session p50 per run: 3782, 4436, 4135 ms.
Agent spoke over a person (talk-overs): 0 in 3 runs. People's transcript pieces: 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

