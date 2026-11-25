# 0021. Hosted agent split and state

Status: partly built. The voice relay, session storage, pre-warm, rollout and team context are built and measured. The decision model's p95 gate (G1) is **not met** on the hosted route, so the hosted route ships as a selectable adapter and the default stays direct. Moving the voice gateway, the ceremony agents and the Teams handlers into the host is **not done** (see "Not moved"). Prompt 4.

## Context

Prompt 4 asks for the agent to run as a Foundry hosted agent, split from the API along the Application boundary, with Cosmos as the source of truth, a pre-warmed sandbox, immutable versions with a canary, and G1 holding with the decision model on and off. ADR 0015 S1 and S3 established that hosted agents work in West US 3 with a cold start of about 4 s; S6 (the live voice path) had no result.

## Decisions

1. **S6 first, as a relay.** The smallest hosted agent forwards every Voice Live frame (ADR 0015 S6 has the measurements). The hop held in the paired bench, so the relay was kept as the seed of the voice gateway: `src/Zava.Scrum.AgentHost`, `VoiceRelay` (stateless, forwards unchanged, refuses a call with no `team`, close 1008) and `VoiceRelayHandler` (Voice Live with the agent's own identity). It is selected by `VoiceLive:RelayEndpoint`; empty means Voice Live directly. Both are real adapters of `IVoiceSessionFactory`. `VoiceSessionConfig` carries the team id and the session id, and the session id is the hosted agent session id, so a reconnect reattaches to the same sandbox.
2. **One Cosmos item per session, partitioned by team (step 0a).** Container `Sessions`, partition `/teamId`, `ISessionItemStore` with a file and a Cosmos implementation, `SessionRepository` over it, and `SessionMigration` (run at API start) that moves the legacy `sessions.json` document and deletes it only after every session is in. Idempotent. Demo reset clears the items. The data-contributor role is granted on the container in Bicep.
3. **State and restart.** The host holds no state. `HostRestartTests` kills the host with a connection open, reads the session back from the store with its events, reconnects to the same hosted session id and continues. The gateway's existing Voice Live reconnect covers the live case.
4. **Pre-warm.** `PrepareStandupHandler` calls `IAgentHostWarmer`: one HTTP call to the agent on the standup's session id with the team id and the API's Entra token, recorded on the session as `HostWarmUp` (route `direct`, `hosted` or `hosted-canary`, whether it started, milliseconds) and returned by `GET /api/sessions/{id}`. "Prepare now" and the scheduler's pre-brief both send `PrepareStandupCommand`. A failed warm-up never blocks preparing. Deployed: 3585 ms on a cold sandbox, 2320 ms on a new session of an already running agent.
5. **Rollout by two agents, not a percentage on one endpoint.** `PATCH agents/{name}` with two fixed-ratio rules was refused: "Multi-arm fixed-ratio traffic splitting requires an experiment selector ('experiment_id' and 'salt'); an ordinary selector must contain exactly one fixed-ratio rule routing 100% of traffic", and setting those fields is "restricted to the Agents Optimization service". The Learn page "Deploy a hosted agent to production" shows a 90/10 split through `azd ai agent endpoint update`; the live service does not allow it for a normal caller in this project. The service wins and the difference is recorded here. So: the stable agent is pinned to one version (allowed), and the canary is a second agent (`<name>-canary`, built by `agent-host.ps1 -Canary`) that the API chooses per session (`VoiceRoute`: a hash of the session id, for the teams listed in `VoiceLive:CanaryTeams`, at `VoiceLive:CanaryPercent`). `canary.ps1` has `-Pin`, `-Canary -Percent -Teams`, `-Promote` (the canary image becomes a new stable version, pinned, canary stopped), `-Rollback` (stop the canary, or pin the previous version) and `-Show`. Because the choice is per team, a canary can be limited to one team, which a version selector could not do. A change of canary settings is an API revision rollover, so a rollback takes effect when the new revision is ready (about a minute), not instantly.
6. **Channels.** The Azure-managed bot and Azure Bot Service stay (ADR 0015 S2): the Teams and Copilot handlers with the 5A single sign-on remain in the API at `/api/messages`. The greenfield environment has its own bot id and Teams app package (the existing human gate in `docs/DEPLOY.md`); `38a0a184` is not used. The direct Foundry publish route is not used. Activities are not routed to the host (see "Not moved").
7. **azd and Bicep.** `agent-host.ps1` (postdeploy) builds the image in the environment registry, creates an immutable version and assigns the agent identity Cognitive Services User and Azure AI User on the Foundry account (least privilege; the identity exists only after the agent does). The project identity has AcrPull already. `main.bicep` `voiceRoute` (`Direct` default, or `Hosted`) sets the API's relay endpoint; `data.bicep` adds the `Sessions` container. No private networking. Nothing the API uses was removed.
8. **Team context.** Every call into the host carries the team id: the relay refuses a call without one, the warm-up sends it, the canary is chosen per team, and the session item is partitioned by it. The team is taken from the session, which the API resolved (`ITeamResolver`, then the authorised route) before the call. Several teams' standups can run through one host; the capacity governor in the API limits them.
9. **No Azure DevOps credential in the host.** The source scan test `DelegatedWriteRulesTests.AgentHost_WhenItExists_ReferencesNoAzureDevOpsCredentials` now runs against `src/Zava.Scrum.AgentHost` and passes.

## Measured

S6 (ADR 0015 S6): the relay adds 8 to 49 ms at p50 and 178 to 230 ms to the p95 of a turn in the paired bench (about 4 % of the p95 baseline); barge-in is not slowed; a new hosted session costs about 2.4 s more to connect than a direct one and the first reconnect after the 15-minute idle stop about 4 s more (6.8 s in all).

**G1 on full standups** (`docs/gates/m11-p4/g1/`, `g1-summary.txt`; deployed `zavaw3`, westus3, single-team Simulated Room standup, route and decision model alternated in rounds so each hosted run has a direct run in the same window, three rounds, 35 to 36 timed turns per cell):

| Decision model | Route | Turns | Total p50 | Total p95 | Voice Live first audio p50 / p95 |
| --- | --- | ---: | ---: | ---: | ---: |
| On | Direct | 35 | 3333 | 4720 | 1535 / 2993 |
| On | Hosted | 36 | 3363 (+30, +0.9 %) | 5679 (+959, +20 %) | 1499 / 3146 |
| Off (rules) | Direct | 36 | 3087 | 4594 | 1458 / 2430 |
| Off (rules) | Hosted | 35 | 3152 (+65, +2.1 %) | 4251 (-343, -7 %) | 1485 / 2147 |

Paired by round (hosted minus direct, model on): p50 -176, -370, +218 ms; p95 +993, +524, +331 ms. Model off: p50 +355, -52, -131 ms; p95 -716, -1571, +384 ms. Against the westus3 deployed figures from before this prompt (p50 about 3.45 s; p95 4.6 to 4.9 s) the picture is the same.

**Reading, without tuning the gate.**
- **p50 holds** with the model on and off (+0.9 % and +2.1 %, both inside 5 %).
- **p95 with the decision model on does not hold:** +20 % against the same-window direct runs (limit 10 %), worse in all three rounds. With the model off the hosted p95 was lower than direct, which a hop cost would not produce.
- **What the tail is made of:** the two slowest hosted turns with the model on (5823 and 5679 ms) were orchestration outliers (4311 and 3880 ms) with a normal voice first audio (1330 and 1360 ms), not slow voice; a direct run with the model off has the same kind of outlier (orchestration 3985 ms, total 5822 ms). The voice component the hop affects has medians of 1499 against 1535 ms (model on) and 1485 against 1458 ms (off). I did not find the cause of the orchestration outliers.
- **What cannot be said:** with 36 turns the p95 is the second slowest turn, so one or two outliers decide it. The S6 bench (64 paired turns per run) put the hop's own p95 cost at about 4 % of the baseline. The two measurements disagree about the tail and neither is large enough to settle it.

**G1 is therefore not passed for the hosted route at p95 with the decision model on.** By the rule of the prompt I stop and write the options, not tune the gate:
1. Keep `Direct` as the default (`voiceRoute`) and ship the hosted route as a selectable adapter; run the gate again with at least 100 turns per cell before changing the default.
2. Find and fix the orchestration outliers (3.9 to 4.3 s) that appear on both routes; they, not the relay, set the tail.
3. Remove the extra hop by moving the voice gateway into the host, so the bridge connects to the host and the host to Voice Live. That needs the host to read Azure DevOps through the API for every read and write (no credential in the host), which is the larger change not made here.

## Not moved (differences from the prompt, and why)

- **The voice gateway, ceremony agents (Microsoft Agent Framework), Teams and Copilot handlers and `/activity/messages` stay in the API.** The host carries only the relay. Moving them means the host would run the Application layer, whose ports read Azure DevOps, while the prompt forbids an Azure DevOps credential in the host and requires every read and write to go through the API: an internal API client for each reader port, an Entra-authenticated host-to-API contract, and the 5A single sign-on handlers and delegated-write context rebuilt on the host side. The .NET Activity adapter is also in preview (`Azure.AI.AgentServer.Activity` 1.0.0-beta.1, ADR 0015 S3). That is a separate piece of work, and the relay result (item 1) and the G1 result above decide whether it is worth doing.
- **The API does not call the host with a separate "team and identity" envelope.** The voice leg carries the team id and the session id, and the platform validates the API's Entra token; the host never sees an end user's identity because no user-delegated work happens on it.
- **The canary is per agent pair and team, not per version of one endpoint** (decision 5).
- **The plan says ADR 0019 in steps 7 and 9;** 0019 is the realtime-model ADR and 0021 is the next free number.
- **The recorded Admin lines (`video\admin-lines\wav`) are not in the repository;** the S6 bench uses spoken questions synthesised with Azure Speech at 24 kHz. The client is a laptop outside Azure.

## Consequences

- The default voice route is direct. The hosted route is available with `VOICE_ROUTE=Hosted` or a `VoiceLive__RelayEndpoint` setting, and costs the sandbox's allocated CPU and memory per active session.
- Session storage no longer limits the number of standups. The migration runs at every API start and is safe to repeat.
- `azd deploy` runs the postdeploy hook, which creates a new version of the stable agent; while the selector is unpinned (the default `@latest`) a new version is live at once. Pin first (`canary.ps1 -Pin <version>`) to keep a version stable while a candidate is tried.
- The hosted-agent administration SDK is the 3.0 beta (`tools/AgentDeploy`), separate from the 2.x line the solution uses.

## Not verified

- G1 at p95 with the decision model on (above), and any sample larger than 36 turns per cell.
- The 10-step Teams rehearsal and the Teams path on the hosted route (the Teams handlers did not move).
- G8 destroy and rebuild with this change: `azd up` and `azd deploy` ran on the existing environment and the new hook created the agent (versions 2 to 4 and the canary), but `azd down --purge` and a rebuild were not run.
- A canary with real traffic: the live test used 14 prepares (8 to the canary at 50 %, then a rollback to 8 of 8 stable), not voice turns through the canary agent.
- The host after a real platform stop (the 16-minute idle test was the relay only, and ran before the team check was added).
- Whether a percentage split becomes available to a normal caller later; the canary would then not need the API's help.
