using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Health.GetHealth;

/// <summary>Reads API health. Exists in M0 to prove the pipeline, handler, port and store end to end.</summary>
public sealed record GetHealthQuery : IQuery<HealthDto>;
