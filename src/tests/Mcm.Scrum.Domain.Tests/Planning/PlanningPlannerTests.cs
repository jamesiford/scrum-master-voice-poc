using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Planning;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Tests.Planning;

public sealed class PlanningPlannerTests
{
    private static readonly DateTimeOffset T = new(2026, 1, 8, 0, 0, 0, TimeSpan.Zero);

    private static WorkItem Item(int id, string type, string state, string? owner, double? points, double? hours = null) =>
        new(new WorkItemId(id), $"Item {id}", type, state, null, owner, T, T, points, hours, null, []);

    private static Member Person(string key, string name, bool active = true) => new($"m-{key}", "t", key, name, null, string.Empty, 1, active);

    [Fact]
    public void Plan_UnassignedItems_GoToTheLeastLoadedMemberAndUpdateTheirLoad()
    {
        var sprint = new SprintSnapshot("p", T, T, [Item(1, "User Story", "Active", "a", 8), Item(2, "User Story", "New", null, 5), Item(3, "User Story", "New", null, 3)]);

        var prep = PlanningPlanner.Plan(sprint, [Person("a", "Ann"), Person("b", "Bob")]);

        Assert.Equal(["Bob", "Bob"], prep.Suggestions.Select(s => s.DisplayName));
        Assert.Equal(8, prep.Loads.Single(l => l.DisplayName == "Ann").OpenPoints);
    }

    [Fact]
    public void Plan_DoneItems_AreNotCarryOverAndNotReadinessGaps()
    {
        var sprint = new SprintSnapshot("p", T, T, [Item(1, "User Story", "Closed", "a", null), Item(2, "User Story", "Active", "a", null)]);

        var prep = PlanningPlanner.Plan(sprint, [Person("a", "Ann")]);

        Assert.Equal(2, Assert.Single(prep.CarryOver).WorkItemId);
        Assert.Contains("No story points", Assert.Single(prep.NotReady).Reasons);
    }

    [Fact]
    public void Plan_InactiveMember_IsNeverSuggested()
    {
        var sprint = new SprintSnapshot("p", T, T, [Item(1, "Task", "New", null, null, 4)]);

        var prep = PlanningPlanner.Plan(sprint, [Person("a", "Ann", active: false), Person("b", "Bob")]);

        Assert.Equal("Bob", Assert.Single(prep.Suggestions).DisplayName);
    }
}
