# Deploying from an empty subscription

`azd up` builds the whole current solution in a new resource group: Foundry with its four model deployments, Azure Communication Services, Cosmos DB, the container registry, Container Apps (API and meeting bridge), the Azure Bot, the Entra objects, the Azure DevOps project and seed data, and the Teams package. Decisions and their evidence are in [ADR 0015a](adr/0015a-greenfield-deployment-model.md).

## Parameters

Set once with `azd env set`. None is a secret.

| Name | Required | Meaning |
| --- | --- | --- |
| `AZURE_LOCATION` | yes | Region. It must offer the realtime models and Voice Live; `preflight.ps1` checks. `eastus2` is proven. |
| `AZURE_TENANT_ID` | yes | The Entra tenant. |
| `ADO_ORG_URL` | yes | For example `https://dev.azure.com/your-org`. The project and seed data are created inside it. |
| `ADMIN_UPN` | yes | The administrator. Also the calendar organiser. |
| `REALTIME_SKU` | no | `DataZoneStandard` (default in `main.parameters.json`) or `GlobalStandard`. Global realtime quota is subscription-wide and was used by the original environment, so the default avoids it. |
| `VOICE_ROUTE` | no | `Direct` (default) or `Hosted`: whether the API reaches Voice Live directly or through the hosted agent relay (ADR 0021). Set it before `azd provision`; the agent itself is created by `agent-host.ps1`. |
| `MEDIA_HOST_ENABLED`, `MEDIA_VM_SIZE` | no | `false` by default; opt-in public Windows media VM, `Standard_D4s_v5` (four vCPU), hard call cap 1. `D3_v2` was unavailable in `zavaw3`/`westus3`. |
| `MEDIA_SERVICE_FQDN` | no | Optional certificate-covered DNS name. Empty uses the Azure public IP DNS label; the hook obtains a Let's Encrypt HTTP-01 certificate and imports directly from the VM to Key Vault. |
| `BRIDGE_MODE` | no | `Acs` (default), `Simulated`, or `GraphMedia`. Select native media only after HTTPS bootstrap and per-meeting RSC installation. The voice factory supports Direct or Hosted; preserve the working route until measured. |
| `BOARD_MODE` | no | `Share` (ACS canvas, default) or `MediaShare` (VBSS on GraphMedia). Bicep sets API `Board__Mode`; image-only updates preserve the current setting. No native browser or additional resource. |
| `CHAT_CAPACITY`, `REALTIME_CAPACITY`, `DECISION_CAPACITY` | no | Deployment capacity. Defaults 100, 10, 100. Below 100 the decision model hits its rate limit. `REALTIME_CAPACITY` applies to each model in the `realtimeModels` list in `infra/main.bicep` (default `gpt-realtime-2.1` only). Quota is one row per model, so a model you do not deploy frees nothing for another. |

```pwsh
azd env new <name>
azd env set AZURE_LOCATION eastus2
azd env set AZURE_TENANT_ID <tenant id>
azd env set ADO_ORG_URL https://dev.azure.com/<org>
azd env set ADMIN_UPN <admin upn>
azd up
pwsh infra/hooks/verify.ps1
```

Resources are named with the `zava-scrum` prefix. The original environment (`mcm-scrum-demo`, resource group `rg-mcm-scrum-demo-wus3`) keeps its `mcm` names until it is retired. Do not run `azd provision` for it with these templates: they would create a second set of resources, not update that one. The previous templates are in git history at commit `84498ad`.

## What runs, in order

