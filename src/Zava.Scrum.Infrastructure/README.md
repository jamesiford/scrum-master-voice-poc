# Zava.Scrum.Infrastructure

Adapters that implement Application ports: persistence (`JsonFile*Repository` today, Cosmos later), the Azure DevOps REST client (`Ado/`), the Microsoft Agent Framework agents over Foundry (the four `Foundry*Agent` adapters, `FoundryChat` and `FoundryAgentFactory`) and the prompt store (`Agents/`), the demo roster source (`Demo/`), and the clock. `AddInfrastructure()` registers them.

- **May reference:** Application, Domain, SDK packages.
- **Tests:** `dotnet test src/tests/Zava.Scrum.Infrastructure.Tests` (repository contract tests)

`WorkSignals/` contains real owner-OBO and metadata-only MCP discovery
adapters, deliberately unregistered until billing and scoped real-user
fixtures pass (ADR 0034). Discovery cannot call tools or read content.
Focused checks: `dotnet test src\tests\Zava.Scrum.Infrastructure.Tests --filter FullyQualifiedName~WorkIqDiscovery`.
