using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Health.GetHealth;

/// <summary>Reads API health. Exists in M0 to prove the pipeline, handler, port and store end to end.</summary>
public sealed record GetHealthQuery : IQuery<HealthDto>;
