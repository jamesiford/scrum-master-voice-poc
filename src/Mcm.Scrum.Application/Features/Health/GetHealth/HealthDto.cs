namespace Mcm.Scrum.Application.Features.Health.GetHealth;

/// <summary>Health read model shown as "API healthy" in the portal.</summary>
/// <param name="Status">Always <c>healthy</c> on success.</param>
/// <param name="Time">Server time.</param>
/// <param name="SessionCount">Sessions in the store; proves the store is reachable.</param>
public sealed record HealthDto(string Status, DateTimeOffset Time, int SessionCount);
