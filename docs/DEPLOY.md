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
| `MEDIA_HOST_ENABLED`, `MEDIA_VM_SIZE` | no | `false` by default; opt-in public Windows media VM, initially `Standard_D3_v2` with hard call cap 1. Enable only in step 6b, `zavaw3`/`westus3`. |
| `MEDIA_SERVICE_FQDN` | no | Optional owned DNS name covered by a trusted media certificate. Empty uses the Azure public IP DNS name; obtaining a trusted certificate for that name is an operator gate, not assumed automatic. |
| `BRIDGE_MODE` | no | `Acs` (default), `Simulated`, or `GraphMedia`. Select native media only after its HTTPS bootstrap and RSC installation; it also requires `VOICE_ROUTE=Hosted`. |
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

**Do not execute the following in step 6a.** Step 6b alone enables/provisions the VM in `zavaw3`, `westus3`; never use `mcm` or its Entra objects. Keep `BRIDGE_MODE=Acs` while bringing up the alternative. No new resources or hourly cost were added in 6a; obtain the actual Windows VM/public IP/disk/vault/storage price in 6b, not an unverified estimate.

```pwsh
azd env select zavaw3
azd env set MEDIA_HOST_ENABLED true
azd env set MEDIA_VM_SIZE Standard_D3_v2
azd env set VOICE_ROUTE Hosted
azd env set BRIDGE_MODE Acs
# Optional: an owned DNS name for which a trusted certificate can be issued.
azd env set MEDIA_SERVICE_FQDN <owned-media-fqdn>
# Provision/deploy only in step 6b. Follow its validation/deployment contract.
```

The opt-in module uses the pinned AVM Windows VM resource with four vCPU, one-call cap, a public IP and TCP 443/8445 only. The subnet exists solely for the Azure NIC; there are no private endpoints. The VM identity gets Key Vault Secrets User on its media vault and Blob Reader on its artefact container. The named deployer gets certificate/secret operator roles on that vault and Blob Contributor on that container. No RDP ingress, tenant-wide joining role or extra tenant policy is added.

The postdeploy hook creates the dedicated `zava-media-zavaw3` calling app/service principal, Azure Bot and Teams calling webhook; it never changes the existing messaging bot identity. An Entra Privileged Role Administrator/Global Administrator must be able to consent to the minimum `Calls.AccessMedia.All` grant; if automation is refused, grant that **application** permission on the dedicated calling app only in Entra admin centre > App registrations > the calling app > API permissions > Microsoft Graph, then Grant admin consent. Do not add `Calls.JoinGroupCall.All` as a workaround. Per-meeting joining is the package's RSC permission, not this tenant-wide media permission.

| Gate | Who | Exact action |
| --- | --- | --- |
| `MEDIA-TLS` | Owner of the chosen DNS name and a Key Vault certificate operator for `MEDIA_VAULT_NAME` | Point the owned `MEDIA_SERVICE_FQDN` at output `MEDIA_PUBLIC_IP`. Obtain a CA-trusted, exportable PFX including its private key and chain, with `MEDIA_FQDN` as its primary DNS name and more than 14 days remaining. Keep the local PFX in a protected operator folder, never the repo. Import with `az keyvault certificate import --vault-name <MEDIA_VAULT_NAME> --name media-tls --file <protected-empty-password-PFX>`. If using the default Azure DNS name, first prove that the certificate issuer can validate that name; no automatic issuance is assumed. |
| Calling-app consent | Entra Privileged Role Administrator or Global Administrator | If the scripted `Calls.AccessMedia.All` grant is refused, perform the dedicated-app consent described above, then rerun the hook. Never grant a broader join role. |
| `MEDIA-RSC` | Teams custom-app administrator/uploader and each meeting's organiser, permitted by existing tenant policy | Publish/approve generated `appPackage\dist\zava-media-bot.zip` through Teams admin centre > Teams apps > Manage apps > Upload new app if the organiser cannot sideload. In each scheduled meeting's Teams chat, add the **Zava Scrum Media** app and accept its resource-specific `Calls.JoinGroupCalls.Chat` permission for that chat. Do this before the ten unattended G6 joins. Do not alter tenant policy if installation is prohibited; record the gate and retain ACS. |
| Lobby admission | Meeting organiser | If existing meeting policy still sends the bot to the lobby, admit **Zava Scrum Media** from the meeting's lobby. Record that as a failed no-manual-step G6 case, not a successful unattended join. Do not relax tenant policy. |

After importing the certificate, rerun `pwsh infra\hooks\media-host.ps1` in the selected `zavaw3` environment. It publishes win-x64, uploads a hash-addressed private blob with Entra, delivers the existing bridge key to Key Vault, bootstraps the VM using IMDS tokens, verifies the ZIP hash/trusted certificate/hostname, installs the signed Microsoft VC++ runtime if needed, registers the public certificate on the calling app and creates a SYSTEM startup task. Only the public certificate is returned by run-command. Secrets/private keys are not parameters or printed. It produces the Teams ZIP and checks `https://<MEDIA_FQDN>/healthz`. Task restarts are configured; a failure is explicit, not a successful fallback.

Keep `BRIDGE_MODE=Acs` until HTTPS readiness and meeting RSC are verified. Then select `GraphMedia` and redeploy the API in step 6b. Its media routes are Easy Auth exclusions but have an independent ASP.NET authentication scheme requiring both `x-bridge-key` and `x-bridge-token`; the loader checks explicit team ownership and the stored link. A portal bearer token alone is insufficient. Run `infra\hooks\verify.ps1` as required by 6b, and the native G6/G2/barge-in/G1 checks; do not apply old ACS/relay numbers to the native path. Preserve ACS if any gate fails. No native meeting-derived data is saved.

## Agent identity governance (prompt 5B, ADR 0022)

`infra/hooks/agent-identity.ps1` (postdeploy) checks the hosted agent's Entra Agent ID: it is an agent identity, holds no secret or certificate, has a sponsor, and prints its recent sign-ins. Human gates, for a Conditional Access administrator (nothing in the deployment can create tenant policy):

| Gate | Who | Action |
| --- | --- | --- |
| Conditional Access for agents | Conditional Access Administrator | Entra admin centre > Protection > Conditional Access > New policy > Target resources > Agents (agent identities): (1) block agent identities with high risk (Agent ID Protection risk); (2) block agent identities tagged non-production from production resources. Test in report-only first. |
| Sponsor attestation | The agent's sponsor (`admin@` by default) | Quarterly: Entra admin centre > Agent ID > the agent > Sponsors and Access reviews; confirm the agent is still needed and its roles (Cognitive Services User and Azure AI User on the Foundry account) are still right. Replace the sponsor when they leave. |