| Phase | Script or template | What it does |
| --- | --- | --- |
| preprovision | `infra/hooks/preflight.ps1` | Fails fast: tools, sign-in, Azure roles, resource providers, Graph scopes, Azure DevOps administrator, region, model catalogue and quota, Cosmos policy. |
| preprovision | `infra/hooks/entra-portal-app.ps1` | Portal app registration, `access_as_user` scope (consented for the organisation), Azure CLI pre-authorised, the three app roles from `infra/entra/portal-app-roles.json` (Admin, Approver, Reader) assigned to `ADMIN_UPN`; sets `PORTAL_CLIENT_ID` and `ADMIN_OBJECT_ID`. |
| provision | `infra/main.bicep` and `infra/modules/*` | Everything in Azure, including role assignments. |
| postprovision | `infra/hooks/entra-post.ps1` | Redirect URIs (web for Easy Auth, single-page application for MSAL.js), the managed identity's Graph `User.Read.All` permission (roster identity resolution), `Portal__ClientId` and `Identity__AdminObjectIds` on the API, and Easy Auth on both apps. |
| postprovision | `infra/hooks/ado-setup.ps1` | Azure DevOps project, area, iterations, seed items, sprint; adds the managed identity to the organisation. |
| postprovision | `infra/hooks/exchange.ps1 -Mode Apply` | Switches calendar and email to Graph once the Exchange grant exists. |
| deploy | azd | Remote ACR builds of both images, then the revisions. |
| postdeploy | `infra/hooks/teams-app.ps1` | Builds `appPackage/dist/zava-scrum-agent-<env>-<version>.zip` with the deployed bot id. |
| postdeploy | `infra/hooks/media-host.ps1` | Opt-in only: separate calling app/Bot registration, minimum media permission, artefact upload, vault delivery, Windows startup task and native Teams package; reports TLS/RSC gates. |

Every script is idempotent; re-running `azd up` creates no duplicates.

## Identity model

- One user-assigned managed identity runs both apps. It reads and writes Azure DevOps (it is added to the organisation, ADR 0015 S4), calls Foundry, Cosmos DB and ACS, and is the Azure Bot's identity (`UserAssignedMSI`). There is no bot client secret, and the Teams `botId` is the identity's client id.
- Easy Auth supports only a client secret, not a federated credential. The hook creates one credential on the portal app and stores it only as the Container Apps secret `portal-secret`. Older `easyauth-*` credentials are removed on each run.
- Writes to Azure DevOps use the approver's own token (on-behalf-of). The API proves the portal app with the managed identity through a federated credential, so there is no client secret for it; Teams card actions sign in through a Bot Service OAuth connection that also uses a federated credential (`entra-post.ps1`).
- The bridge key is generated by the deployment (`newGuid()`) and kept as a Container Apps secret.
- The Azure CLI client is pre-authorised on the portal API scope, so `verify.ps1` and GateRunner call the deployed API with `az account get-access-token --scope api://<PORTAL_CLIENT_ID>/access_as_user`.

## Who can do what (ADR 0017, 0018)

The API validates the Entra bearer token itself; Easy Auth is an outer gate only. Three app roles on the portal app registration: `Zava.Scrum.Admin` (every team and the team registry), `Zava.Scrum.Approver` (approve or reject, for a team the person is in) and `Zava.Scrum.Reader`. The hook assigns all three to `ADMIN_UPN`. To give another person a role: Entra admin centre > Enterprise applications > *Zava Scrum Portal (<env>)* > Users and groups > Add user/group (an Entra Application Administrator or the app owner).

**Team roles are separate and stored by the app** (ADR 0018): TeamLead, Member and Reader per team, keyed by Entra object id. An Admin or the team's lead manages them on the Team page, or syncs them from an Entra group set on the team (add-only). A person with no membership sees no team. In Teams, administrators are the object ids in `Identity__AdminObjectIds` (set to `ADMIN_OBJECT_ID`); other Teams users can only decide their own follow-ups once their roster entry has an Entra object id.

The approval trail is the Cosmos container `ApprovalRecords`. The managed identity has a custom data-plane role there with create and read only (`data.bicep`), and Cosmos DB Data Contributor on the `collections` container only. `azd down` removes the trail with the environment.

## Remaining secrets (defects)

