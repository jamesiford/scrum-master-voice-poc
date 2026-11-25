# 0015. M11 platform decisions

Status: **spikes stopped at S4(a); S4 decided by the owner on 24 November 2026 (option 1); S2 decided on 24 November 2026 (keep the Azure-managed bot)**. S1, S2, S3 and S4 were run and are recorded below. S5, S6 and S7 were not run. No M11 design decision beyond those the evidence and the owner's decisions support is taken here.

## Context

M11 proposes moving the agent from our own Azure Container Apps host, Azure Bot resource and Azure DevOps access to Foundry hosted agents, Entra agent identities and an application-hosted media bot. Each spike below asks one question of the real platform in the demo tenant (West US 3, `fdy-mcm-scrum-demo`). The spike code is in `spikes/` and is not part of the solution. Learn pages were read on 24 November 2026; where a page and the brief differ, the page is recorded.

## S1. Teams app identity when publishing a hosted agent

**Question.** Can a hosted agent keep the existing Entra app and bot id (`38a0a184-e4b8-4864-8d87-bde05a6ae7e9`) so that the Teams update is a version bump, or does Foundry generate a new identity?

**Method.** Deployed a hosted agent (S3), then called `POST {project}/agents/s3-spike/microsoft365/zip?api-version=v1` with `performBotRegistration=false`. That endpoint builds and validates the same package as publishing but registers nothing (Learn: publish-copilot-virtual-network). The manifest is kept in `spikes/s1-pkg/manifest.json`.

**Observed.**

| Manifest field | Value | Meaning |
| --- | --- | --- |
| `id` | `ff128b73-...` | The agent's GUID (`agent_guid`), not our app id |
| `bots[0].botId` | `385b00bb-...` | The agent **instance identity** (`servicePrincipalType` is `ServiceIdentity`) |
| `copilotAgents.customEngineAgents[0].id` | `385b00bb-...` | The same |
| `bots[0].scopes` | personal, team, groupChat, copilot | |
| `webApplicationInfo` and `authorization` | absent | No resource-specific consent is declared, so no meeting RSC |

Learn says the generated `id`, `botId`, `webApplicationInfo.id` and `customEngineAgents[0].id` are service generated and must not be changed. So direct publishing cannot keep `38a0a184`: Foundry issues a new identity per agent, and Teams sees a new app, not a version bump of 1.0.4.

**Decision.** Not taken. Direct publish means a new Teams app and bot id and a reinstall, not an upgrade. The only route that keeps `38a0a184` is to keep our own bot registration and host, which is the present design. Learn also says a deployment tool may provision an Azure Bot resource instead of direct registration; whether Foundry would accept our app id as the agent's channel identity was not tested, because S2 (settled: stay on the Azure-managed bot) and S4 decide whether this route is worth testing.

## S2. Meeting events for a Foundry-registered bot

**Run on 24 November 2026 (Developer Portal inspection and Learn; no live meeting).** Signed in as the demo tenant admin and read, without changing anything:

- **Teams-managed bots** (Developer Portal, Tools, Bot management): 0 items. The S1 spike registered nothing, so there is no Foundry-registered bot to inspect.
- **Azure-managed bots:** `bot-mcm-scrum-demo` (`38a0a184-e4b8-4864-8d87-bde05a6ae7e9`) is listed. Its page offers only the delivery method (socket mode or an HTTPS endpoint, currently `https://ca-api-wiezudy6mumfg.../api/messages`). It has **no meeting event subscription settings**.
- **Learn and the Agents SDK sample:** meeting **start and end** events need only the resource-specific consent `OnlineMeeting.ReadBasic.Chat` in the app manifest (Learn: agents-sdk/teams/teams-meetings). **Participant join and leave** events need the Meeting Event Subscriptions setting on a bot created in the Developer Portal, that is a Teams-managed bot ("for these events, you must create your Bot via TDP"; the `bot-meetings` sample: "aren't available to an Azure Bot resource").
- **SSO conflict.** The Teams SDK documentation says a Teams-managed bot cannot do OAuth or SSO ("Use Azure only when your bot needs OAuth or SSO"). Approver-delegated writes and Microsoft 365 Copilot questions both need the on-behalf-of flow, which needs SSO.

