using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Demo.AbortDemoReseed;

/// <summary>Explicitly releases a reset that has not been approved; never rolls back distributed ADO work.</summary>
public sealed record AbortDemoReseedCommand(string TeamId, Guid OperationId, DecisionContext Decision) : ICommand<Guid>;
