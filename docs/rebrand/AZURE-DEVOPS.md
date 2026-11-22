# Rebrand: Azure DevOps demo project

**Status: the dry run is done and is waiting for James's review before `--apply` is run. Nothing was written to Azure DevOps.**

## What changed in the repo

- `tools/SeedAdo`: the default project is now `Zava-Scrum-Demo` (from `demo/seed-ado.json`); the team description and the demo tag use the new brand (`mcm-demo` becomes `zava-demo`). The seed stays idempotent.
- New `--rebrand` mode (`Rebrander.cs`, `RebrandMap.cs`). It is a **dry run by default**; `--apply` writes. It uses the same authentication as the seeder (`DefaultAzureCredential`; no PAT).
  1. renames the project (`PATCH _apis/projects/{id}`, then polls the operation; the project id stays the same);
  2. renames teams whose names contain a token (the project's default team "MCM-Scrum-Demo Team" does);
  3. updates title, description, acceptance criteria and tags of Epics, Features, User Stories, Tasks and Bugs that contain a token, with the mapping table;
  4. leaves history, comments by people and attachments alone (it only writes the four fields; every change adds one revision to the item);
  5. prints a table of every change.
- `Team:Project` and `Team:AreaPath` (`appsettings.json`, `TeamDefaultsOptions`), `infra/hooks/postdeploy.ps1` and the docs now say `Zava-Scrum-Demo`. **They only work after the rename has run.**

## Commands (run in this order)

```powershell
# 1. Review the dry run (read only). Org and tenant come from your user secrets; nothing is written.
dotnet run --project tools/SeedAdo -- --rebrand --dry-run --org <org-url> --tenant <tenant-id>

# 2. After James approves the table below, write the changes.
dotnet run --project tools/SeedAdo -- --rebrand --apply --org <org-url> --tenant <tenant-id>

# 3. Re-seed under the new project name (idempotent). Do this only after step 2: it finds items by the zava-demo tag.
dotnet run --project tools/SeedAdo -- --org <org-url> --tenant <tenant-id>
```

After the rename, stored team settings in an existing environment still hold the old project and area path (`teams.json` locally, the `teams` documents in Cosmos). Update them with `PUT /api/teams/team-payments` (project `Zava-Scrum-Demo`, area path `Zava-Scrum-Demo\Payments`) or run **Reset demo** on a fresh store. Iteration paths in Azure DevOps follow the project rename automatically.

## Dry-run result (`--rebrand --dry-run`)

The dry run found the project and the existing work items. The changes are the project name, the default team name (`MCM-Scrum-Demo Team`) and the **tags** (`mcm-demo`) on the 29 seeded items. No titles, descriptions or acceptance criteria contain a token.

| Id | Field | Old value | New value |
| --- | --- | --- | --- |
| eaadbc0f-a70c-4958-a55f-ae33aedb0d5e | Project name | MCM-Scrum-Demo | Zava-Scrum-Demo |
| a21aa8f7-a119-48c4-b153-60810eb37d14 | Team name | MCM-Scrum-Demo Team | Zava-Scrum-Demo Team |
| 1 | Tags | demo-age-3; demo-key-retry-story; demo-owner-admin; mcm-demo | demo-age-3; demo-key-retry-story; demo-owner-admin; zava-demo |
| 2 | Tags | demo-age-3; demo-key-settlement-validation; demo-owner-maya; mcm-demo | demo-age-3; demo-key-settlement-validation; demo-owner-maya; zava-demo |
| 3 | Tags | demo-key-plan-summary; demo-owner-daniel; mcm-demo | demo-key-plan-summary; demo-owner-daniel; zava-demo |
| 4 | Tags | demo-key-idempotency; demo-owner-daniel; mcm-demo | demo-key-idempotency; demo-owner-daniel; zava-demo |
| 5 | Tags | demo-key-pci-exceptions; demo-owner-maya; mcm-demo | demo-key-pci-exceptions; demo-owner-maya; zava-demo |
| 6 | Tags | demo-key-reminder-sms; demo-owner-daniel; mcm-demo | demo-key-reminder-sms; demo-owner-daniel; zava-demo |
| 7 | Tags | demo-key-ledger-refactor; mcm-demo | demo-key-ledger-refactor; zava-demo |
| 8 | Tags | demo-key-dispute-evidence; mcm-demo | demo-key-dispute-evidence; zava-demo |
| 9 | Tags | demo-key-tokenisation; demo-owner-daniel; mcm-demo | demo-key-tokenisation; demo-owner-daniel; zava-demo |
| 10 | Tags | demo-key-csv-export; demo-owner-maya; mcm-demo | demo-key-csv-export; demo-owner-maya; zava-demo |
| 11 | Tags | demo-key-status-api; demo-owner-daniel; mcm-demo | demo-key-status-api; demo-owner-daniel; zava-demo |
| 12 | Tags | Blocked; demo-age-4; demo-key-gateway-timeouts; demo-owner-maya; mcm-demo | Blocked; demo-age-4; demo-key-gateway-timeouts; demo-owner-maya; zava-demo |
| 13 | Tags | demo-key-partial-refund-rounding; demo-owner-maya; mcm-demo | demo-key-partial-refund-rounding; demo-owner-maya; zava-demo |
| 14 | Tags | demo-key-retry-load-test; demo-owner-admin; mcm-demo | demo-key-retry-load-test; demo-owner-admin; zava-demo |
| 15 | Tags | demo-key-gateway-logging; demo-owner-daniel; mcm-demo | demo-key-gateway-logging; demo-owner-daniel; zava-demo |
| 16 | Tags | demo-key-settlement-schema-review; demo-owner-maya; demo-quiet-3; mcm-demo | demo-key-settlement-schema-review; demo-owner-maya; demo-quiet-3; zava-demo |
| 17 | Tags | demo-key-batch-runbook; demo-owner-daniel; mcm-demo | demo-key-batch-runbook; demo-owner-daniel; zava-demo |
| 18 | Tags | demo-key-qa-demo-data; demo-owner-maya; mcm-demo | demo-key-qa-demo-data; demo-owner-maya; zava-demo |
| 19 | Tags | demo-key-audit-report; demo-owner-daniel; mcm-demo | demo-key-audit-report; demo-owner-daniel; zava-demo |
| 20 | Tags | demo-key-b-email-receipt; mcm-demo | demo-key-b-email-receipt; zava-demo |
| 21 | Tags | demo-key-b-decline-reason; demo-owner-daniel; mcm-demo | demo-key-b-decline-reason; demo-owner-daniel; zava-demo |
| 22 | Tags | demo-key-b-apple-pay; mcm-demo | demo-key-b-apple-pay; zava-demo |
| 23 | Tags | demo-key-b-partial-payments; demo-owner-maya; mcm-demo | demo-key-b-partial-payments; demo-owner-maya; zava-demo |
| 24 | Tags | demo-key-b-statement-pdf; demo-owner-admin; mcm-demo | demo-key-b-statement-pdf; demo-owner-admin; zava-demo |
| 25 | Tags | demo-key-b-audit-log; demo-owner-daniel; mcm-demo | demo-key-b-audit-log; demo-owner-daniel; zava-demo |
| 26 | Tags | demo-key-b-address-update; demo-owner-maya; mcm-demo | demo-key-b-address-update; demo-owner-maya; zava-demo |
| 27 | Tags | demo-key-b-settlement-import; mcm-demo | demo-key-b-settlement-import; zava-demo |
| 28 | Tags | demo-key-b-collector-dashboard; demo-owner-admin; mcm-demo | demo-key-b-collector-dashboard; demo-owner-admin; zava-demo |
| 29 | Tags | demo-key-b-reporting-model; demo-owner-daniel; mcm-demo | demo-key-b-reporting-model; demo-owner-daniel; zava-demo |
31 change(s) planned.

**ASK James:** review the table, then run step 2.
