# Rebrand report: MCM to Zava

Commit range: `780f79d..HEAD` (the first rebrand commit is `6d4f308`). Nothing was pushed. Nothing was run against Azure; Azure DevOps was only read (the dry run).

## Decisions waiting for James

1. **Azure option** (`docs/rebrand/AZURE.md`): Option A keeps the current resources and only changes display text; Option B creates a fresh `zava` azd environment and retires the old one. The repo works for both. Nothing was deployed.
2. **Azure DevOps `--apply`** (`docs/rebrand/AZURE-DEVOPS.md`): review the dry-run table (31 changes), then run `dotnet run --project tools/SeedAdo -- --rebrand --apply ...`. Until it has run, the repo's default project name `Zava-Scrum-Demo` does not exist in Azure DevOps.
3. **People's names in the public repo**: Rajeev, Tyler and Nate are named in the repo docs and (Rajeev) in four code comments. Not changed.

Also waiting (smaller): the `Encore` references in the OneDrive docs (rule 11, report before changing), and the diagrams and Office files listed below that need regenerating.

## Changes by phase

| Phase | Commit(s) | Result |
| --- | --- | --- |
| 0 Inventory | `6d4f308` | `docs/rebrand/INVENTORY.md`: 556 files, 1850 hits in the repo |
| 1 Solution and .NET projects | `dc5c3df` (527 `git mv` renames, no content change), `4704b69` (518 files, namespaces, types, project references, Dockerfile, `azure.yaml` path) | Solution `ZavaScrum.slnx`; projects `Zava.Scrum.*`. `dotnet build` 0 warnings; 282 + 100 + 42 + 7 tests pass; `dotnet format --verify-no-changes` clean |
| 2 UI text and prompts | `a24f576` (31 files) | Agent prompts (10), bot greeting, join display name, attribution text, `.ics` ids, room and portal titles, bridge text, tests that assert the text. Portal lint, typecheck and build pass; bridge lint and unit tests pass |
| 3 Teams app | `7e9ae8b` (zip rename), `ab45231` | Manifest names and developer name; version 1.0.3 to 1.0.4; `appPackage/zava-scrum-agent.zip` rebuilt from the manifest and the unchanged icons. Manifest `id` and bot id kept. Icons show "SM", not the old name |
| 4 Infra | `e9e15f9` | Name prefix and Cosmos database became parameters; existing resources untouched (`AZURE.md`). `bicep build` compiles |
| 5 Azure DevOps | `b288c20` | Seeder defaults, `--rebrand` mode (dry run default), dry run recorded in `AZURE-DEVOPS.md` |
| 6 Repo docs | `5958f1d` (2 PNG renames), `1f0698d` (17 files) | README, `.github/copilot-instructions.md`, docs, READMEs; `CHANGELOG.md` created |
| 7 OneDrive docs | no git (not a repo) | 133 text files edited in place, 183 files renamed. Backup: `C:\Users\FORDJA~1\AppData\Local\Temp\zava-rebrand-backup-20261122-151253` |

## Files renamed

- **Repo:** 526 renames, all `git mv` (see `docs/rebrand/RENAMES.md` for every old and new name). `src/Mcm.Scrum.*` to `src/Zava.Scrum.*` (and the files inside whose names had the token), `McmScrum.slnx` to `ZavaScrum.slnx`, `appPackage/mcm-scrum-agent.zip` to `zava-scrum-agent.zip`, and two diagram PNGs under `docs/`. Git reports every one as a rename. The local `src/.../data` folder moved with its project.
- **OneDrive:** 183 files (27 `.html`, 56 `.svg`, 76 `.png`, 15 `.md`, 6 `.pdf`, 3 `.docx`), for example `MCM-Scrum-Agent-PoC-Spec.md` to `Zava-Scrum-Agent-PoC-Spec.md` and `MCM AI Scrum Master Agent - Production Specification.pdf` to `Zava AI Scrum Master Agent - Production Specification.pdf`. Links between the docs were fixed; the talk-track link checker (`Presenter Pack/build/check_links.py`) reports 88 links, 0 problems. No folder name contained a token.

## Configuration keys

**None renamed**: no section name, option, environment variable or user-secret key contained the token. Some default values changed (`Team:Project`, `Team:AreaPath`, the Cosmos database name). See `docs/rebrand/SECRETS-MIGRATION.md`. `UserSecretsId` values are unchanged.

## Azure DevOps dry run

`--rebrand --dry-run` against the real organisation (read only): 31 changes planned. The project `MCM-Scrum-Demo` becomes `Zava-Scrum-Demo`; its default team `MCM-Scrum-Demo Team` becomes `Zava-Scrum-Demo Team`; the tag `mcm-demo` on the 29 seeded work items becomes `zava-demo`. No title, description or acceptance criteria contained a token. Full table in `docs/rebrand/AZURE-DEVOPS.md`.

## Binary files to regenerate (James)

- **Repo:** `docs/Zava-Scrum-Agent-PoC-Architecture.png` and `docs/Zava-Scrum-Agent-PoC-Standup-Flow-As-Built.png`: renamed, but the text drawn inside still shows the old name. The Teams package icons need no change.
- **OneDrive diagrams:** all 76 `.png` files were renamed only. Their `.svg` siblings were rewritten, so re-render each PNG from its SVG (or run the build scripts in `Presenter Pack/build`, which now use the new names).
- **OneDrive Office and PDF files** (renamed only, content not edited; they still contain the old name): `Zava AI Scrum Master Agent - Production Specification.pdf`, `... - Solution Architecture.docx`, `... - Solution Architecture.pdf`, plus their copies under `PoC As-Built/Production-docs` and `Original-copies` (9 files in all). Links inside these files to other renamed files, if any, are not updated.
- The ACR workbook `Zava-Scrum-Agent-ACR-Estimate.xlsx` is only named in notes; it was never built.

