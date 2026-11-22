# SeedAdo

Console tool that creates the demo backlog in the Azure DevOps sandbox project (SPEC 9.2): the `Payments` area, the `Payments (Demo)` team, a current `Demo Sprint` iteration, and the 19 work items in `demo/seed-ado.json`.

- **Idempotent.** Items are found by their `demo-key-*` tag and reset in place; the sprint dates are moved so the demo sprint is always current. Run it again before every demo.
- **Auth.** `DefaultAzureCredential` only (run `az login`); no PAT. Pass `--tenant <id>` (or set `Azure__TenantId`) when several accounts are signed in.
- **Run.** `dotnet run --project tools/SeedAdo -- --tenant <tenant-id>` (optional `--org`, `--file`). Use a sandbox project only.
- **Flags it plants.** Two stale stories (`demo-age-3`), one blocked bug (`Blocked`, `demo-age-4`), one overdue task, one task with no recent update (`demo-quiet-3`), and two unassigned stories, plus healthy items and three closed stories for burndown.
- **Owners.** Every assigned item is assigned to the signed-in user and tagged `demo-owner-{key}`; the API maps the tag to the cast member (ADR 0002).
