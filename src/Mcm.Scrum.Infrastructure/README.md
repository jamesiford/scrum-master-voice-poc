# Mcm.Scrum.Infrastructure

Adapters that implement Application ports: persistence (`JsonFile*Repository` today, Cosmos later), the Azure DevOps REST client (`Ado/`), the Foundry-backed Backlog and Risk agent and prompt store (`Agents/`), the demo roster source (`Demo/`), and the clock. `AddInfrastructure()` registers them.

- **May reference:** Application, Domain, SDK packages.
- **Tests:** `dotnet test src/tests/Mcm.Scrum.Infrastructure.Tests` (repository contract tests)
