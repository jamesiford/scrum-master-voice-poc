# Rebrand inventory: MCM to Zava (Phase 0)

Generated from `git ls-files` at the start of the rebrand (tracked files only; `bin`, `obj`, `node_modules` are not tracked). Search pattern: case-insensitive `mcm|midland|encore`.

## 1. Content hits by area

| Area | Files | Hits |
| --- | ---: | ---: |
| C# solution and projects | 449 | 1234 |
| Tests | 64 | 375 |
| Repo docs | 11 | 160 |
| Solution root and other | 2 | 20 |
| Repo instructions (.github) | 1 | 15 |
| Infra (Bicep, azure.yaml, hooks) | 3 | 15 |
| Agent prompts and instructions | 10 | 10 |
| Meeting bridge (Node/TS and room pages) | 6 | 7 |
| Scripts and tools (SeedAdo) | 5 | 7 |
| Teams app package and manifest | 1 | 3 |
| Front end (admin portal) | 3 | 3 |
| Demo data and cast | 1 | 1 |
| **Total** | **556** | **1850** |

Distinct tokens found in the repo: `Mcm` (namespaces, projects, types), `MCM` (prose, UI text, agent prompts, ADO names, Teams manifest), `Midland Credit Management` (10 places in prose and prompts), lowercase `mcm` (slugs, Azure resource names, tags, temp folders). No `MCM_` constants or environment variables, no `Encore` and no `Midland` other than the company name were found. There are no configuration section names or user-secret keys that contain a token.

## 2. File and folder names that contain a token (planned new names)

| Current | New |
| --- | --- |
| `McmScrum.slnx` | `ZavaScrum.slnx` |
| `appPackage/mcm-scrum-agent.zip` | `appPackage/zava-scrum-agent.zip` |
| `docs/MCM-Scrum-Agent-PoC-Architecture.png` | `docs/Zava-Scrum-Agent-PoC-Architecture.png` |
| `docs/MCM-Scrum-Agent-PoC-Standup-Flow-As-Built.png` | `docs/Zava-Scrum-Agent-PoC-Standup-Flow-As-Built.png` |
| `src/Mcm.Scrum.Api` | `src/Zava.Scrum.Api` |
| `src/Mcm.Scrum.AppHost` | `src/Zava.Scrum.AppHost` |
| `src/Mcm.Scrum.Application` | `src/Zava.Scrum.Application` |
| `src/Mcm.Scrum.Domain` | `src/Zava.Scrum.Domain` |
| `src/Mcm.Scrum.Infrastructure` | `src/Zava.Scrum.Infrastructure` |
| `src/tests/Mcm.Scrum.Application.Tests` | `src/tests/Zava.Scrum.Application.Tests` |
| `src/tests/Mcm.Scrum.ArchitectureTests` | `src/tests/Zava.Scrum.ArchitectureTests` |
| `src/tests/Mcm.Scrum.Domain.Tests` | `src/tests/Zava.Scrum.Domain.Tests` |
| `src/tests/Mcm.Scrum.Infrastructure.Tests` | `src/tests/Zava.Scrum.Infrastructure.Tests` |

The 13 rows above are the top-most renamed path (a folder row covers everything below it). 530 tracked paths in total contain a token, almost all under `src/Mcm.Scrum.*`. Files inside renamed projects keep their own names unless they contain a token (`Mcm.Scrum.Api.csproj` becomes `Zava.Scrum.Api.csproj`).

## 3. Binary files with a token in the name or content

| File | Hits | Action |
| --- | ---: | --- |
| `appPackage/mcm-scrum-agent.zip` | 0 | Rebuild the zip from the updated manifest (Phase 3) |
| `docs/MCM-Scrum-Agent-PoC-Architecture.png` | 0 | Needs a re-render (diagram text shows the old name); not edited |
| `docs/MCM-Scrum-Agent-PoC-Standup-Flow-As-Built.png` | 1 | Needs a re-render (diagram text shows the old name); not edited |

No `.svg`, `.pdf`, `.docx`, `.pptx` or `.xlsx` files are tracked in the repo.

## 4. Things that live outside the repo