- **`portal-secret` (Easy Auth client secret).** Container Apps Easy Auth accepts only a client secret, not a federated credential. It is stored only as a Container Apps secret, rotated on each hook run, and is not used for Azure DevOps or ACS. This is a known defect, to be removed by moving the outer gate to the in-app validation already in place (ADR 0017) or when Easy Auth supports federated credentials.
- No ACS key or connection string, no bot secret, no Azure DevOps PAT in the deployment (`Ado:AuthMode` `Pat` exists for local development only).

## Container registry permissions

The registry is created with `roleAssignmentMode: LegacyRegistryPermissions`, set explicitly. In that mode the Foundry project identity needs **AcrPull**, not Container Registry Repository Reader (ADR 0015 S3). Both identities get AcrPull in `data.bicep`. If you switch the mode to ABAC, change the role to Container Registry Repository Reader.

## Human gates

The platform forces these. Each is named in the script output as `GATE`.

| Gate | Who | Action |
| --- | --- | --- |
| Deployer permissions | Subscription Owner (or Contributor plus User Access Administrator); Entra Application Administrator and Privileged Role Administrator (or Global Administrator) for the tenant-wide `User.Read` consent; Azure DevOps Project Collection Administrator | Checked by `preflight.ps1`. |
| Teams app upload | A Teams administrator, or a user allowed to upload custom apps | `pwsh infra/hooks/teams-app.ps1 -Interactive` (device-code sign-in; Graph accepts catalogue uploads only for a signed-in user with `AppCatalog.Submit`), or Teams admin centre > Teams apps > Manage apps > Upload new app, with the zip in `appPackage/dist`. Then approve the app. |
| Adding an Azure DevOps organisation | An Azure DevOps Project Collection Administrator of that organisation | Organisation settings > Users > Add users: add the API's managed identity (`id-<prefix>-<token>`, shown in the deployment outputs) with at least Basic access, then add it to the project's Contributors for each team project. Only organisations backed by this deployment's Entra tenant can be registered; the registry refuses another tenant. Then register the team (Teams page or onboarding wizard) and press Test connection. |
| Azure DevOps access for approvers | An Azure DevOps Project Collection Administrator (or project administrator) | Each person who approves needs to be a Contributor of the team's project, because an approval is written as that person (ADR 0020). Without it Azure DevOps refuses the write and the follow-up shows the reason. The managed identity no longer writes. |
| Admin consent for Azure DevOps | An Entra Global Administrator or Privileged Role Administrator | `entra-portal-app.ps1` grants the delegated Azure DevOps `user_impersonation` permission for the organisation. If the deployer cannot, do it in Entra admin centre > Enterprise applications > Zava Scrum Portal > Permissions > Grant admin consent. Without it every approval is refused with a consent message. |
| Deployer Entra roles | A Privileged Role Administrator | The deployer needs Application Administrator (or Cloud Application Administrator) to create the app registration, federated credentials and consents, and Privileged Role Administrator or Global Administrator for the tenant-wide consents. Global Reader is not enough: `entra-portal-app.ps1` stops with `Authorization_RequestDenied`. |
| Exchange calendar and mail scope | An Exchange administrator | `pwsh infra/hooks/exchange.ps1 -Mode Grant`. It gives the managed identity Application Calendars.ReadWrite and Application Mail.Send on the organiser's mailbox only (RBAC for Applications). Until then the calendar and email run on the outbox. |

## The hosted agent (prompt 4)

`infra/hooks/agent-host.ps1` runs in `postdeploy`: it builds `Dockerfile.agenthost` in the environment registry (`az acr build`, a small context), creates a new immutable version of the hosted agent `zava-voice-relay` (`tools/AgentDeploy`, needs the `Azure.AI.Projects` 3.0 beta) and assigns the agent's own identity Cognitive Services User and Azure AI User on the Foundry account. The agent identity exists only once the agent does, so those roles cannot be in the Bicep that runs first. The project identity already has AcrPull on the registry (the registry uses legacy permissions; Container Registry Repository Reader fails with `ImageError`). `canary.ps1` changes traffic; nothing else does. `voiceRoute=Hosted` (azd env `VOICE_ROUTE`) points the API at the agent.

