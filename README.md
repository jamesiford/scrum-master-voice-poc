# Zava Scrum Master voice PoC

A demo-first proof of concept of a voice-led "junior Scrum Master" for Zava. It joins a Teams standup by voice, grounds questions in Azure DevOps, shares a live standup board, posts follow-up cards, and writes to Azure DevOps only after a person approves.

## Licence

MIT. See [LICENSE](LICENSE).

## Prerequisites

- .NET 10 SDK
- Node.js 22
- Azure CLI (`az`) and Azure Developer CLI (`azd`)
- Access to the Foundry project (chat and Voice Live deployments) and the Azure DevOps project
- Microsoft Edge (the bridge drives headless Chromium through Playwright)

## Quick start (local, Aspire)

```
az login --tenant <tenant-id>
dotnet run --project tools/SeedAdo -- --tenant <tenant-id> --org https://dev.azure.com/<your-organisation>   # seed the sandbox; safe to repeat
npm install --prefix src/meeting-bridge
npm install --prefix src/admin-portal
dotnet run --project src/Zava.Scrum.AppHost --launch-profile http
```

Real values are not stored in the repository. Set them once as user secrets (Azure access uses `DefaultAzureCredential`):

```
dotnet user-secrets set "Foundry:ProjectEndpoint" "https://<foundry>.services.ai.azure.com/api/projects/<project>" --project src/Zava.Scrum.Api
dotnet user-secrets set "Ado:Org" "https://dev.azure.com/<your-organisation>" --project src/Zava.Scrum.Api
dotnet user-secrets set "Azure:TenantId" "<tenant-id>" --project src/Zava.Scrum.Api
dotnet user-secrets set "Azure:TenantId" "<tenant-id>" --project src/Zava.Scrum.AppHost
dotnet user-secrets set "Speech:Endpoint" "https://<foundry>.cognitiveservices.azure.com" --project src/Zava.Scrum.AppHost
# Optional: Identity:AdminObjectIds (comma-separated Entra object ids allowed to decide any card and run the "prepare sprint planning" chat command). Approvals also need Azure:TenantId.
# Teams only (secrets): Bot:AppId, Connections:ServiceConnection:Settings:{ClientId,ClientSecret,TenantId,AuthorityEndpoint}, Acs:ConnectionString, Bridge:ApiKey
```

The Aspire dashboard (http://localhost:15888) lists the portal, bridge and API URLs; the API is on http://localhost:5080 and the bridge on http://localhost:5090. All configuration keys are listed in [SPEC section 17](docs/SPEC.md).

Checks:

```
dotnet build ZavaScrum.slnx
dotnet test ZavaScrum.slnx
dotnet format ZavaScrum.slnx --verify-no-changes
npm run lint --prefix src/meeting-bridge
npm run lint --prefix src/admin-portal
npm run e2e --prefix src/meeting-bridge   # needs the stack running
```

## Deploy (azd)

```
azd env set BOT_APP_ID <id>
azd env set BOT_CLIENT_SECRET <secret>
azd env set PORTAL_CLIENT_ID <id>
azd env set PORTAL_CLIENT_SECRET <secret>
azd env set BRIDGE_API_KEY <key>
azd env set FOUNDRY_PROJECT_NAME <foundry project name>
azd env set ADO_ORG_URL https://dev.azure.com/<your-organisation>
azd up
```

Infrastructure is in `infra/` (Bicep). The Foundry, Log Analytics and Application Insights resources must already exist. After changing Bicep parameters, run `azd provision` before `azd deploy`.

## Demo

Runbook and storyboard: [docs/DEMO.md](docs/DEMO.md).

## Documents

- [docs/SPEC.md](docs/SPEC.md): build spec and history
- [docs/AS-BUILT.md](docs/AS-BUILT.md): what was actually built
- [docs/CEREMONIES.md](docs/CEREMONIES.md): ceremony scope and the three patterns
- [docs/adr](docs/adr): decision records (ADR 0009: which document wins; ADR 0009b: Agent Framework and OpenTelemetry; ADR 0010: ceremony packs and carry-over; ADR 0011: sprint planning pack)
- [docs/DOC-DRIFT.md](docs/DOC-DRIFT.md): differences found between the documents and the code
- [.github/copilot-instructions.md](.github/copilot-instructions.md): engineering rules

## Known limits

- Teams app installation from the admin center failed repeatedly with a tenant/admin-portal error; sideloading per user is the workaround. Until installed, cards fall back to the portal outbox.
- Voice latency p50 is about 2 to 3 seconds against a 1 second target; each speaker now waits briefly while the agent checks their update.
- The utterance classifier recognises English phrasing; unusual wording can be missed.
- Only one API replica is supported (live state is in memory).
- Cosmos has public network access (Entra-only) because the tenant policy blocked private defaults.
- ACS is reached with Entra only; there is no connection string.
- `JsonFile*` class names are historical; they sit on `ICollectionStore` (file or Cosmos).
- `SessionRepositoryContract` exists for `ISessionRepository` only and runs against the JSON-file store only; there is no Cosmos run.
- OpenTelemetry to Application Insights is basic and on only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set; it has not been checked in a live Application Insights resource.
- Agents are Microsoft Agent Framework chat-client agents over the Foundry v2 SDK behind ports (ADR 0009b); Foundry hosted agents are not used.
- `knowledge/working-agreements.md` is not read by any code.
- The sprint calendar's Graph app (`Calendars.ReadWrite`, application) is not yet scoped to one mailbox with an Exchange application access policy; without `Calendar:Organizer` the `.ics` download is the fallback.
- Email follow-ups are recorded to an outbox; `Email:Mode=Graph` sends them through Microsoft Graph but has not been run against a live mailbox.
- Escalation cards have no Acknowledge or Reassign buttons.
- The shared board in a live meeting is still being validated.
