# MCM Scrum Master voice PoC

Voice-led junior Scrum Master demo. Build spec: [docs/SPEC.md](docs/SPEC.md). What was actually built: [docs/AS-BUILT.md](docs/AS-BUILT.md). Demo runbook: [docs/DEMO.md](docs/DEMO.md). Decisions: [docs/adr](docs/adr). Engineering rules: [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Run locally

```
az login --tenant <tenant-id>
dotnet run --project tools/SeedAdo -- --tenant <tenant-id>   # M1: seed the Azure DevOps sandbox (safe to repeat)
npm install --prefix src/meeting-bridge
npm install --prefix src/admin-portal
dotnet run --project src/Mcm.Scrum.AppHost --launch-profile http
```

The Aspire dashboard (http://localhost:15888) lists the portal, bridge and API URLs. In the portal: Roster > Sync roster, then Sessions > Prepare now to see the pre-brief, then **Start in Simulated Room** to run the standup (tick Autopilot in the room to let the cast speak your lines too). The bridge is on http://localhost:5090 and the API on http://localhost:5080. Azure settings (organisation, Foundry endpoint, tenant) are in `src/Mcm.Scrum.Api/appsettings.json`; no secrets are needed because everything uses `DefaultAzureCredential`.

## Checks

```
dotnet build McmScrum.slnx
dotnet test McmScrum.slnx
dotnet format McmScrum.slnx --verify-no-changes
npm run lint --prefix src/meeting-bridge
npm run lint --prefix src/admin-portal
```

Standup end to end (needs the stack running): `npm run e2e --prefix src/meeting-bridge`

