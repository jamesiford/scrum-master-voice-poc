using Mcm.Scrum.Domain.Kpis;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Domain.Tests.Standups;

public sealed class StandupAgendaTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    private static StandupSession Session()
    {
        MemberBrief M(string id) => new(id, id, [], "q");
        var brief = new PreBrief("t", "p", "g", 3, new Burndown(10, 4, 6, 3, false), [M("a"), M("b"), M("c")], [], [], "s");
        return StandupSession.Prepare(Guid.NewGuid(), "t", Now, brief);
    }

    [Fact]
    public void NextSpeaker_DeferredMember_IsSkippedThenCircledBackAtTheEnd()
    {
        var s = Session();
        s.RecordEvent(StandupEventType.Deferred, "a", [], "Stepped away", Now);

        Assert.Equal("b", s.NextSpeaker()!.Value.Member.MemberId);
        s.CompleteTurn("b");
        s.CompleteTurn("c");
        var back = s.NextSpeaker()!.Value;

        Assert.Equal("a", back.Member.MemberId);
        Assert.True(back.CircleBack);
    }

    [Fact]
    public void IsDeferred_AfterCircledBack_IsFalse()
    {
        var s = Session();
        s.RecordEvent(StandupEventType.Deferred, "a", [], "x", Now);
        s.RecordEvent(StandupEventType.CircledBack, "a", [], "x", Now);

        Assert.False(s.IsDeferred("a"));
    }

    [Fact]
    public void Retract_MarksTheEventAndHidesItFromActiveEvents()
    {
        var s = Session();
        s.RecordEvent(StandupEventType.Blocker, "a", [1], "Blocked on X", Now);
        var id = s.Events[0].Id;

        Assert.True(s.Retract(id));

        Assert.Empty(s.ActiveEvents);
        Assert.Single(s.Events);
        Assert.False(s.Retract(id));
    }

    [Fact]
    public void HasSpoken_OnlyWhenSomethingIsAttributedToTheMember()
    {
        var s = Session();
        s.RecordEvent(StandupEventType.NoResponse, "a", [], "silent", Now);

        Assert.False(s.HasSpoken("a"));
        Assert.Equal(1, s.NoResponseCount("a"));
    }
}
