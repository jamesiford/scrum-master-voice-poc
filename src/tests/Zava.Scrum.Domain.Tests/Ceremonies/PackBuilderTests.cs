using Zava.Scrum.Domain.Ceremonies;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Ceremonies;

public sealed class PackBuilderTests
{
    private static readonly DateTimeOffset T = new(2026, 1, 8, 0, 0, 0, TimeSpan.Zero);

    private static WorkItem Item(int id, string type, string state, string? owner, double? points, double? hours = null) =>
        new(new WorkItemId(id), $"Item {id}", type, state, null, owner, T, T, points, hours, null, []);

    private static Member Person(string key, string name, string? unique = null) => new($"m-{key}", "t", key, name, unique, string.Empty, 1, true);

    private static SprintSnapshot Sprint() => new("P\\Sprint 1", T, T, [
        Item(1, "User Story", "Closed", "a", 8),
        Item(2, "User Story", "Active", "a", 5),
        Item(3, "User Story", "New", null, 3),
        Item(4, "Bug", "Closed", "b", 2),
    ]);

    [Fact]
    public void Review_DeliveredPointsAreComputedFromDoneStoriesAndBugs()
    {
        var pack = ReviewPackBuilder.Build(Sprint());

        Assert.Equal(FollowUpKind.ReviewPack, pack.FollowUpKind);
        Assert.Contains(pack.Facts, f => f.Label == "Delivered" && f.Value == "10 of 18 points");
        Assert.Equal([1, 4], pack.Changes.Select(c => c.WorkItemId));
        Assert.All(pack.Changes, c => Assert.Equal(ChangeOp.Comment, c.Op));
    }

    [Fact]
    public void Retro_ReadinessGapsBecomeAtMostThreeCreateWorkItemActionsInTheNextIteration()
    {
        var items = Enumerable.Range(1, 6).Select(i => Item(i, "User Story", "New", "a", null)).ToList();
        var sprint = new SprintSnapshot("P\\Sprint 1", T, T, items);

        var pack = RetroPackBuilder.Build(sprint, [], [Person("a", "Ann")], "P\\Sprint 2");

        Assert.Equal(FollowUpKind.RetroActions, pack.FollowUpKind);
        Assert.Equal(RetroPackBuilder.MaxActions, pack.Changes.Count);
        var create = pack.Changes[0];
        Assert.Equal(ChangeOp.CreateWorkItem, create.Op);
        Assert.Equal(0, create.WorkItemId);
        Assert.Equal("P\\Sprint 2", create.New!.IterationPath);
        Assert.Contains("retro-action", create.New.Tags);
        Assert.StartsWith("Retro action:", create.New.Title, StringComparison.Ordinal);
    }

    [Fact]
    public void IsGrounded_TextWithOnlyPackNumbers_IsAccepted()
    {
        var pack = ReviewPackBuilder.Build(Sprint());

        Assert.True(pack.IsGrounded("We delivered 10 of 18 points across 2 stories and bugs."));
    }

    [Fact]
    public void IsGrounded_TextWithAnInventedNumber_IsRejected()
    {
        var pack = ReviewPackBuilder.Build(Sprint());

        Assert.False(pack.IsGrounded("We delivered 12 of 18 points."));
    }
}
