namespace Zava.Scrum.Application.Features.Standups.GetSessionLatency;

/// <summary>Measured per-hop latency for a single session; absent hops are not estimated.</summary>
/// <param name="TeamId">Owning team.</param>
/// <param name="SessionId">Session.</param>
/// <param name="Hops">Only hops with actual timing samples.</param>
public sealed record SessionLatencyDto(string TeamId, Guid SessionId, IReadOnlyList<HopLatencyDto> Hops);
