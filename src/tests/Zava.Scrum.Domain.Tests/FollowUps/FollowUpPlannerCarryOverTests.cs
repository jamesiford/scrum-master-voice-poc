using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.FollowUps;

public sealed class FollowUpPlannerCarryOverTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    private static BriefItem Blocked(int id, int days) => new(new WorkItemId(id), $"Item {id}", "Task", "Active", days, [Flag.Blocked]);

    private static StandupSession Session(MemberBrief[] members)
    {
        var brief = new PreBrief("t", "p", "g", 3, new Burndown(10, 4, 6, 3, false), members, [], [], "s");
        var s = StandupSession.Prepare(Guid.NewGuid(), "t", Now, brief);
        s.Start("simulated", null, 15, Now);
        return s;
    }

    [Fact]
    public void Plan_BlockerWithNamedUnblocker_CreatesUnblockFollowUpForThatMember()
    {
        var daniel = new MemberBrief("m1", "Daniel Park", [Blocked(5, 0)], "q");
        var maya = new MemberBrief("m2", "Maya Lin", [], "q");
        var s = Session([daniel, maya]);
        s.RecordEvent(StandupEventType.Blocker, "m1", [5], "waiting on the vendor", Now);
        s.SetUnblocker(s.Events.Single(e => e.Type == StandupEventType.Blocker).Id, "m2");

        var unblock = FollowUpPlanner.Plan(s, 2, Guid.NewGuid, Now).Single(f => f.Kind == FollowUpKind.Unblock);

        Assert.Equal("m2", unblock.MemberId);
        Assert.Contains("Daniel is blocked on Task 5 (Item 5)", unblock.Message, StringComparison.Ordinal);
        Assert.Contains("waiting on the vendor", unblock.Message, StringComparison.Ordinal);
        Assert.Empty(unblock.Changes);
    }

    [Fact]
    public void Plan_BlockerWithoutUnblocker_CreatesNoUnblockFollowUp()
    {
        var s = Session([new MemberBrief("m1", "Daniel Park", [Blocked(5, 0)], "q"), new MemberBrief("m2", "Maya Lin", [], "q")]);
        s.RecordEvent(StandupEventType.Blocker, "m1", [5], "waiting", Now);

        Assert.DoesNotContain(FollowUpPlanner.Plan(s, 2, Guid.NewGuid, Now), f => f.Kind == FollowUpKind.Unblock);
    }

    [Theory]
    [InlineData(2, 2, true)]
    [InlineData(3, 2, true)]
    [InlineData(1, 2, false)]
    public void Plan_BlockedLongEnough_EscalatesToTheScrumMasterEvenIfNotRaisedAgain(int days, int afterDays, bool escalated)
    {
        var s = Session([new MemberBrief("m1", "Daniel Park", [Blocked(5, days)], "q")]);

        var escalations = FollowUpPlanner.Plan(s, afterDays, Guid.NewGuid, Now).Where(f => f.Kind == FollowUpKind.Escalation).ToList();

        Assert.Equal(escalated, escalations.Count == 1);
        Assert.All(escalations, e => Assert.True(e.ForScrumMaster));
    }

    [Fact]
    public void Plan_MissedCommitment_EscalatesToTheScrumMaster()
    {
        var item = new BriefItem(new WorkItemId(5), "Story five", "User Story", "Active", 1, [Flag.MissedCommitment]);
        var daniel = new MemberBrief("m1", "Daniel Park", [item], "q", [new PreviousCommitment(5, "finish story 5", 2)]);

        var f = Assert.Single(FollowUpPlanner.Plan(Session([daniel]), 2, Guid.NewGuid, Now), x => x.Kind == FollowUpKind.Escalation);

        Assert.True(f.ForScrumMaster);
        Assert.Contains("Daniel said \"finish story 5\" on User Story 5", f.Message, StringComparison.Ordinal);
        Assert.Contains("2 standups ago", f.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_CommitmentOnlyOneStandupOld_DoesNotEscalate()
    {
        var item = new BriefItem(new WorkItemId(5), "Story five", "User Story", "Active", 1, []);
        var daniel = new MemberBrief("m1", "Daniel Park", [item], "q", [new PreviousCommitment(5, "finish story 5", 1)]);

        Assert.DoesNotContain(FollowUpPlanner.Plan(Session([daniel]), 2, Guid.NewGuid, Now), f => f.Kind == FollowUpKind.Escalation);
    }
}
