# Decision evaluation

Authored synthetic cases: 72; team `team-evaluation`; SHA256 `6f2e250500ada5c9d317657e981c4fe6ceeb84056a3174f77f58ebd35c5991d8`.
Model: Microsoft-Decision-1.1 / `zava-decision`. Binary threshold 0.7; intent threshold 0.6.
Accuracy is exact per utterance, not inflated by the many negative binary labels.

| Path | n | Intent | Items | Comment kinds | Dates | Escaped IDs |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Rules | 72 | 95.8% | 97.2% | 55.6% | 0.0% (n8) | 0 |
| Decision + grounding | 72 | 98.6% | 98.6% | 95.8% | 75.0% (n8) | 0 |

Model failures: 0; rules fallbacks: 4; stop reason: none.
0.7 accepted precision: 94.5%, n=73;
Brier 0.0298, ECE 0.0750, missing=0.
0.6 accepted intent precision: 98.5%, n=68;
Brier 0.0415, ECE 0.1109, missing=0.
Model candidate ids rejected: 0/36.
Explicit outside-sprint challenge mentions: 3; escaped ids: 0.

Release check: FAIL. CI reporting is non-blocking; `--release-check` exits non-zero on failure or absent model evidence.
Rules intentionally emit no comment kinds or probabilities. Their absent capability is scored, not filled in with a stand-in.
Knowledge, Work IQ and live Foundry content evaluations: not_run, n=0.