| Thing | Current value | Rule |
| --- | --- | --- |
| Azure resource group | `rg-mcm-scrum-demo-wus3` | Azure resources cannot be renamed in place. Not renamed; see `AZURE.md` |
| Foundry account / project | `fdy-mcm-scrum-demo` / `proj-mcm-scrum-demo-wus3` | Same. `fdy-mcm-scrum-demo` is also the default of `SPEECH_ENDPOINT` in the bridge (`src/meeting-bridge/src/config.ts`) |
| Log Analytics / Application Insights | `log-fdy-mcm-scrum-demo-dybvsjflknh74`, `appi-fdy-mcm-scrum-demo-9280` | Same. Hard-coded parameter defaults in `infra/main.bicep` |
| Bot, ACS, identity, registry, Cosmos, Container Apps environment | `bot-mcm-scrum-demo`, `acs-mcm-scrum-demo`, `id-mcm-scrum-<token>`, `acrmcmscrum<token>`, `cosmos-mcm-scrum-<token>`, `cae-mcm-scrum-<token>`; Cosmos database `mcmscrum` | Same. Hard-coded names in `infra/main.bicep` and `infra/hooks/postdeploy.ps1` |
| Teams app and bot ids | In `appPackage/manifest.json` and Entra | GUIDs are kept. Manifest `version` is bumped |
| Entra app registrations | `mcm-scrum-agent-bot`, `mcm-scrum-agent-calendar` | Display names live in Entra, not in the repo (the repo only mentions them in docs). Not renamed here |
| `UserSecretsId` | `mcm-scrum-apphost` (AppHost). The API project has its own id | Kept unchanged |
| Azure DevOps project | `MCM-Scrum-Demo` (org in user secrets) | Renamed by the seeder `--rebrand` mode only after James approves (Phase 5) |
| Azure DevOps work items | Titles, descriptions and tags in the existing project; tag `mcm-demo` | Same |
| azd environment | `.azure/<env>/.env` (not tracked) | Left unchanged |
| OneDrive docs | `...\Customers\Midland Credit Management\AI Scrum Master Agent\` | Naming only (Phase 7). The customer folder is not renamed |

## 5. Real customer people, emails and phone numbers in the repo (listed only, not changed)

| File | Mentions of Rajeev, Tyler, Nate or [RM] |
| --- | ---: |
| `docs/CEREMONIES.md` | 12 |
| `docs/REQUIREMENTS-AUDIT.md` | 28 |
| `docs/SPEC.md` | 1 |
| `src/Mcm.Scrum.Domain/Calendars/CalendarDurations.cs` | 1 |
| `src/Mcm.Scrum.Domain/Planning/PlanningSource.cs` | 1 |
| `src/Mcm.Scrum.Domain/Standups/RetroFormat.cs` | 1 |
| `src/Mcm.Scrum.Infrastructure/DependencyInjection.cs` | 1 |

- **Names found:** Rajeev (the customer sponsor), Tyler and Nate (named in `docs/REQUIREMENTS-AUDIT.md`). No `Monique`.
- **Source code comments** that name Rajeev: `src/Mcm.Scrum.Domain/Calendars/CalendarDurations.cs`, `Planning/PlanningSource.cs`, `Standups/RetroFormat.cs`, `src/Mcm.Scrum.Infrastructure/DependencyInjection.cs`.
- **Customer email addresses (`@mcmcg.com` or other):** none. Email addresses in the repo are test fixtures (`@x.com`).
- **Phone numbers:** none.
- The repo is public. These are **not changed**; James decides (see the report).

## 6. OneDrive docs folder (`AI Scrum Master Agent`)

File types: .css 1, .docx 3, .html 29, .json 3, .md 47, .pdf 6, .png 76, .ps1 1, .py 8, .svg 74. 248 files.

Text files with hits: 134 files, 2357 hits. Binary Office/PDF files (not edited): 9.

### 6.1 Text files with hits

| File | Hits |
| --- | ---: |
| `MCM-Scrum-Agent-PoC-Spec.html` | 70 |
| `MCM-Scrum-Agent-PoC-Spec.md` | 38 |
| `MCM-Scrum-Agent-Production-Spec.html` | 70 |
| `MCM-Scrum-Agent-Production-Spec.md` | 31 |
| `MCM-Scrum-Agent-Solution-Architecture.html` | 66 |
| `MCM-Scrum-Agent-Solution-Architecture.md` | 34 |
| `PoC As-Built\MCM-Scrum-Agent-Ceremony-Catalogue.html` | 13 |
| `PoC As-Built\MCM-Scrum-Agent-Ceremony-Catalogue.md` | 7 |
| `PoC As-Built\MCM-Scrum-Agent-Documentation-Review.html` | 69 |
| `PoC As-Built\MCM-Scrum-Agent-Documentation-Review.md` | 67 |
| `PoC As-Built\MCM-Scrum-Agent-PoC-As-Built.html` | 9 |
| `PoC As-Built\MCM-Scrum-Agent-PoC-As-Built.md` | 3 |
| `PoC As-Built\MCM-Scrum-Agent-PoC-Demo-Runbook.md` | 6 |
| `PoC As-Built\MCM-Scrum-Agent-PoC-Spec.html` | 81 |
| `PoC As-Built\MCM-Scrum-Agent-PoC-Spec.md` | 47 |
| `PoC As-Built\MCM-Scrum-Agent-Requirements-Audit.html` | 11 |
| `PoC As-Built\MCM-Scrum-Agent-Requirements-Audit.md` | 5 |
| `PoC As-Built\Original-copies\MCM-Scrum-Agent-Documentation-Review-2026-11-22-1111.md` | 4 |
| `PoC As-Built\Original-copies\MCM-Scrum-Agent-PoC-Spec.html` | 70 |
| `PoC As-Built\Original-copies\MCM-Scrum-Agent-PoC-Spec.md` | 38 |
| `PoC As-Built\Original-copies\copilot-instructions.md` | 12 |
| `PoC As-Built\Production-docs\MCM-Scrum-Agent-Production-Spec.html` | 76 |
| `PoC As-Built\Production-docs\MCM-Scrum-Agent-Production-Spec.md` | 37 |
| `PoC As-Built\Production-docs\MCM-Scrum-Agent-Solution-Architecture.html` | 66 |
| `PoC As-Built\Production-docs\MCM-Scrum-Agent-Solution-Architecture.md` | 33 |
| `PoC As-Built\Production-docs\Original-copies\MCM-Scrum-Agent-Production-Spec.html` | 70 |
| `PoC As-Built\Production-docs\Original-copies\MCM-Scrum-Agent-Production-Spec.md` | 31 |
| `PoC As-Built\Production-docs\Original-copies\MCM-Scrum-Agent-Solution-Architecture.html` | 66 |
| `PoC As-Built\Production-docs\Original-copies\MCM-Scrum-Agent-Solution-Architecture.md` | 34 |
| `PoC As-Built\README.md` | 9 |
| `PoC As-Built\_build\build-html.py` | 6 |
| `PoC As-Built\_build\template.html` | 1 |
| `PoC As-Built\adr\0001-domain-events-in-domain.md` | 1 |
| `PoC As-Built\adr\0005-m3b-m7-deviations.md` | 2 |
| `PoC As-Built\adr\0009b-agent-framework-and-opentelemetry.md` | 4 |
| `PoC As-Built\adr\0010-ceremony-kinds-packs-and-carry-over.md` | 1 |
| `PoC As-Built\sync-from-repo.ps1` | 11 |
| `Presenter Pack\BINDINGS.md` | 20 |
| `Presenter Pack\MCM-Scrum-Agent-Talk-Track-Reader.html` | 126 |
| `Presenter Pack\PREFLIGHT.md` | 3 |
| `Presenter Pack\README.md` | 16 |
| `Presenter Pack\RUN-SHEET.md` | 2 |
| `Presenter Pack\TALK-TRACK-v1.md` | 5 |
| `Presenter Pack\TALK-TRACK-v2.md` | 5 |
| `Presenter Pack\TALK-TRACK.md` | 12 |
| `Presenter Pack\VOICE.md` | 2 |
| `Presenter Pack\build\build_reader.py` | 42 |
| `Presenter Pack\build\check_links.py` | 1 |
| `Presenter Pack\build\diagrams\build_flows.py` | 7 |
| `Presenter Pack\build\diagrams\flows_f1_f2.py` | 7 |
| `Presenter Pack\build\diagrams\flows_f3_f5.py` | 3 |
| `Presenter Pack\build\diagrams\flows_f6_f8.py` | 3 |
| `Presenter Pack\build\diagrams\poc-model.json` | 30 |
| `Presenter Pack\build\diagrams\prod-model.json` | 81 |
| `Presenter Pack\build\previous-BINDINGS-2026-11-22.md` | 9 |
| `Presenter Pack\build\previous-PREFLIGHT-2026-11-22.md` | 1 |
| `Presenter Pack\build\previous-README-2026-11-22.md` | 7 |
| `Presenter Pack\build\previous-RUN-SHEET-2026-11-22.md` | 1 |
| `Presenter Pack\build\previous-reader-2026-11-22.html` | 11 |
| `Presenter Pack\build\windows.json` | 3 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Architecture-and-Workflows.html` | 45 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-1-Daily-Scrum.svg` | 1 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.svg` | 2 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-3-Sprint-Planning.svg` | 1 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-4-Retrospective.svg` | 1 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-5-Sprint-Review.svg` | 1 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-6-KPIs-Reports.svg` | 1 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-7-Scheduling.svg` | 1 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-8-Risk-Escalation.svg` | 1 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Flows.html` | 19 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-Architecture.html` | 31 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C1-Context.svg` | 4 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C2-Containers.svg` | 7 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C3-Components.svg` | 2 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C1-Context.svg` | 3 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C2-Containers.svg` | 2 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C3-Components.svg` | 1 |
| `Presenter Pack\diagrams\MCM-Scrum-Agent-Production-Architecture.html` | 82 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Architecture-and-Workflows.html` | 45 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-1-Daily-Scrum.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-3-Sprint-Planning.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-4-Retrospective.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-5-Sprint-Review.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-6-KPIs-Reports.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-7-Scheduling.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-8-Risk-Escalation.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flows.html` | 22 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-Architecture.html` | 32 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C1-Context.svg` | 4 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C2-Containers.svg` | 7 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C3-Components.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C1-Context.svg` | 3 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C2-Containers.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C3-Components.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Production-Architecture.html` | 80 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Architecture-and-Workflows.html` | 45 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-1-Daily-Scrum.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-3-Sprint-Planning.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-4-Retrospective.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-5-Sprint-Review.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-6-KPIs-Reports.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-7-Scheduling.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-8-Risk-Escalation.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flows.html` | 22 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-Architecture.html` | 31 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C1-Context.svg` | 4 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C2-Containers.svg` | 7 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C3-Components.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C1-Context.svg` | 3 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C2-Containers.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C3-Components.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Production-Architecture.html` | 80 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Architecture-and-Workflows.html` | 45 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-1-Daily-Scrum.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-3-Sprint-Planning.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-4-Retrospective.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-5-Sprint-Review.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-6-KPIs-Reports.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-7-Scheduling.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-8-Risk-Escalation.svg` | 1 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C1-Context.svg` | 4 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C2-Containers.svg` | 7 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C3-Components.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C1-Context.svg` | 3 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C2-Containers.svg` | 2 |
| `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C3-Components.svg` | 1 |
| `Presenter Pack\prompts\ghcp-docs-sync.md` | 1 |
| `Presenter Pack\prompts\ghcp-m9-ceremony-demos.md` | 3 |
| `Presenter Pack\prompts\ghcp-rebrand-to-zava.md` | 43 |
| `README.md` | 8 |
| `copilot-instructions.md` | 15 |

