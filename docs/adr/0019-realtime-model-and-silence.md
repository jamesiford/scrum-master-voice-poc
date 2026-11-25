# 0019. Realtime model and silence

Status: **accepted, 25 November 2026**. Built in M11 prompt 3B. Evidence: `docs/gates/m11-p3b/` (read its README first).

## Context

Voice Live runs `gpt-realtime-2.1` with a 800 ms silence setting. The brief asked whether `gpt-realtime-2.1-mini` is better, whether a shorter silence setting is safe, and to remove what nothing uses. `gpt-live-1` was excluded up front: it is a separate full-duplex API surface, not a Voice Live model, and floor control already owns turn-taking.

**Checked against Microsoft Learn (Voice Live overview, 25 November 2026).** `gpt-realtime-2.1` (Pro tier) and `gpt-realtime-2.1-mini` (Standard tier) are both listed as Voice Live models and both support Azure text to speech voices. The page also says natively supported models are "fully managed, so you don't need to deploy" them. The repository deploys the model anyway in the Foundry account, which this work did not change; whether the deployment is needed for Voice Live is not established here. Azure OpenAI audio costs about 10 tokens a second in and 20 out, which matches the cost estimator's 600 and 1,200 a minute. The model catalogue in westus3 offers both models as GlobalStandard (checked with `az cognitiveservices model list`).

## Decisions

1. **Keep `gpt-realtime-2.1`.** The mini model did not beat it on latency and was worse on behaviour. Next to each other, in the same mode, the p50 differed by under 100 ms (rules only 3301 against 3342 ms; decision model on 4192 against 4253 ms) and the mini model's p95 was worse (orchestration p95 4508 against 3177 ms with the decision model on). In 18 standups per model the mini model raised 72 % of the flagged items with their owner against 87 %, repeated its own sentence or the previous turn 8 times against 0, and finished 139 timed turns against 215 because its standups ended earlier. I did not choose it for latency, and there was no latency to choose it for. Details and the method's limits are in the README.
2. **Keep `Voice__SilenceMs` at 800 ms.** At 800, 650 and 500 ms nothing separated the settings: the agent never spoke over a person, and the 550 and 700 ms pauses in the scripted openings did not split the utterances even at 500 ms, so the voice service's own end-of-turn detection holds through pauses of that length. The rule was "the smallest value with no increase in interruptions", which would be 500 ms on this evidence, but the test could not tell the values apart and shorter settings cost more in a real room than in a scripted one, so I did not change a default on a test with no resolving power. It stays a code and configuration default, not a per-team setting: the portal has no place for it.
3. **The realtime models are a parameterised list.** `realtimeModels` in `infra/main.bicep` (default `gpt-realtime-2.1` only) feeds `foundry.bicep`; `requiredModels` in `infra/hooks/regions.json` is the matching preflight list. `gpt-live-1` is removed from both. The mini deployment was added for the measurement and removed again after it lost.
4. **The Models page no longer shows placeholder prices.** Tiers come from Learn. The USD rates are on the Azure Speech pricing page, which loads them by script and could not be read here, so prices are `null`, the cost per standup shows "Price unverified (Pro tier)", and the estimator returns `null` rather than guessing (`ModelPrice` prices are now nullable; tests added). Set real rates under `ModelComparison:Models` when someone reads them.
5. **The preflight counts a re-run's own deployments.** A second `azd up` into the same environment saw its own gpt-realtime deployment as used quota and refused. The check now adds back the capacity this environment already holds.
6. **Two measurement aids stay.** `[pause N]` markers in cast lines with a pause variant of the cast, and a `TalkOver` event (recorded when someone cuts the agent off; in a scripted run that is the agent speaking over a person) with transcript capture in the gate harness. `bench.ps1` drives the batches.

## Measured

See the README for the tables. The points that matter for the decision:

- The same configuration moved by about 25 % between two time windows in one session (rules only: 3301 / 4288 ms, then 4135 / 5803 ms). So latency comparisons are only meaningful between batches run next to each other, and nothing under about 25 % separates rows run at different times.
- G1: the default configuration (model, silence, analyser) is unchanged by this work, so G1 holds by construction. It cannot be re-confirmed at the 5 % and 10 % tolerances by measurement, because the noise between time windows is larger than that. Single-standup p50 in westus3 was 3439 and 3470 ms on the verify runs; three at once measured 3400 ms (G9).
- Decision model on was slower than rules only (4192 against 3301 ms) in this session, but that pair was an hour apart, so the 900 ms gap is mostly the window. The decision call itself took 164 ms p50.

## Quota

The expectation in the brief was that removing `gpt-live-1` lets `gpt-realtime-2.1` use the whole limit. **That is not how the quota is shaped.** `az cognitiveservices usage list` shows one row per model: `OpenAI.GlobalStandard.gpt-realtime-2.1` has a limit of 10, `gpt-live-1` has its own limit of 10, and `gpt-realtime-2.1-mini` has its own. Removing `gpt-live-1` frees only the `gpt-live-1` row. The `mcm` environment holds 5 of the 10 `gpt-realtime-2.1` units, so `zavaw3` could take 5. The quota request in `docs/gates/M11-gates.md` now asks for more `gpt-realtime-2.1` units.

## Consequences

- No behaviour change to the default path. The Foundry account has one fewer deployment (`gpt-live-1`).
- Not done: listening to the audio, a longer pause test (1,000 ms and above) that could separate the silence settings, a drift-proof interleaved latency design, and reading the USD prices.
- Found on the way: all sessions of all teams are one Cosmos item, which passes the 2 MB limit after a few dozen standups with transcripts (this session ran about 45, on top of earlier ones) (see the README). Needs one item per session before many teams run for long.
