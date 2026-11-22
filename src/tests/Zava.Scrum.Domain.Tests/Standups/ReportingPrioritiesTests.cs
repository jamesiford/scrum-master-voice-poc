using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Standups;

public sealed class ReportingPrioritiesTests
{
    private static BriefItem Item(int id, string type, params Flag[] flags) => new(new WorkItemId(id), $"Title {id}", type, "Active", 3, flags);

    [Fact]
    public void For_OpenItems_ListsMostUrgentFirstAndSkipsCovered()
    {
        var m = new MemberBrief("m", "Maya", [Item(1, "User Story", Flag.Stale), Item(2, "Bug", Flag.Blocked), Item(3, "Task"), Item(4, "Task", Flag.Overdue)], "q");

        var text = ReportingPriorities.For(m, new HashSet<int> { 4 });

        Assert.True(text.IndexOf("bug 2", StringComparison.Ordinal) < text.IndexOf("story 1", StringComparison.Ordinal));
        Assert.DoesNotContain("task 4", text, StringComparison.Ordinal);
        Assert.Contains("task 3", text, StringComparison.Ordinal);
    }

    [Fact]
    public void For_AtRiskItem_RanksBetweenOverdueAndStale()
    {
        var m = new MemberBrief("m", "Maya", [Item(1, "Task", Flag.Stale), Item(2, "Task", Flag.AtRisk), Item(3, "Task", Flag.Overdue)], "q");

        var text = ReportingPriorities.For(m, new HashSet<int>());

        Assert.True(text.IndexOf("task 3", StringComparison.Ordinal) < text.IndexOf("task 2", StringComparison.Ordinal));
        Assert.True(text.IndexOf("task 2", StringComparison.Ordinal) < text.IndexOf("task 1", StringComparison.Ordinal));
    }

    [Fact]
    public void For_EverythingCovered_SaysSo() =>
        Assert.Contains("already covered", ReportingPriorities.For(new MemberBrief("m", "Maya", [Item(1, "Task")], "q"), new HashSet<int> { 1 }), StringComparison.Ordinal);
}

public sealed class FollowUpNoResponseTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Plan_PresentButSilentMember_GetsAFollowUpThatSaysTheyWereThere()
    {
        var brief = new PreBrief("t", "p", "g", 3, new Burndown(10, 4, 6, 3, false), [new MemberBrief("a", "Ann Lee", [], "q")], [], [], "s");
        var s = StandupSession.Prepare(Guid.NewGuid(), "t", Now, brief);
        s.RecordEvent(StandupEventType.NoResponse, "a", [], "silent", Now);

        var plan = FollowUpPlanner.Plan(s, 2, Guid.NewGuid, Now);

        var f = Assert.Single(plan);
        Assert.Contains("did not hear", f.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_RetractedBlocker_IsNotFollowedUp()
    {
        var item = new BriefItem(new WorkItemId(1), "t", "Bug", "Active", 1, []);
        var brief = new PreBrief("t", "p", "g", 3, new Burndown(10, 4, 6, 3, false), [new MemberBrief("a", "Ann", [item], "q")], [], [], "s");
        var s = StandupSession.Prepare(Guid.NewGuid(), "t", Now, brief);
        s.RecordEvent(StandupEventType.Blocker, "a", [1], "Blocked", Now);
        s.Retract(s.Events[0].Id);

        Assert.Empty(FollowUpPlanner.Plan(s, 2, Guid.NewGuid, Now));
    }
}
