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

## S5, S6, S7. Media bot, voice path, board in the meeting

**Not run.** The brief says to stop at a failed spike. They also need a Windows Server VM with a public address, a Graph Communications media bot, and a live Teams meeting with real participants, none of which exist in the demo tenant. Facts from Learn that bear on them, not measured here:

- An application-hosted media bot must be C# on .NET, on a Windows Server guest OS in Azure (IaaS VM, VM scale set, Cloud Service or AKS), not an Azure web app. Each VM needs its own public IP and port. Real-time media calls are pinned to the VM that accepted them and end if it stops.
- Minimum two cores, four vCPU for non-Dv2 sizes. Video encode and decode are done in software on the CPU (no GPU offload); "one or two concurrent media sessions per CPU core" is quoted for raw RGB24 or NV12 video. That is the figure S7 must replace with a measurement.
- Supported audio formats include PCM S16LE; the 24 kHz mono rule in the repository's voice section is ours, not the platform's.

## Consequences

- M11 cannot proceed as briefed. S4(a) fails, S1 shows direct publishing cannot keep the Teams identity, and S5 to S7 (media bot, voice path, board share) are still unrun. S2 is settled: stay on the Azure-managed bot and take the roster from the media bot.
- What is established: hosted agents work in West US 3 with a 4 s cold start, a persistent `$HOME`, and a WebSocket voice path that adds about 80 ms per 20 ms audio frame from a laptop (a laptop figure; it includes the public internet and is not the in-Azure figure S6 needs).
- Clean-up done: the `s3-spike` agent was force-deleted, the `AcrPull` and `Container Registry Repository Reader` assignments on the registry were removed, the spike image repository was deleted, and the control entitlement was removed. Work item #33 created for S4(b) was destroyed. Nothing in `infra/` changed.

## What is needed to continue

1. Choose how Azure DevOps writes are authored under S4: the API's managed identity (the approver named only in the comment), or the approver's delegated token through an on-behalf-of exchange (the approver is the revision author). S4(b) showed a delegated token records the person as author.
2. For S5 to S7: a Windows Server VM in the tenant and a person or test account to join a scheduled meeting as a participant.
