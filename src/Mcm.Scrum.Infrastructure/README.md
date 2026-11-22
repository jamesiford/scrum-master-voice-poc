# Mcm.Scrum.Infrastructure

Adapters that implement Application ports: persistence (`JsonFile*Repository` today, Cosmos later), the Azure DevOps REST client (`Ado/`), the Microsoft Agent Framework agents over Foundry (the four `Foundry*Agent` adapters, `FoundryChat` and `FoundryAgentFactory`) and the prompt store (`Agents/`), the demo roster source (`Demo/`), and the clock. `AddInfrastructure()` registers them.

- **May reference:** Application, Domain, SDK packages.
- **Tests:** `dotnet test src/tests/Mcm.Scrum.Infrastructure.Tests` (repository contract tests)
