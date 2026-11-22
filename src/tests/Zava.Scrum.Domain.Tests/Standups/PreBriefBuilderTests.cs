using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Standups;

public sealed class PreBriefBuilderTests
{
    private static readonly Team Team = new("t1", "Payments", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "Ship retries");

    private static readonly Member[] Roster =
    [
        new("m1", "t1", "priya", "Priya", null, "PREE-ya", 1, true),
        new("m2", "t1", "marcus", "Marcus", null, "MAR-kus", 2, true),
        new("m3", "t1", "lena", "Lena", null, "LEE-na", 3, false),
    ];

    private static PreBrief Build(params WorkItem[] items) =>
        new PreBriefBuilder(FlagEvaluator.CreateDefault()).Build(
            Team, Roster, new SprintSnapshot("p\\s", TestData.Now.AddDays(-4), TestData.Now.AddDays(6), items), TestData.Now);

    [Fact]
    public void Build_StaleStory_AppearsOnOwnerWithStaleFlagAndQuestionNamingIt()
    {
        var brief = Build(
            TestData.Item(4521, ownerKey: "priya", daysInState: 3, points: 5),
            TestData.Item(4530, ownerKey: "marcus", points: 3));

        var priya = brief.Members.Single(m => m.MemberId == "m1");

        Assert.Equal(Flag.Stale, Assert.Single(priya.Items).Flags.Single());
        Assert.Contains("4521", priya.SuggestedQuestion, StringComparison.Ordinal);
        Assert.Empty(brief.Members.Single(m => m.MemberId == "m2").Items.SelectMany(i => i.Flags));
    }

    [Fact]
    public void Build_UnassignedItems_AreListedForTheTeamAndCountedAsRisk()
    {
        var brief = Build(TestData.Item(1, assignedTo: null), TestData.Item(2, assignedTo: null));

        Assert.Equal(2, brief.UnassignedItems.Count);
        Assert.Contains("2 items are unassigned", brief.TeamRisks);
    }

    [Fact]
    public void Build_AbsentMember_IsLeftOutOfTheBrief()
    {
        var brief = Build(TestData.Item(1, ownerKey: "lena"));

        Assert.DoesNotContain(brief.Members, m => m.MemberId == "m3");
    }

    [Fact]
    public void Build_DoneItems_AreNotInTheBriefButCountTowardBurndown()
    {
        var brief = Build(TestData.Item(1, ownerKey: "priya", state: "Closed", points: 8), TestData.Item(2, ownerKey: "priya", points: 5));

        Assert.Equal(2, Assert.Single(brief.Members, m => m.MemberId == "m1").Items.Single().Id.Value);
        Assert.Equal(8, brief.Burndown.CompletedPoints);
    }

    [Fact]
    public void Build_BlockedBug_ProducesTeamRiskWithItsAge()
    {
        var brief = Build(TestData.Item(4602, "Bug", ownerKey: "marcus", daysInState: 4, tags: "Blocked"));

        Assert.Contains("Bug 4602 blocked for 4 days", brief.TeamRisks);
    }

    [Fact]
    public void WithSuggestions_ReplacesQuestionsOnly_LeavesFactsUntouched()
    {
        var brief = Build(TestData.Item(4521, ownerKey: "priya", daysInState: 3, points: 5));

        var updated = PreBriefBuilder.WithSuggestions(brief, "S", new Dictionary<string, string> { ["m1"] = "Q?" });

        Assert.Equal("Q?", updated.Members.Single(m => m.MemberId == "m1").SuggestedQuestion);
        Assert.Equal(brief.Burndown, updated.Burndown);
        Assert.Equal("S", updated.Summary);
    }
}
