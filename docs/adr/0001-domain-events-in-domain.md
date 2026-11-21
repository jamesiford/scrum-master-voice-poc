# 0001. Domain events live in Domain; dispatch wiring deferred

## Context
SPEC 6.1.3 lists `IDomainEvent` next to the mediator contract in Application, but SPEC 6.1.1 says aggregates in Domain raise domain events and Domain may reference nothing.

## Decision
`IDomainEvent`, `AggregateRoot` and `Result`/`Error` live in `Mcm.Scrum.Domain/Common`. `INotificationHandler<T>` and `IPublisher` stay in Application. The choice between a `DomainEventDispatcher` called by handlers and a repository decorator (SPEC 6.1.3) is deferred to M3, the first milestone with a real domain event (`FollowUpApproved`); it will be recorded in a new ADR then.

## Consequences
The dependency rule holds with no workaround. M0 ships the mediator's publish path and tests, but no aggregate event is dispatched yet.
