using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Demo.GetDemoReseed;

/// <summary>Reads the scoped recoverable reset journal as a validated Admin.</summary>
public sealed record GetDemoReseedQuery(string TeamId, Guid OperationId, DecisionContext Decision) : IQuery<DemoReseedDto>;
