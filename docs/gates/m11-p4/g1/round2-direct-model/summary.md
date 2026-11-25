# Gate runner summary

### round2-direct-model (build e37ef09, 1 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 3537 | 4720 | 12 |
| Speech end to transcript | 0 | 199 | 12 |
| Analyser (rules and grounding) | 30 | 105 | 12 |
| Decision model call | 163 | 408 | 12 |
| Orchestration (floor, silence, reconcile) | 1567 | 2069 | 12 |
| Voice Live first audio | 1517 | 2993 | 12 |

Questions only (Clarify and Unblocker turns): p50 3556 ms, p95 4720 ms, n 9.
Read by the decision model: 10 of 12 turns; the rest used the rules.
API-reported session p50 per run: 3537 ms.
Agent spoke over a person (talk-overs): 0 in 1 runs. People's transcript pieces: 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.
