namespace Zava.Scrum.Application.Features.Standups.GetSessionLatency;

/// <summary>Nearest-rank percentiles and sample size for one measured interval.</summary>
/// <param name="Hop">Fixed operation name.</param>
/// <param name="P50Ms">Median milliseconds.</param>
/// <param name="P95Ms">95th percentile milliseconds.</param>
/// <param name="N">Actual number of samples.</param>
public sealed record HopLatencyDto(string Hop, int P50Ms, int P95Ms, int N);
