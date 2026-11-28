# Zava.Scrum.Infrastructure

Adapters that implement Application ports: file/Cosmos persistence, the Azure DevOps REST client (`Ado/`), the Microsoft Agent Framework agents over Foundry (the four `Foundry*Agent` adapters, `FoundryChat` and `FoundryAgentFactory`) and the prompt store (`Agents/`), the demo roster source (`Demo/`), and the clock. `AddInfrastructure()` registers them.

`TeamRecords` stores cards, follow-ups and bot deliveries per item, partitioned
by team (ADR0040). Both DI modes use real item adapters; startup migration
preserves authoritative legacy overrides. Focused checks:
`dotnet test src\tests\Zava.Scrum.Infrastructure.Tests --filter "FullyQualifiedName~TeamRecordStorageTests|FullyQualifiedName~DemoReseedPersistenceTests"`.
`tools\StorageProbe` shares internal renderer/envelope contracts for real
Cosmos export/load measurements; it cannot change public APIs or approval rules.

- **May reference:** Application, Domain, SDK packages.
- **Tests:** `dotnet test src/tests/Zava.Scrum.Infrastructure.Tests` (repository contract tests)

`WorkSignals/` contains real owner-OBO and metadata-only MCP discovery
adapters, deliberately unregistered until billing and scoped real-user
fixtures pass (ADR 0034). Discovery cannot call tools or read content.
Focused checks: `dotnet test src\tests\Zava.Scrum.Infrastructure.Tests --filter FullyQualifiedName~WorkIqDiscovery`.