## People and email findings (not changed)

- **Repo:** Rajeev (`docs/CEREMONIES.md`, `docs/REQUIREMENTS-AUDIT.md`, `docs/SPEC.md`; code comments in `src/Zava.Scrum.Domain/Calendars/CalendarDurations.cs`, `Planning/PlanningSource.cs`, `Standups/RetroFormat.cs`, `src/Zava.Scrum.Infrastructure/DependencyInjection.cs`), Tyler and Nate (`docs/REQUIREMENTS-AUDIT.md`). No `Monique`. No customer email addresses (the `@x.com` addresses are test fixtures) and no phone numbers. The repo is public, so James decides.
- **OneDrive:** names (Rajeev, Monique, Tyler and others) are kept as asked.

## Allowlist (hits that remain on purpose)

Search used: `rg -n -i --hidden -g '!.git' -g '!**/bin/**' -g '!**/obj/**' -g '!**/node_modules/**' "mcm|midland|encore"`.

- **The rebrand's own documents:** the CHANGELOG.md entry (the wording was prescribed), everything under `docs/rebrand/` (they name the old tokens by necessity), and in `tools/SeedAdo`: `RebrandMap.cs` (the mapping table), `Rebrander.cs` and the `--old-project` default `MCM-Scrum-Demo` in `Program.cs`, and the `--rebrand` note in `tools/SeedAdo/README.md`.
- **Existing Azure resources and registrations kept under Option A:** `rg-mcm-scrum-demo-wus3`, `fdy-mcm-scrum-demo` (including the bridge's default `SPEECH_ENDPOINT` in `src/meeting-bridge/src/config.ts`), `proj-mcm-scrum-demo-wus3`, `log-fdy-mcm-scrum-demo-dybvsjflknh74`, `appi-fdy-mcm-scrum-demo-9280`, `bot-mcm-scrum-demo`, the Entra app names `mcm-scrum-agent-bot` and `mcm-scrum-agent-calendar` (named in `docs/DEMO.md`), and the AppHost `UserSecretsId` `mcm-scrum-apphost`.
- **Customer folder name:** `...\Customers\Midland Credit Management\` (not renamed, by rule).
- **OneDrive:** the existing Azure resource names above and the derived names (`id-mcm-scrum-...`, `acrmcmscrum...`, `cosmos-mcm-scrum-...`, the Cosmos database `mcmscrum`) in `BINDINGS.md`, the architecture docs and `build_reader.py`; the instruction document `Presenter Pack/prompts/ghcp-rebrand-to-zava.md`, left unedited because it lists the old names on purpose; and the `Encore` hits (below).
- **False positives:** none in text. Base64 image data in the OneDrive HTML files contains letter sequences that look like tokens; those were protected from replacement.
- **Binary content** (PNG, PDF, DOCX) that still shows the old name, listed above.

## Things I was not sure about

- **`Encore` (rule 11).** The rule says to report each hit before changing it, and James was not available. Eight hits in the OneDrive `Solution-Architecture` docs (`Midland Credit Management (MCM / Encore Capital Group)`, now `Zava (Zava / Encore Capital Group)`) were left as they are. There were none in the repo.
- **Rule order.** The table lists `MCM` before `MCM_`; applied literally, `MCM_` would never match. I applied `MCM_` first. There were no `MCM_` hits in the repo.
- **`Zava (Zava)`.** The literal mapping turned "Midland Credit Management (MCM)" in `.github/copilot-instructions.md` into "Zava (Zava)". I collapsed it to "Zava". It is the only wording change outside the table.
- **Backlog-risk prompt line endings.** `agents/instructions/backlog-risk.md` had Windows line endings in the working copy and Unix line endings in git. I restored the committed form before the edit so the diff is one line.
- **Script files in OneDrive.** The brief lists `.md`, `.html`, `.svg`, `.json` and `.txt`. I also applied the naming change to `.py`, `.ps1` and `.css`, because the files they open were renamed and the build scripts would otherwise break. 
- **Office, PDF and PNG files renamed.** The brief says to rename files with a token in the name; I did, without editing contents.
- **Azure names as parameters.** `foundryName`, `logAnalyticsName` and `appInsightsName` are now required azd values (no hard-coded default), so an existing environment must set `FOUNDRY_NAME`, `LOG_ANALYTICS_NAME`, `APP_INSIGHTS_NAME`, `BOT_NAME` and `NAME_PREFIX=mcm-scrum` before `azd provision` (commands in `AZURE.md`).
- **Seeder before rename.** The normal seed finds items by the `zava-demo` tag, so run `--rebrand --apply` before the first seed under the new project name, or it fails to find the project.
- **Portal run.** The API does not serve the portal locally (the Docker image copies the build into `wwwroot`), so I checked the built `dist/index.html` title ("Zava Scrum Agent") and the sign-in heading instead of a browser session.
- **Tags on existing work items.** The `--rebrand` mode adds one revision to each changed item; history and comments by people are not touched.