**Decision.** Keep the Azure-managed bot (`38a0a184`), which keeps SSO and the existing app id.
- Use **meeting end** (and start) as an additional trigger by adding the `OnlineMeeting.ReadBasic.Chat` consent to the manifest. Delivery to our Azure bot is **not yet verified**; it needs a manifest update and a live meeting.
- Take **participant join and leave from the media bot's call roster** (prompt 6), not from Teams meeting event subscriptions. No Teams-managed registration is needed.
- Publishing a spike agent with `performBotRegistration=true` is **not needed**: S1 showed Foundry issues a new identity, and S2 no longer depends on a Foundry-registered bot.

**Not verified.** That meeting start and end events reach an Azure Bot resource with only the RSC consent; that a Foundry-registered bot would count as Teams-managed.

## S3. Hosted agent availability and fit

**Method.** Built a minimal .NET 10 agent (`spikes/s3-hosted-agent`) serving `/invocations` and `/invocations_ws`, pushed it with `az acr build`, created versions through the Azure.AI.Projects SDK (`spikes/s3-deploy`), and timed each step from a developer laptop to West US 3.

**Availability.**

- West US 3 is listed for hosted agents (Learn: limits-quotas-regions). The default limit is 1,000 concurrent hosted sessions per subscription in that region.
- Protocols: Responses, Invocations, Invocations (WebSocket) and A2A are available in all hosted-agent regions. Native Activity is GA for Python; **the .NET Activity adapter is in preview** (`Azure.AI.AgentServer.Activity` 1.0.0-beta.1). `Azure.AI.AgentServer.Invocations` is 1.0.0-beta.8. The hosted-agent administration API needs `Azure.AI.Projects` 3.0.0-beta.6, while the solution uses the 2.x line.
- Sandbox sizes run from 0.25 vCPU and 0.5 GiB to 16 vCPU and 32 GiB. Billing is on allocated CPU and memory per active session, including idle time before suspension.

**Measured.**

| Step | Run 1 | Run 2 |
| --- | --- | --- |
| Create version call returns | 7.0 s | 8.0 s (32 s for the very first agent, which also creates its blueprint and identity) |
| Version `Active` | 21.1 s | 17.9 s |
| First request on a new session (cold) | 3.4 s | 4.1 s |
| Next requests on that session (warm) | 0.26, 0.31, 0.26 s | 0.24, 0.25, 0.23 s |
| WebSocket connect | 1.5 s | 1.8 s |
| 640-byte binary frame round trip, 50 frames | p50 82 ms, p95 108 ms | p50 81 ms, p95 100 ms |
| First request after about 19 minutes idle | | 3.8 s, then 0.9 s |

After idle the process restarted (new process start time) and the file written to `$HOME` before the idle period was still there. In-process state is lost; `$HOME`, `/files`, Foundry conversations and the Foundry state store persist. The idle timeout defaults to 15 minutes and is configurable from 2 to 240 minutes; a connection is capped at about 30 minutes and closes with code 1001, so a client must reconnect with the same session id. Frames are limited to 1 MB.

Two deployment facts that cost time:

- The registry `acrmcmscrumwiezudy6mumfg` uses legacy registry permissions. The project identity needed `AcrPull`; `Container Registry Repository Reader` gave `ImageError: Container registry authentication failed`. Any template that adds a hosted agent must grant the role that matches the registry's permission mode.
- Deleting an agent with idle sessions returns 409 until `force=true`.

**Decision.** Not taken. Hosted agents are available in West US 3 and the cold start is about 4 s on top of a 20 s version build, so "Prepare now" can pre-warm by calling the agent once with the session id the standup will use. The .NET preview packages and the beta SDK are a real cost to record against rule 1; they are the only .NET route today.

## S4. Azure DevOps and agent identities

**Method.** From inside the hosted container, requested a token for Azure DevOps (`499b84ac-1321-427f-aa17-267ca6975798/.default`) from the platform identity endpoint (`IDENTITY_ENDPOINT`, which the sandbox provides), and called Azure DevOps. Then tried to add the identity to the organisation, with an ordinary service principal as a control. Part (b) used a delegated user token.

**Observed.**

