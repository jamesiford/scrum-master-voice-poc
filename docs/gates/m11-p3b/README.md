# M11 prompt 3B: realtime model and silence measurements

Evidence for ADR 0019. Environment: `zavaw3` (westus3), build deployed from commit 0f21ec3 (the harness runs report the checked-out commit, fe5f0e4, which differs only in docs). Voice: Azure TTS `en-US-Ava:DragonHDLatestNeural` for every candidate. Only `VoiceLive__Model`, `UtteranceAnalysis__Mode`, `Voice__SilenceMs` and the bridge cast file changed between runs (`bench.ps1`).

Method: three Simulated Room standups run at the same time per batch (one per team, `GateRunner --teams`), two batches per candidate configuration, so six standups per configuration. Percentiles are nearest-rank. A "question" is a Clarify or Unblocker turn.

## Latency per hop (p50 / p95 ms)

| Configuration | Standups | Total p50 / p95 | Questions only p50 / p95 (n) | Voice Live first audio p50 / p95 | Orchestration p50 / p95 | Decision model p50 |
| --- | ---: | --- | --- | --- | --- | ---: |
| gpt-realtime-2.1, rules only | 12 | 3301 / 4288 | see `rt21-rules/summary.md` | 1458 / 2404 | 1873 / 2541 | 0 |
| gpt-realtime-2.1-mini, rules only | 12 | 3342 / 4712 | see `mini-rules/summary.md` | 1370 / 2271 | 1937 / 2821 | 0 |
| gpt-realtime-2.1, decision model on | 6 | 4192 / 7267 | 4254 / 7550 (54) | 1844 / 3278 | 2163 / 3177 | 164 |
| gpt-realtime-2.1-mini, decision model on | 6 | 4253 / 6456 | 4414 / 6456 (33) | 1523 / 2687 | 2446 / 4508 | 156 |

The rules-only batches ran first in the session and the decision-model batches about an hour later. A repeat of the rules-only configuration at the end (`rt21-rules-ctrl`, 3 standups) gave 4135 / 5803, against 3301 / 4288 earlier, so the same configuration moved by about 25 % between time windows. **Differences under about 25 % between rows run at different times cannot be attributed to the setting.** Rows run next to each other (2.1 against mini in the same mode) are the fair comparison, and there the models are within 100 ms at p50.

## Quality checks (transcript read, not listening)

`quality-checks.py` reads the saved transcripts of all 18 standups per model (rules and decision-model batches together). I did not listen to the audio; a person should listen to about 20 turns of each model before this is treated as the quality sign-off.

| Check (18 standups per model) | gpt-realtime-2.1 | gpt-realtime-2.1-mini |
| --- | ---: | ---: |
| Agent turns | 269 | 247 |
| Timed reactive turns completed | 215 | 139 |
| Flagged items the agent raised with their owner (of 144) | 125 (87 %) | 104 (72 %) |
| Agent turn that repeats a sentence inside itself | 0 | 2 |
| Agent turn identical to the one before | 0 | 6 |

- The mini model skipped flagged items. In one standup it never asked Daniel about story 3, task 15 or task 19 and moved on after one answer; the same cast and data produced all three questions on 2.1.
- It repeated itself ("Thanks, Daniel." twice in a row) 6 times.
- It completed 139 timed turns against 215: its standups ended earlier, which fits it asking fewer questions (I did not trace each one). A lower latency per turn would not make that better.
- **A correction to my first reading.** Lines such as "Two standups ago, you listed continuing the settlement file validation" looked invented. They are not: the carry-over feature supplies earlier commitments from the previous standups of the same team, and the earlier harness runs created them. Both models do this equally (3 each), so it is not counted against either.
- Not checked: wording against each turn instruction, and names (the cast names are simple). No truncation of a long multi-item update was seen on either model in the transcripts, but the transcript of what the agent says back is not the audio.

## Silence setting

Model gpt-realtime-2.1, decision model on, pause variant of the cast (`demo/cast/payments-standup-pauses.yaml`): the three opening updates hold mid-sentence pauses of 550 and 700 ms (500 ms would split both, 650 ms the 700 ms ones, 800 ms none). One batch of three standups per value.

| `Voice__SilenceMs` | Total p50 / p95 ms (n) | Times the agent spoke over a person | People's transcript pieces per standup |
| ---: | --- | ---: | --- |
| 800 | 3423 / 4535 (34) | 0 | 15, 19, 19 |
| 650 | 3494 / 5536 (32) | 0 | 19, 18, 16 |
| 500 | 3633 / 4727 (30) | 0 | 13, 17, 19 |
| 800 (repeat, last) | 3639 / 4990 (33) | 0 | 19, 15, 19 |

Nothing separates the values. No setting made the agent talk over anyone, and in the 500 ms transcripts I read the openings stayed in one piece even with 700 ms pauses, so the voice service's own end-of-turn detection holds through pauses of that length and the test cannot tell the settings apart. The lower piece counts are standups that ended before every cast line was spoken, not fragmentation. The 800 ms repeat differs from the first 800 ms batch by about 6 %, which is larger than any difference between the settings.

## Things that went wrong on the way

- The first four batches were labelled "decision" but ran rules-only, because `bench.ps1` set `UtteranceAnalysis__Mode=Decision`; the valid values are `Model` and `Rules`. They are kept as additional rules-only batches (`*-rules`, files ending `-batchA`). The decision-model batches were then run correctly (`*-model`).
- A control batch of the default configuration failed twice: the first time the standups ended early, the second time one standup never closed and the harness timed out after 25 minutes. Neither is in the tables.
- **Scale limit found.** The sessions of every team are one Cosmos item (`sessions.json`, the PoC data model). After a few dozen standups with transcripts (this session ran about 45, on top of earlier ones) it passed the 2 MB item limit (`RequestEntityTooLarge`, 413) and `PrepareStandupCommand` returned 500. `POST /api/demo/reset` cleared it. This is a real limit for many teams and the production answer is one item per session (`// PROD:` in SPEC 15). It is not fixed here.
