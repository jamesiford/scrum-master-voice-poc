namespace Zava.Scrum.Domain.Standups;

/// <summary>Latency maths for the demo metric: end of speech to first agent audio.</summary>
public static class LatencyStats
{
    /// <summary>Computes a percentile by the nearest-rank method.</summary>
    /// <param name="samplesMs">Samples in milliseconds, any order.</param>
    /// <param name="percentile">0 to 100.</param>
    /// <returns>The percentile, or 0 when there are no samples.</returns>
    public static int Percentile(IReadOnlyCollection<int> samplesMs, double percentile)
    {
        ArgumentNullException.ThrowIfNull(samplesMs);
        if (samplesMs.Count == 0)
        {
            return 0;
        }

        var sorted = samplesMs.Order().ToArray();
        var rank = (int)Math.Ceiling(percentile / 100d * sorted.Length);
        return sorted[Math.Clamp(rank, 1, sorted.Length) - 1];
    }
}
