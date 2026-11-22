using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Kpis;

public sealed class BurndownCalculatorTests
{
    private static SprintSnapshot Sprint(params WorkItem[] items) =>
        new("P\\S1", TestData.Now.AddDays(-4), TestData.Now.AddDays(6), items);

    [Fact]
    public void Calculate_MixedItems_SumsCommittedCompletedRemaining()
    {
        var sprint = Sprint(
            TestData.Item(1, state: "Closed", points: 8),
            TestData.Item(2, state: "Active", points: 5),
            TestData.Item(3, state: "New", points: 3),
            TestData.Item(4, type: "Task"));

        var result = BurndownCalculator.Calculate(sprint, TestData.Now);

        Assert.Equal((16d, 8d, 8d, 6), (result.CommittedPoints, result.CompletedPoints, result.RemainingPoints, result.DaysLeft));
    }

    [Fact]
    public void Calculate_RequiredBurnAbove130PercentOfPace_IsAtRisk()
    {
        // Pace 2/day over 4 days; needs 26/6 = 4.33/day which is above 2.6.
        var sprint = Sprint(TestData.Item(1, state: "Closed", points: 8), TestData.Item(2, points: 26));

        Assert.True(BurndownCalculator.Calculate(sprint, TestData.Now).AtRisk);
    }

    [Fact]
    public void Calculate_OnPace_IsNotAtRisk()
    {
        // Pace 4/day; needs 12/6 = 2/day.
        var sprint = Sprint(TestData.Item(1, state: "Closed", points: 16), TestData.Item(2, points: 12));

        Assert.False(BurndownCalculator.Calculate(sprint, TestData.Now).AtRisk);
    }

    [Fact]
    public void Calculate_NothingRemaining_IsNotAtRisk()
    {
        var sprint = Sprint(TestData.Item(1, state: "Closed", points: 8));

        Assert.False(BurndownCalculator.Calculate(sprint, TestData.Now).AtRisk);
    }

    [Fact]
    public void Calculate_SprintOverWithWorkLeft_IsAtRisk()
    {
        var sprint = new SprintSnapshot("P\\S1", TestData.Now.AddDays(-14), TestData.Now.AddDays(-1), [TestData.Item(1, points: 3)]);

        var result = BurndownCalculator.Calculate(sprint, TestData.Now);

        Assert.True(result.AtRisk);
        Assert.Equal(0, result.DaysLeft);
    }
}
