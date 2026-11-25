# Gate runner summary

### rt21-model (build fe5f0e4, 6 runs)

| Hop | p50 ms | p95 ms | n |
| --- | ---: | ---: | ---: |
| Total (speech end to first audio) | 4192 | 7267 | 72 |
| Speech end to transcript | 0 | 195 | 72 |
| Analyser (rules and grounding) | 47 | 186 | 72 |
| Decision model call | 164 | 250 | 72 |
| Orchestration (floor, silence, reconcile) | 2163 | 3177 | 72 |
| Voice Live first audio | 1844 | 3278 | 72 |

Questions only (Clarify and Unblocker turns): p50 4254 ms, p95 7550 ms, n 54.
Read by the decision model: 60 of 72 turns; the rest used the rules.
API-reported session p50 per run: 3916, 3804, 3983, 4271, 4706, 4216 ms.
Agent spoke over a person (talk-overs): 0 in 6 runs. People's transcript pieces: 19, 19, 19, 19, 19, 19 per run.

Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.

