using System.Text.Json;
using Mcm.Scrum.Domain.Kpis;

namespace Mcm.Scrum.Domain.Tests.Kpis;

public sealed class KpiCalculatorTests
{
    private static SprintHistory Sprint(string name, int day, double committed, double completed, int carry = 0) =>
        new(name, new DateOnly(2026, 1, day), new DateOnly(2026, 1, day + 1), committed, completed, carry, 2, 3, 90, 80, 1, 12);

    [Fact]
    public void Summarize_ThreeSprints_MatchesHandCalculation()
    {
        var window = KpiCalculator.Window([Sprint("a", 1, 40, 32, 3), Sprint("b", 3, 42, 35, 2), Sprint("c", 5, 38, 34, 2)], 3);

        var kpi = KpiCalculator.Summarize(window);

        Assert.Equal(101d / 120d, kpi.SayDoRatio, 6);
        Assert.Equal(101d / 3d, kpi.Velocity, 6);
        Assert.Equal(7, kpi.CarryOverItems);
        Assert.Equal(3, kpi.AgentRunStandups);
    }

    [Fact]
    public void Window_MoreThanSixRequested_UsesTheLatestSixOldestFirst()
    {
        var all = Enumerable.Range(1, 8).Select(i => Sprint($"s{i}", i * 2, 10, i)).ToList();

        var window = KpiCalculator.Window(all, 20);

        Assert.Equal(["s3", "s4", "s5", "s6", "s7", "s8"], window.Select(s => s.Name));
    }

    [Fact]
    public void Window_FewerThanThreeRequested_StillUsesThree()
    {
        var all = Enumerable.Range(1, 5).Select(i => Sprint($"s{i}", i * 2, 10, i)).ToList();

        Assert.Equal(3, KpiCalculator.Window(all, 1).Count);
    }

    [Fact]
    public void Summarize_NothingCommitted_HasZeroRatioNotNaN() =>
        Assert.Equal(0, KpiCalculator.Summarize([Sprint("a", 1, 0, 0)]).SayDoRatio);

    [Fact]
    public void Summarize_Fixture_SixSprintsHaveKnownVelocityAndRatio()
    {
        var path = FindFixture();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var sprints = doc.RootElement.GetProperty("sprints").EnumerateArray()
            .Select(e => new SprintHistory(
                e.GetProperty("name").GetString()!,
                DateOnly.Parse(e.GetProperty("start").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                DateOnly.Parse(e.GetProperty("end").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                e.GetProperty("committedPoints").GetDouble(),
                e.GetProperty("completedPoints").GetDouble(),
                e.GetProperty("carryOverItems").GetInt32(),
                e.GetProperty("avgBlockerAgeDays").GetDouble(),
                e.GetProperty("avgCycleTimeDays").GetDouble(),
                e.GetProperty("standupAttendancePct").GetDouble(),
                e.GetProperty("followUpClosedWithin24hPct").GetDouble(),
                e.GetProperty("agentRunStandups").GetInt32(),
                e.GetProperty("avgStandupMinutes").GetDouble())).ToList();

        var kpi = KpiCalculator.Summarize(KpiCalculator.Window(sprints, 6));

        Assert.Equal(219d / 246d, kpi.SayDoRatio, 6);
        Assert.Equal(219d / 6d, kpi.Velocity, 6);
        Assert.Equal(9, kpi.CarryOverItems);
        Assert.Equal(13, kpi.AgentRunStandups);
    }

    private static string FindFixture()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "demo", "kpi-history.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("demo/kpi-history.json not found.");
    }
}