Human gates for the hosted agent: none new. The greenfield environment's bot id and Teams app package remain the existing gate (Teams app upload, above); the old `mcm` bot id `38a0a184` is not used. The direct Foundry publish route is not used.

## Capacity and health

`Capacity__MaxConcurrentStandups` (default 3), `Capacity__StaggerSeconds` (5) and `Capacity__GraceMinutes` (10) limit how many standups start on one host; a standup that cannot start waits with a visible reason and the team is told after the grace. Realtime model capacity is `realtimeCapacity` in `infra/main.bicep` (`REALTIME_CAPACITY`); the GlobalStandard quota in the region must cover one deployment per concurrent standup you intend to run. Both container apps have HTTP startup, readiness and liveness probes on `/healthz`, so traffic reaches a new revision only when it answers.

## Checks

- `infra/hooks/verify.ps1` prints PASS or FAIL per check: health routes, the Easy Auth bearer path, the bot endpoint rejecting unauthenticated calls, a Simulated Room standup through the gate harness, a follow-up approved through the API and its comment found in Azure DevOps. It retries the two health routes for up to three minutes, because a new revision can still be starting.
- CI (`.github/workflows/infra.yml`) lints the Bicep and runs `az deployment sub what-if`. ARM cannot expand nested modules that depend on earlier outputs, so what-if shows the resource group and outputs only; `azd up` in a scratch group is the full proof.

## Teardown

```pwsh
azd down --purge
```

`--purge` releases the soft-deleted Foundry account so the same name and quota can be used again. It does not remove the Entra app, the Azure DevOps project or the Teams catalogue entry; they are reused by the next `azd up` (the Entra app is found by tag, the project by name).

## Known limits

- Provisioning takes about 20 minutes; the Container Apps environment alone took 13.5 minutes in the proven run.
- `eastus2` hosted-agent support is documented on Learn but was measured only in West US 3 (ADR 0015 S3). The current application does not use hosted agents.
- The destroy-and-rebuild proof is incomplete (G8): the second `azd up` stopped at preprovision because the deployer's privileged Entra role had lapsed. See docs/gates/M11-gates.md.
- A new-subscription run was not possible: the work used one subscription and tenant.

## Native media preparation and human gates (6a/6b, ADR 0023)

Use only `zavaw3`, `westus3`; never use `mcm` or its Entra objects. Keep `BRIDGE_MODE=Acs` while bringing up the alternative. For an empty environment the existing `azd up` templates include the opt-in module. For an existing environment, the scoped helper deploys that same module without resetting working Container Apps to greenfield placeholder images or rotating their secrets.

```pwsh
azd env select zavaw3
azd env set MEDIA_HOST_ENABLED true
azd env set MEDIA_VM_SIZE Standard_D4s_v5
azd env set BRIDGE_MODE Acs
pwsh infra\hooks\media-host.ps1 -ConsentOnly
# After the validation/deployment contract passes, for an existing environment:
pwsh infra\hooks\media-provision.ps1 -ValidateOnly
pwsh infra\hooks\media-provision.ps1
pwsh infra\hooks\media-host.ps1
pwsh infra\hooks\media-app.ps1 -Fqdn <MEDIA_FQDN>
pwsh infra\hooks\verify-media.ps1 -RequireReady
```

The opt-in module uses the pinned AVM Windows VM resource with four vCPU, one-call cap, a public IP and TCP 443/8445. TCP 80 serves only the temporary HTTP-01 challenge listener. The subnet exists solely for the Azure NIC; there are no private endpoints. The VM identity gets Key Vault Secrets User and Certificates Officer on its media vault, and Blob Reader on its artefact container. The named deployer gets certificate/secret operator roles on that vault and Blob Contributor on that container. No RDP ingress, tenant-wide joining role or extra tenant policy is added.