- (a) The agent can obtain a token: `idtyp=app`, `oid` and `appid` both `385b00bb-...`, so it is a distinct, auditable principal. Azure DevOps answered 203 (sign-in redirect) to every call, so the organisation does not know the identity.
- Adding it fails. `POST vsaex.dev.azure.com/{org}/_apis/serviceprincipalentitlements` returns `VS403283: Could not add user '385b00bb-...' at this time` with `stakeholder` or `express`; `vssps ... /graph/serviceprincipals` returns `VS860016: Could not find subject ... in the backing domain`. **The control succeeded**: the ordinary service principal `mcm-scrum-agent-calendar` was added with the same call (and removed again). The difference is the object type: the agent identity is `servicePrincipalType: ServiceIdentity`. The Azure DevOps service principal page lists service principals and managed identities and does not mention agent identities.
- (b) A delegated user token (`idtyp=user`, `scp=user_impersonation`) created and patched a Task, and Azure DevOps recorded `System Administrator` as creator and changer. This used the Azure CLI's user token, not a middle-tier on-behalf-of exchange; an on-behalf-of token is the same kind of delegated token, but the exchange itself was not run. Learn says Foundry does not do the exchange for you: the application backend acquires the user token and forwards it in an `x-client-*` header, and the agent's own identity is not combined with it.
- (c) Moot for the agent identity, which has no access at all. Reader-only access for it could not be set up.

**Failed.** The brief's premise, that an Entra agent identity can be given Reader-level access to Azure DevOps, does not hold in this tenant today.

**Options.** The owner chose option 1 on 24 November 2026 (the others were not tried):

1. **Chosen.** Keep Azure DevOps access in our own API host, using an identity Azure DevOps accepts (a service principal or managed identity), as now. A hosted agent would call the API for reads and never hold Azure DevOps credentials. This keeps the approval guard, `FollowUp.GetApprovedChanges()` and `IWorkItemWriter` where they are. The identity per operation is therefore: read, the API host's managed identity; propose, the model through the API (no Azure DevOps identity); write, the API host after an approved follow-up.
2. Wait for or ask Azure DevOps for agent identity support, then repeat this spike.
3. Have the hosted agent act only with delegated user tokens forwarded by a backend, and use no agent-identity access to Azure DevOps. Read then needs a signed-in user, which a scheduled standup does not have.

## S5 and S7. Media bot and board in the meeting

**Not run.** The brief says to stop at a failed spike. They also need a Windows Server VM with a public address, a Graph Communications media bot, and a live Teams meeting with real participants, none of which exist in the demo tenant. Facts from Learn that bear on them, not measured here:

- An application-hosted media bot must be C# on .NET, on a Windows Server guest OS in Azure (IaaS VM, VM scale set, Cloud Service or AKS), not an Azure web app. Each VM needs its own public IP and port. Real-time media calls are pinned to the VM that accepted them and end if it stops.
- Minimum two cores, four vCPU for non-Dv2 sizes. Video encode and decode are done in software on the CPU (no GPU offload); "one or two concurrent media sessions per CPU core" is quoted for raw RGB24 or NV12 video. That is the figure S7 must replace with a measurement.
- Supported audio formats include PCM S16LE; the 24 kHz mono rule in the repository's voice section is ours, not the platform's.

## S6. The live voice path through a hosted agent (run in prompt 4, 25 November 2026)

**Question.** Does relaying the Voice Live conversation through a Foundry hosted agent (`invocations_ws`) stay inside the G1 tolerance (5 % p50, 10 % p95)?

**Learn checked first** (build-voice-agent, hosted-agent-contract): the route is `GET /invocations_ws`; the platform proxies the upgrade and relays text and binary frames unchanged, validates the caller's Entra token itself (the container never sees it), consumes `api-version` and the path segments and forwards other query parameters unchanged (so `team` and `model` arrive in the container), caps a frame at 1 MB and a connection at about 30 minutes (close 1001, reconnect with the same `agent_session_id`; the sandbox persists across reconnects), and reads `FOUNDRY_AGENT_SESSION_ID` or the query parameter for the session. No difference from the prompt.

**Method.** `src/Zava.Scrum.AgentHost` is the smallest hosted agent: it opens a Voice Live session with the agent's own identity and forwards every frame both ways (the seed of the voice gateway; see ADR 0021). It runs in the greenfield `zavaw3` environment (westus3), version 1 (no team check yet), identity roles Cognitive Services User and Azure AI User on the Foundry account. `tools/VoiceRelayBench` drives the same Voice Live session two ways through the shipped `VoiceLiveSessionFactory`: directly, and through the relay (`VoiceLive:RelayEndpoint`). The audio is eight spoken questions synthesised with Azure Speech (24 kHz PCM16, 20 s in all, standing in for the recorded Admin lines, which are not in the repository), sent in real time as 20 ms frames, followed by silence until `speech_stopped`; then `response.create` and the wait for the first audio delta. Runs alternate by block (direct then relayed, then relayed then direct, four alternations of eight turns, so 32 turns per path, 64 per run), and the difference is taken per pair (same question, same block position). The client is a laptop outside Azure, so both paths include the same internet legs; the relayed path adds the platform gateway and the container.

