# Rebrand: configuration and secrets migration

**No configuration section, option name, environment variable or user-secret key contained the customer name, so no key was renamed.** There are no `MCM_` constants or variables in the repo, and `UserSecretsId` values are unchanged, so existing user secrets keep working.

What did change are **default values**. Check whether you override them:

| Setting | Old default | New default | What to do |
| --- | --- | --- | --- |
| `Team:Project` | `MCM-Scrum-Demo` | `Zava-Scrum-Demo` | Works only after the Azure DevOps rename has run (`docs/rebrand/AZURE-DEVOPS.md`). Until then, set the old value in user secrets or the environment |
| `Team:AreaPath` | `MCM-Scrum-Demo\Payments` | `Zava-Scrum-Demo\Payments` | Same |
| `Cosmos:Database` (`Cosmos__Database`) | `mcmscrum` | `zavascrum` | The existing Cosmos database is still `mcmscrum`. Infra now passes `Cosmos__Database` from the Bicep parameter; for the existing environment keep `NAME_PREFIX=mcm-scrum` (see `docs/rebrand/AZURE.md`) |
| `Bridge:DisplayName` | `MCM Scrum Agent` | `Zava Scrum Agent` | Display text only |

If you have set any of these locally, nothing needs to change. If you rely on the old defaults, set them explicitly. The commands below keep the old behaviour; do **not** run them if you want the new defaults. Values are placeholders, not secrets.

```powershell
# Keep working against the existing, not yet renamed Azure DevOps project:
dotnet user-secrets set "Team:Project" "MCM-Scrum-Demo" --project src/Zava.Scrum.Api
dotnet user-secrets set "Team:AreaPath" "MCM-Scrum-Demo\Payments" --project src/Zava.Scrum.Api

# Keep using the existing Cosmos database (Azure only; the local JSON store has no database name):
az containerapp update -n <api-app> -g <resource-group> --set-env-vars Cosmos__Database=mcmscrum
```

Local user secrets live under the project's `UserSecretsId` (a GUID for the API), which is unchanged. The `src/Mcm.Scrum.Api` folder was renamed to `src/Zava.Scrum.Api`; its local `data/` folder moved with it (it is git-ignored).

New azd environment variables (for `azd`, not user secrets): `NAME_PREFIX`, `FOUNDRY_NAME`, `LOG_ANALYTICS_NAME`, `APP_INSIGHTS_NAME`, `BOT_NAME`. See `docs/rebrand/AZURE.md`.

## Other things that reset
- **Admin portal:** no `localStorage` or `sessionStorage` keys contained the customer name, so saved preferences are kept.