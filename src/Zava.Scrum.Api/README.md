# Zava.Scrum.Api

Composition root and thin presentation adapters (minimal API endpoints per feature, later the bot handler, bridge WebSocket and scheduler). No business logic.

- **May reference:** Application, Infrastructure.
- **Run:** `dotnet run --project src/Zava.Scrum.AppHost --launch-profile http` starts the API, bridge and portal (see the Aspire dashboard at http://localhost:15888 for URLs).
- **Tests:** `dotnet test src/tests/Zava.Scrum.ArchitectureTests` enforces the dependency rule.