### 6.2 File and folder names with a token

- `MCM AI Scrum Master Agent - Production Specification.pdf` -> `Zava AI Scrum Master Agent - Production Specification.pdf`
- `MCM AI Scrum Master Agent - Solution Architecture.docx` -> `Zava AI Scrum Master Agent - Solution Architecture.docx`
- `MCM AI Scrum Master Agent - Solution Architecture.pdf` -> `Zava AI Scrum Master Agent - Solution Architecture.pdf`
- `MCM-Scrum-Agent-Deployment-Topology.png` -> `Zava-Scrum-Agent-Deployment-Topology.png`
- `MCM-Scrum-Agent-Flow-Ask.png` -> `Zava-Scrum-Agent-Flow-Ask.png`
- `MCM-Scrum-Agent-Flow-FollowUp.png` -> `Zava-Scrum-Agent-Flow-FollowUp.png`
- `MCM-Scrum-Agent-Flow-Sprint.png` -> `Zava-Scrum-Agent-Flow-Sprint.png`
- `MCM-Scrum-Agent-PoC-Architecture.png` -> `Zava-Scrum-Agent-PoC-Architecture.png`
- `MCM-Scrum-Agent-PoC-Spec.html` -> `Zava-Scrum-Agent-PoC-Spec.html`
- `MCM-Scrum-Agent-PoC-Spec.md` -> `Zava-Scrum-Agent-PoC-Spec.md`
- `MCM-Scrum-Agent-Production-Spec.html` -> `Zava-Scrum-Agent-Production-Spec.html`
- `MCM-Scrum-Agent-Production-Spec.md` -> `Zava-Scrum-Agent-Production-Spec.md`
- `MCM-Scrum-Agent-Reference-Architecture.png` -> `Zava-Scrum-Agent-Reference-Architecture.png`
- `MCM-Scrum-Agent-Solution-Architecture.html` -> `Zava-Scrum-Agent-Solution-Architecture.html`
- `MCM-Scrum-Agent-Solution-Architecture.md` -> `Zava-Scrum-Agent-Solution-Architecture.md`
- `MCM-Scrum-Agent-Standup-Flow.png` -> `Zava-Scrum-Agent-Standup-Flow.png`
- `PoC As-Built\MCM-Scrum-Agent-Ceremony-Catalogue.html` -> `PoC As-Built\Zava-Scrum-Agent-Ceremony-Catalogue.html`
- `PoC As-Built\MCM-Scrum-Agent-Ceremony-Catalogue.md` -> `PoC As-Built\Zava-Scrum-Agent-Ceremony-Catalogue.md`
- `PoC As-Built\MCM-Scrum-Agent-Documentation-Review.html` -> `PoC As-Built\Zava-Scrum-Agent-Documentation-Review.html`
- `PoC As-Built\MCM-Scrum-Agent-Documentation-Review.md` -> `PoC As-Built\Zava-Scrum-Agent-Documentation-Review.md`
- `PoC As-Built\MCM-Scrum-Agent-PoC-Architecture.png` -> `PoC As-Built\Zava-Scrum-Agent-PoC-Architecture.png`
- `PoC As-Built\MCM-Scrum-Agent-PoC-As-Built.html` -> `PoC As-Built\Zava-Scrum-Agent-PoC-As-Built.html`
- `PoC As-Built\MCM-Scrum-Agent-PoC-As-Built.md` -> `PoC As-Built\Zava-Scrum-Agent-PoC-As-Built.md`
- `PoC As-Built\MCM-Scrum-Agent-PoC-Demo-Runbook.md` -> `PoC As-Built\Zava-Scrum-Agent-PoC-Demo-Runbook.md`
- `PoC As-Built\MCM-Scrum-Agent-PoC-Spec.html` -> `PoC As-Built\Zava-Scrum-Agent-PoC-Spec.html`
- `PoC As-Built\MCM-Scrum-Agent-PoC-Spec.md` -> `PoC As-Built\Zava-Scrum-Agent-PoC-Spec.md`
- `PoC As-Built\MCM-Scrum-Agent-PoC-Standup-Flow-As-Built.png` -> `PoC As-Built\Zava-Scrum-Agent-PoC-Standup-Flow-As-Built.png`
- `PoC As-Built\MCM-Scrum-Agent-Requirements-Audit.html` -> `PoC As-Built\Zava-Scrum-Agent-Requirements-Audit.html`
- `PoC As-Built\MCM-Scrum-Agent-Requirements-Audit.md` -> `PoC As-Built\Zava-Scrum-Agent-Requirements-Audit.md`
- `Presenter Pack\MCM-Scrum-Agent-Talk-Track-Reader.html` -> `Presenter Pack\Zava-Scrum-Agent-Talk-Track-Reader.html`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Architecture-and-Workflows.html` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Architecture-and-Workflows.html`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-1-Daily-Scrum.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-1-Daily-Scrum.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-1-Daily-Scrum.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-1-Daily-Scrum.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-2-FollowUp-Approval.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-2-FollowUp-Approval.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-3-Sprint-Planning.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-3-Sprint-Planning.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-3-Sprint-Planning.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-3-Sprint-Planning.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-4-Retrospective.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-4-Retrospective.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-4-Retrospective.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-4-Retrospective.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-5-Sprint-Review.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-5-Sprint-Review.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-5-Sprint-Review.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-5-Sprint-Review.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-6-KPIs-Reports.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-6-KPIs-Reports.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-6-KPIs-Reports.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-6-KPIs-Reports.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-7-Scheduling.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-7-Scheduling.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-7-Scheduling.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-7-Scheduling.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-8-Risk-Escalation.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-8-Risk-Escalation.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flow-8-Risk-Escalation.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flow-8-Risk-Escalation.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Flows.html` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Flows.html`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-Architecture.html` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-PoC-Architecture.html`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C1-Context.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-PoC-C1-Context.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C1-Context.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-PoC-C1-Context.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C2-Containers.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-PoC-C2-Containers.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C2-Containers.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-PoC-C2-Containers.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C3-Components.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-PoC-C3-Components.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-PoC-C3-Components.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-PoC-C3-Components.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C1-Context.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Prod-C1-Context.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C1-Context.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Prod-C1-Context.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C2-Containers.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Prod-C2-Containers.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C2-Containers.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Prod-C2-Containers.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C3-Components.png` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Prod-C3-Components.png`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Prod-C3-Components.svg` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Prod-C3-Components.svg`
- `Presenter Pack\diagrams\MCM-Scrum-Agent-Production-Architecture.html` -> `Presenter Pack\diagrams\Zava-Scrum-Agent-Production-Architecture.html`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Architecture-and-Workflows.html` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Architecture-and-Workflows.html`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-1-Daily-Scrum.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-1-Daily-Scrum.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-1-Daily-Scrum.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-1-Daily-Scrum.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-2-FollowUp-Approval.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-2-FollowUp-Approval.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-3-Sprint-Planning.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-3-Sprint-Planning.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-3-Sprint-Planning.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-3-Sprint-Planning.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-4-Retrospective.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-4-Retrospective.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-4-Retrospective.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-4-Retrospective.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-5-Sprint-Review.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-5-Sprint-Review.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-5-Sprint-Review.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-5-Sprint-Review.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-6-KPIs-Reports.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-6-KPIs-Reports.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-6-KPIs-Reports.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-6-KPIs-Reports.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-7-Scheduling.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-7-Scheduling.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-7-Scheduling.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-7-Scheduling.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-8-Risk-Escalation.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-8-Risk-Escalation.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Flow-8-Risk-Escalation.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Flow-8-Risk-Escalation.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C1-Context.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-PoC-C1-Context.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C1-Context.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-PoC-C1-Context.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C2-Containers.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-PoC-C2-Containers.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C2-Containers.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-PoC-C2-Containers.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C3-Components.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-PoC-C3-Components.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-PoC-C3-Components.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-PoC-C3-Components.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C1-Context.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Prod-C1-Context.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C1-Context.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Prod-C1-Context.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C2-Containers.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Prod-C2-Containers.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C2-Containers.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Prod-C2-Containers.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C3-Components.png` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Prod-C3-Components.png`
- `Presenter Pack\diagrams\_previous-2026-11-22\MCM-Scrum-Agent-Prod-C3-Components.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22\Zava-Scrum-Agent-Prod-C3-Components.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Architecture-and-Workflows.html` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Architecture-and-Workflows.html`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-1-Daily-Scrum.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-1-Daily-Scrum.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-1-Daily-Scrum.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-1-Daily-Scrum.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-2-FollowUp-Approval.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-2-FollowUp-Approval.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-3-Sprint-Planning.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-3-Sprint-Planning.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-3-Sprint-Planning.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-3-Sprint-Planning.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-4-Retrospective.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-4-Retrospective.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-4-Retrospective.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-4-Retrospective.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-5-Sprint-Review.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-5-Sprint-Review.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-5-Sprint-Review.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-5-Sprint-Review.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-6-KPIs-Reports.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-6-KPIs-Reports.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-6-KPIs-Reports.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-6-KPIs-Reports.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-7-Scheduling.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-7-Scheduling.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-7-Scheduling.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-7-Scheduling.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-8-Risk-Escalation.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-8-Risk-Escalation.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flow-8-Risk-Escalation.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flow-8-Risk-Escalation.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Flows.html` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Flows.html`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-Architecture.html` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-PoC-Architecture.html`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C1-Context.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-PoC-C1-Context.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C1-Context.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-PoC-C1-Context.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C2-Containers.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-PoC-C2-Containers.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C2-Containers.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-PoC-C2-Containers.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C3-Components.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-PoC-C3-Components.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-PoC-C3-Components.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-PoC-C3-Components.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C1-Context.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Prod-C1-Context.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C1-Context.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Prod-C1-Context.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C2-Containers.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Prod-C2-Containers.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C2-Containers.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Prod-C2-Containers.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C3-Components.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Prod-C3-Components.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Prod-C3-Components.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Prod-C3-Components.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-b\MCM-Scrum-Agent-Production-Architecture.html` -> `Presenter Pack\diagrams\_previous-2026-11-22-b\Zava-Scrum-Agent-Production-Architecture.html`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Architecture-and-Workflows.html` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Architecture-and-Workflows.html`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-1-Daily-Scrum.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-1-Daily-Scrum.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-1-Daily-Scrum.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-1-Daily-Scrum.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-2-FollowUp-Approval.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-2-FollowUp-Approval.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-2-FollowUp-Approval.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-3-Sprint-Planning.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-3-Sprint-Planning.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-3-Sprint-Planning.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-3-Sprint-Planning.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-4-Retrospective.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-4-Retrospective.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-4-Retrospective.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-4-Retrospective.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-5-Sprint-Review.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-5-Sprint-Review.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-5-Sprint-Review.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-5-Sprint-Review.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-6-KPIs-Reports.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-6-KPIs-Reports.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-6-KPIs-Reports.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-6-KPIs-Reports.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-7-Scheduling.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-7-Scheduling.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-7-Scheduling.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-7-Scheduling.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-8-Risk-Escalation.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-8-Risk-Escalation.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flow-8-Risk-Escalation.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flow-8-Risk-Escalation.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Flows.html` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Flows.html`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-Architecture.html` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-PoC-Architecture.html`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C1-Context.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-PoC-C1-Context.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C1-Context.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-PoC-C1-Context.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C2-Containers.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-PoC-C2-Containers.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C2-Containers.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-PoC-C2-Containers.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C3-Components.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-PoC-C3-Components.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-PoC-C3-Components.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-PoC-C3-Components.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C1-Context.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Prod-C1-Context.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C1-Context.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Prod-C1-Context.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C2-Containers.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Prod-C2-Containers.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C2-Containers.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Prod-C2-Containers.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C3-Components.png` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Prod-C3-Components.png`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Prod-C3-Components.svg` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Prod-C3-Components.svg`
- `Presenter Pack\diagrams\_previous-2026-11-22-c\MCM-Scrum-Agent-Production-Architecture.html` -> `Presenter Pack\diagrams\_previous-2026-11-22-c\Zava-Scrum-Agent-Production-Architecture.html`
- `PoC As-Built\Original-copies\MCM-Scrum-Agent-Documentation-Review-2026-11-22-1111.md` -> `PoC As-Built\Original-copies\Zava-Scrum-Agent-Documentation-Review-2026-11-22-1111.md`
- `PoC As-Built\Original-copies\MCM-Scrum-Agent-Flow-Ask.png` -> `PoC As-Built\Original-copies\Zava-Scrum-Agent-Flow-Ask.png`
- `PoC As-Built\Original-copies\MCM-Scrum-Agent-Flow-FollowUp.png` -> `PoC As-Built\Original-copies\Zava-Scrum-Agent-Flow-FollowUp.png`
- `PoC As-Built\Original-copies\MCM-Scrum-Agent-Flow-Sprint.png` -> `PoC As-Built\Original-copies\Zava-Scrum-Agent-Flow-Sprint.png`
- `PoC As-Built\Original-copies\MCM-Scrum-Agent-PoC-Architecture.png` -> `PoC As-Built\Original-copies\Zava-Scrum-Agent-PoC-Architecture.png`
- `PoC As-Built\Original-copies\MCM-Scrum-Agent-PoC-Spec.html` -> `PoC As-Built\Original-copies\Zava-Scrum-Agent-PoC-Spec.html`
- `PoC As-Built\Original-copies\MCM-Scrum-Agent-PoC-Spec.md` -> `PoC As-Built\Original-copies\Zava-Scrum-Agent-PoC-Spec.md`
- `PoC As-Built\Original-copies\MCM-Scrum-Agent-Standup-Flow.png` -> `PoC As-Built\Original-copies\Zava-Scrum-Agent-Standup-Flow.png`
- `PoC As-Built\Production-docs\MCM AI Scrum Master Agent - Production Specification.pdf` -> `PoC As-Built\Production-docs\Zava AI Scrum Master Agent - Production Specification.pdf`
- `PoC As-Built\Production-docs\MCM AI Scrum Master Agent - Solution Architecture.docx` -> `PoC As-Built\Production-docs\Zava AI Scrum Master Agent - Solution Architecture.docx`
- `PoC As-Built\Production-docs\MCM AI Scrum Master Agent - Solution Architecture.pdf` -> `PoC As-Built\Production-docs\Zava AI Scrum Master Agent - Solution Architecture.pdf`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Deployment-Topology.png` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Deployment-Topology.png`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Flow-Ask.png` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Flow-Ask.png`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Flow-FollowUp.png` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Flow-FollowUp.png`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Flow-Sprint.png` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Flow-Sprint.png`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Production-Spec.html` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Production-Spec.html`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Production-Spec.md` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Production-Spec.md`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Reference-Architecture.png` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Reference-Architecture.png`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Solution-Architecture.html` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Solution-Architecture.html`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Solution-Architecture.md` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Solution-Architecture.md`
- `PoC As-Built\Production-docs\MCM-Scrum-Agent-Standup-Flow.png` -> `PoC As-Built\Production-docs\Zava-Scrum-Agent-Standup-Flow.png`
- `PoC As-Built\Production-docs\Original-copies\MCM AI Scrum Master Agent - Production Specification.pdf` -> `PoC As-Built\Production-docs\Original-copies\Zava AI Scrum Master Agent - Production Specification.pdf`
- `PoC As-Built\Production-docs\Original-copies\MCM AI Scrum Master Agent - Solution Architecture.docx` -> `PoC As-Built\Production-docs\Original-copies\Zava AI Scrum Master Agent - Solution Architecture.docx`
- `PoC As-Built\Production-docs\Original-copies\MCM AI Scrum Master Agent - Solution Architecture.pdf` -> `PoC As-Built\Production-docs\Original-copies\Zava AI Scrum Master Agent - Solution Architecture.pdf`
- `PoC As-Built\Production-docs\Original-copies\MCM-Scrum-Agent-Production-Spec.html` -> `PoC As-Built\Production-docs\Original-copies\Zava-Scrum-Agent-Production-Spec.html`
- `PoC As-Built\Production-docs\Original-copies\MCM-Scrum-Agent-Production-Spec.md` -> `PoC As-Built\Production-docs\Original-copies\Zava-Scrum-Agent-Production-Spec.md`
- `PoC As-Built\Production-docs\Original-copies\MCM-Scrum-Agent-Solution-Architecture.html` -> `PoC As-Built\Production-docs\Original-copies\Zava-Scrum-Agent-Solution-Architecture.html`
- `PoC As-Built\Production-docs\Original-copies\MCM-Scrum-Agent-Solution-Architecture.md` -> `PoC As-Built\Production-docs\Original-copies\Zava-Scrum-Agent-Solution-Architecture.md`

### 6.3 Office and PDF files (not edited; James regenerates)

- `MCM AI Scrum Master Agent - Production Specification.pdf`
- `MCM AI Scrum Master Agent - Solution Architecture.docx`
- `MCM AI Scrum Master Agent - Solution Architecture.pdf`
- `PoC As-Built\Production-docs\MCM AI Scrum Master Agent - Production Specification.pdf`
- `PoC As-Built\Production-docs\MCM AI Scrum Master Agent - Solution Architecture.docx`
- `PoC As-Built\Production-docs\MCM AI Scrum Master Agent - Solution Architecture.pdf`
- `PoC As-Built\Production-docs\Original-copies\MCM AI Scrum Master Agent - Production Specification.pdf`
- `PoC As-Built\Production-docs\Original-copies\MCM AI Scrum Master Agent - Solution Architecture.docx`
- `PoC As-Built\Production-docs\Original-copies\MCM AI Scrum Master Agent - Solution Architecture.pdf`
