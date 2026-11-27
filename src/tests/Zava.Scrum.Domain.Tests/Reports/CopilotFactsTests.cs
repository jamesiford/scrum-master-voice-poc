using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Reports;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Reports;

public sealed class CopilotFactsTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);
    private static readonly Team Team = new("t1", "One", "UTC", 15, new("https://dev.azure.com/zava", "p", "t", "p"), new TeamBehaviours(), "Ship");
    private static readonly Member Member = new("m1", "t1", "one", "One", "one@zava.test", "", 1, true);

    [Fact]
    public void Build_Commitments_FiltersOtherTeamMemberRetractedAndUnreadableIds()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t1", Now, null);
        session.RecordEvent(StandupEventType.Commitment, "m1", [1], "Finish #1", Now);
        session.RecordEvent(StandupEventType.Commitment, "m1", [999], "Finish #999", Now);
        session.RecordEvent(StandupEventType.Commitment, "m1", [1], "Talk about #999", Now);
        session.RecordEvent(StandupEventType.Commitment, "m2", [1], "Someone else's #1", Now);
        session.RecordEvent(StandupEventType.Commitment, "m1", [1], "Retracted #1", Now);
        session.Retract(session.Events[^1].Id);
        var foreign = StandupSession.Prepare(Guid.NewGuid(), "t2", Now, null);
        foreign.RecordEvent(StandupEventType.Commitment, "m1", [1], "Foreign #1", Now);

        var facts = CopilotFactsBuilder.Build(Team, CopilotSkill.Commitments, Sprint(Item(1)), Member, [session, foreign], Now);

        Assert.Single(facts.Lines);
        Assert.Equal("#1: Finish #1", facts.Lines[0]);
        Assert.Equal([new WorkItemId(1)], facts.ItemIds);
    }

    [Fact]
    public void Build_MyItems_UsesActualAssigneeNotOwnerTagAndBoundsLinesWithoutTruncatingCount()
    {
        var items = Enumerable.Range(1, 7).Select(Item).Append(Item(20) with { AssignedTo = "other", OwnerKey = "one" }).ToArray();

        var facts = CopilotFactsBuilder.Build(Team, CopilotSkill.MyItems, Sprint(items), Member, [], Now);

        Assert.Contains("7 readable", facts.Summary, StringComparison.Ordinal);
        Assert.Equal(5, facts.Lines.Count);
        Assert.DoesNotContain(new WorkItemId(20), facts.ItemIds);
    }

    [Fact]
    public void Build_ItemLabels_DropsUnreadableReferencesBeforeChannelOutput()
    {
        var item = Item(1) with { Title = "Related to #999", State = "Waiting for #999" };

        var facts = CopilotFactsBuilder.Build(Team, CopilotSkill.MyItems, Sprint(item), Member, [], Now);

        Assert.Equal("#1: Work item - State unavailable", Assert.Single(facts.Lines));
        Assert.DoesNotContain("999", string.Join("\n", facts.Lines), StringComparison.Ordinal);
    }

    [Fact]
    public void Build_StandupToday_UsesTeamLocalDateAndExcludesOtherCeremoniesAndTeams()
    {
        var localTeam = new Team("t1", "One", "America/Los_Angeles", 15, Team.Ado, Team.Behaviours, "Ship");
        var yesterday = StandupSession.Prepare(Guid.NewGuid(), "t1", Now.AddHours(-3), null);
        var foreign = StandupSession.Prepare(Guid.NewGuid(), "t2", Now, null);
        var retro = StandupSession.Prepare(Guid.NewGuid(), "t1", Now, null, CeremonyKind.Retrospective);

        var facts = CopilotFactsBuilder.Build(localTeam, CopilotSkill.StandupStatus, Sprint(), Member, [yesterday, foreign, retro], Now);

        Assert.Equal("No standup is prepared for today.", facts.Summary);
        Assert.Empty(facts.Lines);
    }

    private static WorkItem Item(int id) => new(new(id), "Item", "Task", "Active", Member.AdoUniqueName, null, Now, Now, null, null, null, []);
    private static SprintSnapshot Sprint(params WorkItem[] items) => new("p\\s", Now.AddDays(-1), Now.AddDays(5), items);
}