The postdeploy hook creates the dedicated `zava-media-zavaw3` calling app/service principal, Azure Bot and Teams calling webhook; it never changes the existing messaging bot identity. An Entra Privileged Role Administrator/Global Administrator must be able to consent to the minimum `Calls.AccessMedia.All` grant; if automation is refused, grant that **application** permission on the dedicated calling app only in Entra admin centre > App registrations > the calling app > API permissions > Microsoft Graph, then Grant admin consent. Do not add `Calls.JoinGroupCall.All` as a workaround. Per-meeting joining is the package's RSC permission, not this tenant-wide media permission.

| Gate | Who | Exact action |
| --- | --- | --- |
| `MEDIA-TLS`, only if automated issuance fails | DNS owner and Key Vault certificate operator for `MEDIA_VAULT_NAME` | Point a certificate-covered `MEDIA_SERVICE_FQDN` at output `MEDIA_PUBLIC_IP`; obtain a CA-trusted exportable PFX with its private key/full chain, the FQDN as primary name and more than 14 days remaining. Keep it outside the repo in a protected folder. Import with `az keyvault certificate import --vault-name <MEDIA_VAULT_NAME> --name media-tls --file <protected-empty-password-PFX>`, then rerun the hook. The normal Azure DNS label uses automated HTTP-01, not this fallback. |
| Calling-app consent, if refused | Entra Privileged Role Administrator or Global Administrator | Perform the dedicated-app consent described above, then rerun the hook. **Completed in 6b:** app `b681ab3c-023c-4e91-9212-50980b43dec1` holds only `Calls.AccessMedia.All`, verified by Graph readback during the authorised admin window. Never grant a broader join role. |
| `MEDIA-CATALOGUE` | Teams administrator signed into Teams admin centre | Upload generated `appPackage\dist\zava-media-bot.zip` through Teams apps > Manage apps > Upload new app. Open **Zava Scrum Media**, approve it if pending review, and confirm it is available under existing policy. The 6b administrator's actual Graph POST returned `Forbidden`: its Azure CLI token lacks `AppCatalog.Submit`, `AppCatalog.ReadWrite.All` or `Directory.ReadWrite.All`; an active Global Administrator role does not add OAuth scopes. No unrelated client or tenant policy is changed to bypass this. |
| `MEDIA-RSC` | Each meeting's organiser, permitted by existing tenant policy | In the stored scheduled meeting's Teams chat, add the published **Zava Scrum Media** app and accept its resource-specific `Calls.JoinGroupCalls.Chat` permission for that chat before the ten unattended G6 joins. Catalogue publication and tenant media consent do not grant this meeting consent. If installation is prohibited, record the gate and retain ACS; do not alter tenant policy. |
| Lobby admission | Meeting organiser | If existing meeting policy still sends the bot to the lobby, admit **Zava Scrum Media** from the meeting's lobby. Record that as a failed no-manual-step G6 case, not a successful unattended join. Do not relax tenant policy. |
| Native voice acceptance | Meeting organiser plus Daniel, Maya and Admin as distinct signed-in Teams participants | Use the stored calendar link; perform ten joins without admission/manual steps, label each participant's speech and overlaps for >=95% G2, and interrupt the agent with >=350 ms real speech. Measure audio cessation <=500 ms, cancellation/truncation and the spoken answer, including opening/recap states. Run native per-hop G1 before changing the default. Synthetic identity-tagged bytes are not these acceptance samples. |
| `MEETING-EVENT-CATALOGUE` | Teams administrator | Upload `appPackage\dist\zava-scrum-agent-zavaw3-1.0.5.zip` (or the explicitly configured later version) through Teams admin centre > Teams apps > Manage apps > Upload new app; approve under existing policy. The 7 updated-package POST was attempted during the active Global Administrator window and refused for missing delegated catalogue scopes, request `6cba8a7e-81ae-460f-a1b8-27abb7fe93ca` at 2026-11-26T21:20:16Z. No new unrelated scopes/policies are added. |
| `MEETING-EVENT-RSC` | Scheduled meeting organiser and team administrator/lead | Add the published **Zava Scrum Agent** to the actual stored meeting chat and accept `OnlineMeeting.ReadBasic.Chat`. Bind that exact conversation to the correct team using the existing Bind card as a team lead/admin. The messaging bot keeps SSO; the media app's join consent alone does not grant event delivery. Start and end a real meeting, verify the API's matched lifecycle metadata and exactly one end/recap path; an unbound/other-team event must not end it. |
| `NATIVE-BOARD-ACCEPTANCE` | Meeting organiser, members and a guest | After both apps are installed and a fresh session uses the stored meeting/verified roster, select GraphMedia/MediaShare as in DEMO.md. Without participant action, time the board's first appearance and each change to <=2 seconds for every participant, including the guest. Observe share stop/reconnect; sample VM process encoder CPU throughout one meeting against the unchanged cap 1, and run native G1 with the share running. Record sample sizes and failures. Renderer startup/goldens or Simulated Room are not these samples. |

