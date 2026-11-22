# Rebrand: Azure resources and the two options

**Status: waiting for a decision from James. Nothing was run against Azure.**

Azure resources cannot be renamed in place; a new name creates a new resource. So the rebrand changes the **defaults for new environments** and leaves the existing demo environment alone.

## What changed in the repo

| File | Change |
| --- | --- |
| `infra/main.bicep` | New parameter `namePrefix` (default `zava-scrum`) builds the names of the resources the template creates: `acs-<prefix>-demo`, `id-<prefix>-<token>`, `acr<prefix without dashes><token>`, `cosmos-<prefix>-<token>`, `cae-<prefix>-<token>`. New parameter `cosmosDatabaseName` (default: the prefix without dashes, `zavascrum`) names the Cosmos database and is passed to the API as `Cosmos__Database`. |
| `infra/main.bicep` | `foundryName`, `logAnalyticsName` and `appInsightsName` no longer have hard-coded defaults (they pointed at the existing `mcm` resources). They are required and come from the azd environment. |
| `infra/main.parameters.json` | New entries: `NAME_PREFIX` (default `zava-scrum`), `FOUNDRY_NAME`, `LOG_ANALYTICS_NAME`, `APP_INSIGHTS_NAME`. |
| `infra/hooks/postdeploy.ps1` | The Azure Bot name comes from `BOT_NAME` (default `bot-zava-scrum-demo`) instead of the hard-coded `bot-mcm-scrum-demo`. |
| `azure.yaml` | `name: zava-scrum-agent`; the API project path follows the renamed folder. |
| `src/Zava.Scrum.Infrastructure/Persistence/CosmosOptions.cs` | The default database name is `zavascrum`. |

With `namePrefix = mcm-scrum` every name above is exactly what the existing environment was deployed with (`id-mcm-scrum-<token>`, `acrmcmscrum<token>`, `cosmos-mcm-scrum-<token>`, `cae-mcm-scrum-<token>`, `acs-mcm-scrum-demo`, database `mcmscrum`). That is how Option A avoids duplicates. `bicep build` compiles the template (the only warnings are the existing `login.microsoftonline.com` linter warnings).

Not touched, because they are existing resources or registrations: `rg-mcm-scrum-demo-wus3`, `fdy-mcm-scrum-demo`, `proj-mcm-scrum-demo-wus3`, `log-fdy-mcm-scrum-demo-dybvsjflknh74`, `appi-fdy-mcm-scrum-demo-9280`, `bot-mcm-scrum-demo`, the Entra app registrations `mcm-scrum-agent-bot` and `mcm-scrum-agent-calendar`, and the AppHost `UserSecretsId` `mcm-scrum-apphost`. The bridge's default `SPEECH_ENDPOINT` (`src/meeting-bridge/src/config.ts`) still points at `fdy-mcm-scrum-demo`; Azure sets `SPEECH_ENDPOINT` from the Foundry resource in a deployment, so the default only matters for local runs.

## Option A: keep the current resources (only the display text changes)

Nothing is created and no data moves. Tell azd the existing names, then deploy as before:

```powershell
azd env select <existing-env>
azd env set NAME_PREFIX mcm-scrum
azd env set FOUNDRY_NAME fdy-mcm-scrum-demo
azd env set LOG_ANALYTICS_NAME log-fdy-mcm-scrum-demo-dybvsjflknh74
azd env set APP_INSIGHTS_NAME appi-fdy-mcm-scrum-demo-9280
azd env set BOT_NAME bot-mcm-scrum-demo
azd provision --preview   # must show no new resources and no renames
azd deploy                # only when the preview is clean
```

- **Existing data:** the Cosmos database stays `mcmscrum` (derived from `NAME_PREFIX`). Nothing is lost.
- **The names `mcm` remain visible** in the Azure portal and in resource URLs. They are allowlisted in `REPORT.md`.
- Re-upload `appPackage/zava-scrum-agent.zip` to Teams (the manifest version is 1.0.4; the app and bot ids are unchanged, so it updates the same app).

## Option B: create a fresh `zava` environment, then retire the old one

A new environment gets `zava` names everywhere the template creates a resource. The Foundry, Log Analytics and Application Insights resources and the Azure Bot are reused or created by hand first, because the template only references them.

```powershell
azd env new zava-scrum-demo
azd env set AZURE_LOCATION <region>
azd env set NAME_PREFIX zava-scrum            # the default; shown for clarity
azd env set FOUNDRY_NAME <foundry-account>    # an existing account, or a new zava-named one
azd env set FOUNDRY_PROJECT_NAME <project>
azd env set LOG_ANALYTICS_NAME <workspace>
azd env set APP_INSIGHTS_NAME <component>
azd env set BOT_APP_ID <bot app id>           # a new Entra app for a new bot, or the existing one
azd env set BOT_NAME bot-zava-scrum-demo
azd env set ADO_ORG_URL https://dev.azure.com/<your-organisation>
azd env set BOT_CLIENT_SECRET <secret>
azd env set PORTAL_CLIENT_ID <id>
azd env set PORTAL_CLIENT_SECRET <secret>
azd env set BRIDGE_API_KEY <key>
azd up
# when the new environment works:
azd env select <old-env>
azd down --purge
```

- **Existing data:** a new Cosmos account and database (`zavascrum`) start empty. Demo data is recreated with **Reset demo** and the seeder; nothing is copied from the old database. Sessions, follow-ups and cards in the old environment are lost when it is retired.
- **Teams and the bot:** a new Azure Bot needs a new bot app id, so the Teams app must be re-registered with a new manifest `id` and `botId` (the repo manifest keeps the current GUIDs). If you reuse the existing bot app registration, keep the manifest ids and only point the bot endpoint at the new API (`postdeploy.ps1` does this).
- **Entra apps:** rename the display names `mcm-scrum-agent-bot` and `mcm-scrum-agent-calendar` in Entra if you want them to match (`az ad app update --id <app id> --display-name zava-scrum-agent-bot`); the app ids do not change.

## Decision needed

**ASK James:** Option A or Option B. Until then the repo works for both: Option A needs only the `azd env set` commands above.
