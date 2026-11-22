# Mcm.Scrum.Application

Use cases (commands and queries with handlers and validators), ports, the hand-rolled mediator (`Abstractions/Messaging`) and pipeline behaviours (Logging, Validation, ExceptionMapping, in that order).

- **May reference:** Domain, FluentValidation, `Microsoft.Extensions.*.Abstractions`.
- **Tests:** `dotnet test src/tests/Mcm.Scrum.Application.Tests`