After importing the certificate, rerun `pwsh infra\hooks\media-host.ps1` in the selected `zavaw3` environment. It publishes win-x64, uploads a hash-addressed private blob with Entra, delivers the existing bridge key to Key Vault, bootstraps the VM using IMDS tokens, verifies the ZIP hash/trusted certificate/hostname, installs the signed Microsoft VC++ runtime if needed, registers the public certificate on the calling app and creates a SYSTEM startup task. Only the public certificate is returned by run-command. Secrets/private keys are not parameters or printed. It produces the Teams ZIP and checks `https://<MEDIA_FQDN>/healthz`. Task restarts are configured; a failure is explicit, not a successful fallback.

`media-certificate.ps1` pins Posh-ACME 4.32.0, reserves the challenge URL for SYSTEM, and imports a full-chain PFX directly into Key Vault using a random in-memory export password. ACME state is protected under `C:\ProgramData\ZavaMedia\acme`; no private key or password enters run-command output. **Verified in 6b:** Let's Encrypt issued the Azure DNS-label certificate, imported as `media-tls`, expiring 2027-02-24 19:22:03 UTC. Rerun `media-host.ps1` at least monthly and before certificate expiry: within 30 days of expiry it renews, imports, updates the app's public certificate and restarts the task. There is no unattended renewal scheduler; do not assume the certificate or app credential renews merely because the VM is running.

**Live hosting proof, 2026-11-26:** https://zava-media-3fsyvjqqjw5go.westus3.cloudapp.azure.com/healthz returned `service=media-bot`, `recording=false`, `maxConcurrentMeetings=1` with normal certificate validation, and remained healthy after rebooting the idle media VM without manually starting the task. `ZavaMediaBot` is running as SYSTEM with one at-startup trigger. Native package build assets and the required Graph service base URL are included; a managed-only compile had not exposed those startup defects. This is SDK/startup/TLS readiness, not a Teams handshake, join or capacity load test.

For this existing environment, `pwsh infra\hooks\deploy-service.ps1 -Service api` (then `-Service bridge`) builds the same Dockerfiles from `git archive HEAD`, avoiding untracked caches and conductor notes. Commit runtime inputs first. It preserves existing Container Apps settings/secrets and does not republish an unchanged hosted-agent version. Then run `pwsh infra\hooks\entra-post.ps1` to merge the independent native routes into existing Easy Auth exclusions, and `pwsh infra\hooks\verify.ps1`. The normal empty-environment `azd up` path is unchanged.