**Measured** (milliseconds, `docs/gates/m11-p4/s6-*.json`; failed turns 0):

| | Run 1 | Run 2 |
| --- | ---: | ---: |
| Direct, speech end to first audio, p50 / p95 | 1904 / 2155 | 1906 / 2160 |
| Relayed, speech end to first audio, p50 / p95 | 1968 / 2385 | 1918 / 2339 |
| **Paired difference** (relayed minus direct), p50 / mean / p95 | **+48.5 / +70.9 / +505** | **+7.5 / +32.6 / +410** |
| Paired difference, speech detected (`speech_started`), p50 | +16 | not recorded separately |
| Paired difference, first audio after `response.create`, p50 / p95 | +33 / +447 | +30 / +386 |
| Relayed p95 minus direct p95 | +230 | +178 |

Run 2 overlapped with .NET builds on the same laptop, which is why it is a second sample and not a replacement. Other measurements, four alternations each:

| Measurement | Direct | Relayed |
| --- | ---: | ---: |
| New session, connect and configure, p50 / p95 (s) | 1.9 / 2.8 | 4.3 / 5.7 |
| Reconnect to the same session id, p50 / p95 (s) | 2.1 / 2.3 | 2.8 / 3.5 |
| Barge-in: `response.cancel` to the last audio still received, p50 / p95 (ms, n 12) | 214 / 3250 | 157 / 2104 |
| After 16 minutes idle (the 15-minute stop), same session id | | reconnect 6.8 s, then a normal turn (2.4 s) |

**Reading.** Against the full-turn baseline of about 3.45 s p50 and 4.9 s p95, the tolerance is about 172 ms at p50 and 490 ms at p95. The hop adds 8 to 49 ms at p50 (0.2 to 1.4 %) and 178 to 230 ms to the p95 of the turn (3.6 to 4.7 %): inside both. The p95 of the paired differences (410 to 505 ms) is the tail of individual turns and sits at the edge of the p95 allowance; it comes from a few slow turns, not a shift of the distribution, and it is recorded here, not hidden. Barge-in is not slowed. The cost is at connect time: a new hosted session takes about 2.4 s longer than a direct one (the sandbox starts), and after the idle stop the first reconnect takes about 4 s longer again. That is what pre-warm (ADR 0021) removes from the standup.

**Decision.** The hop holds. The relay is kept as the seed of the voice gateway and ships as an adapter selected by `VoiceLive:RelayEndpoint` (ADR 0021). A laptop is a worse client than the API in the same region, so these figures are an upper bound for the hop; the G1 gate on full standups through the API is in ADR 0021.
## Consequences

- M11 cannot proceed as briefed. S4(a) fails, S1 shows direct publishing cannot keep the Teams identity, and S5 and S7 (media bot, board share) are still unrun. S6 was run in prompt 4 and holds. S2 is settled: stay on the Azure-managed bot and take the roster from the media bot.
- What is established: hosted agents work in West US 3 with a 4 s cold start, a persistent `$HOME`, and a WebSocket voice path that adds about 80 ms per 20 ms audio frame from a laptop (a laptop figure; it includes the public internet and is not the in-Azure figure S6 needs).
- Clean-up done: the `s3-spike` agent was force-deleted, the `AcrPull` and `Container Registry Repository Reader` assignments on the registry were removed, the spike image repository was deleted, and the control entitlement was removed. Work item #33 created for S4(b) was destroyed. Nothing in `infra/` changed.

## What is needed to continue

1. Choose how Azure DevOps writes are authored under S4: the API's managed identity (the approver named only in the comment), or the approver's delegated token through an on-behalf-of exchange (the approver is the revision author). S4(b) showed a delegated token records the person as author.
2. For S5 and S7: a Windows Server VM in the tenant and a person or test account to join a scheduled meeting as a participant.
