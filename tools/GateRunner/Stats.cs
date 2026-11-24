namespace Zava.Scrum.Tools.GateRunner;

/// <summary>Percentiles over small samples.</summary>
internal static class Stats
{
    /// <summary>Nearest-rank percentile: the smallest value with at least <paramref name="p"/> percent of the sample at or below it. Never interpolates, so every reported number was actually observed.</summary>
    /// <param name="values">The sample.</param>
    /// <param name="p">The percentile, 0 to 100.</param>
    /// <returns>The percentile, or 0 for an empty sample.</returns>
    public static int Percentile(IReadOnlyCollection<int> values, double p)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.Order().ToArray();
        var rank = (int)Math.Ceiling(p / 100.0 * sorted.Length);
        return sorted[Math.Clamp(rank, 1, sorted.Length) - 1];
    }
}