Keep `BRIDGE_MODE=Acs` until HTTPS readiness and meeting RSC are verified. Then persist `azd env set BRIDGE_MODE GraphMedia` and apply `az containerapp update -g rg-zava-scrum-zavaw3 -n ca-api-3fsyvjqqjw5go --set-env-vars "Bridge__Mode=GraphMedia"`; image-only deployment does not apply Bicep environment values. Restore `Acs` with the same two commands, leaving the VM intact. Direct/Hosted voice selection is independent; Direct remains deployed. The media routes are Easy Auth exclusions but have an independent ASP.NET authentication scheme requiring both `x-bridge-key` and `x-bridge-token`; the loader checks explicit team ownership and the stored link. A portal bearer token alone is insufficient. Native unauthenticated context returned 401 in 6b, not an Easy Auth redirect. Run `infra\hooks\verify.ps1` and native G6/G2/barge-in/G1 checks; do not apply old ACS/relay or fallback regression numbers to the native path. Preserve ACS if any gate fails. No native meeting-derived data is saved.

The inspected `team-payments` schedule is disabled/simulated with no stored meeting links, and Daniel/Maya have no roster Entra object ids (Admin does). The team's administrator/lead must approve a real sprint calendar through **Schedule > Proposed sprint calendar > Propose calendar > Approve**, or configure a real stored meeting link through the normal schedule flow. For this seed-backed roster, the deployment operator maps actual signed-in tenant accounts with `az containerapp update -g rg-zava-scrum-zavaw3 -n ca-api-3fsyvjqqjw5go --set-env-vars "Demo__MemberEmails__daniel=<real-Daniel-UPN>" "Demo__MemberEmails__maya=<real-Maya-UPN>"`; do not use the `m-` prefix in those mapping keys. Then the team administrator/lead invokes authenticated `POST /api/teams/team-payments/members/sync` and verifies non-null distinct `entraObjectId` values through `GET /api/teams/team-payments/members` before preparing a fresh session. These are actual-account choices, not invented object ids or scripted cast labels. Other seed-backed teams use their own configured seed file/mappings.

Prompt 7 adds no resources or roles. `media-host.ps1` checks both NativeMedia and
SkiaSharp payloads before uploading; the actual VM renders a real waiting frame
at startup. `/healthz` must report `boardRendererReady:true`, `boardWidth:1280`
and `boardHeight:720`; `verify-media.ps1` checks them without claiming an encoder
or participant sample. `teams-app.ps1` preserves the deployment's SSO resource
and adds only scheduled meeting event RSC. Empty-environment hooks/templates
include the same changes. No manual private keys, parameters or portal resources.

**Step 7 deployment proof:** API/native source `0d07e83`; API image
`m11-7-0d07e83` (ready revision `--0000080`), bridge `m11-7-c25287f`
(`--0000007`). Actual native DLL hash matches the local published Release DLL;
its product version includes `0d07e83` and the running SYSTEM task retains one
boot trigger. Mandatory `verify.ps1 -GateOutput docs\gates\m11-p7\after` passes
all eight checks, including the signed-in approver's Azure DevOps comment and
audit chain. One-run fallback p95 worsens 39.16% over 12 turns; native board/
event/encoder/G1 acceptance remains n=0. No resources or roles were added,
no default was switched, and all deployed services remain running.

## Agent identity governance (prompt 5B, ADR 0022)

`infra/hooks/agent-identity.ps1` (postdeploy) checks the hosted agent's Entra Agent ID: it is an agent identity, holds no secret or certificate, has a sponsor, and prints its recent sign-ins. Human gates, for a Conditional Access administrator (nothing in the deployment can create tenant policy):

| Gate | Who | Action |
| --- | --- | --- |
| Conditional Access for agents | Conditional Access Administrator | Entra admin centre > Protection > Conditional Access > New policy > Target resources > Agents (agent identities): (1) block agent identities with high risk (Agent ID Protection risk); (2) block agent identities tagged non-production from production resources. Test in report-only first. |
| Sponsor attestation | The agent's sponsor (`admin@` by default) | Quarterly: Entra admin centre > Agent ID > the agent > Sponsors and Access reviews; confirm the agent is still needed and its roles (Cognitive Services User and Azure AI User on the Foundry account) are still right. Replace the sponsor when they leave. |
