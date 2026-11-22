using Mcm.Scrum.Domain.Flags;
using Mcm.Scrum.Domain.FollowUps;
using Mcm.Scrum.Domain.Kpis;
using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Tests.Standups;

public sealed class CarryOverTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);
    private static readonly Team Team = new("t1", "Payments", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "Goal");

    private static readonly Member[] Roster =
    [
        new("m1", "t1", "daniel", "Daniel Park", null, "Daniel", 1, true),
        new("m2", "t1", "maya", "Maya Lin", null, "MY-ah", 2, true),
    ];

    private static readonly MemberBrief[] Briefs =
    [
        new("m1", "Daniel Park", [new BriefItem(new WorkItemId(5), "Story five", "User Story", "Active", 1, [])], "q"),
        new("m2", "Maya Lin", [], "q"),
    ];

    private static StandupSession Standup(DateTimeOffset at, params (StandupEventType Type, string Member, int Item, string Text)[] events)
    {
        var brief = new PreBrief("t1", "p", "g", 3, new Burndown(10, 4, 6, 3, false), Briefs, [], [], "s");
        var s = StandupSession.Prepare(Guid.NewGuid(), "t1", at, brief);
        s.Start("simulated", null, 15, at);
        foreach (var e in events)
        {
            s.RecordEvent(e.Type, e.Member, [e.Item], e.Text, at);
        }

        return s;
    }

    private static readonly IReadOnlySet<int> Open = new HashSet<int> { 5, 6 };

    [Fact]
    public void Collect_CommitmentFromLastStandup_IsOneStandupAgoAndNotMissed()
    {
        var carry = CommitmentCarryOver.Collect([Standup(Now.AddDays(-1), (StandupEventType.Commitment, "m1", 5, "finish story 5"))], Open);

        var c = Assert.Single(carry.Commitments["m1"]);

        Assert.Equal(1, c.StandupsAgo);
        Assert.False(c.IsMissed);
    }

    [Fact]
    public void Collect_CommitmentTwoStandupsAgoStillOpen_IsMissed()
    {
        var older = Standup(Now.AddDays(-2), (StandupEventType.Commitment, "m1", 5, "finish story 5"));
        var recent = Standup(Now.AddDays(-1));

        var carry = CommitmentCarryOver.Collect([recent, older], Open);

        var missed = Assert.Single(CommitmentCarryOver.Missed(carry));
        Assert.Equal("m1", missed.MemberId);
        Assert.Equal(2, missed.Commitment.StandupsAgo);
    }

    [Fact]
    public void Collect_ItemNoLongerOpen_IsDropped()
    {
        var carry = CommitmentCarryOver.Collect([Standup(Now.AddDays(-1), (StandupEventType.Commitment, "m1", 5, "finish"))], new HashSet<int> { 6 });

        Assert.Empty(carry.Commitments);
    }

    [Fact]
    public void Collect_RetractedCommitment_IsIgnored()
    {
        var s = Standup(Now.AddDays(-1), (StandupEventType.Commitment, "m1", 5, "finish"));
        s.Retract(s.Events.Single(e => e.Type == StandupEventType.Commitment).Id);

        Assert.Empty(CommitmentCarryOver.Collect([s], Open).Commitments);
    }

    [Fact]
    public void Collect_OtherCeremoniesAndUnstartedSessions_AreIgnored()
    {
        var review = StandupSession.Prepare(Guid.NewGuid(), "t1", Now, null, CeremonyKind.SprintReview);
        var unstarted = StandupSession.Prepare(Guid.NewGuid(), "t1", Now, new PreBrief("t1", "p", "g", 3, new Burndown(10, 4, 6, 3, false), Briefs, [], [], "s"));

        var carry = CommitmentCarryOver.Collect([review, unstarted], Open);

        Assert.Empty(carry.Commitments);
        Assert.Empty(carry.Blocked);
    }

    [Fact]
    public void Collect_BlockerWithUnblocker_IsStillBlockedWithThatPerson()
    {
        var s = Standup(Now.AddDays(-1), (StandupEventType.Blocker, "m1", 5, "waiting on vendor"));
        s.SetUnblocker(s.Events.Single(e => e.Type == StandupEventType.Blocker).Id, "m2");

        var blocked = Assert.Single(CommitmentCarryOver.Collect([s], Open).Blocked);

        Assert.Equal(new StillBlocked(5, "m2"), blocked);
    }

    [Fact]
    public void Build_MissedCommitment_FlagsTheItemAndAddsPreviousCommitmentsAndRisk()
    {
        var older = Standup(Now.AddDays(-2), (StandupEventType.Commitment, "m1", 5, "finish story 5"));
        var carry = CommitmentCarryOver.Collect([Standup(Now.AddDays(-1)), older], Open);
        var sprint = new SprintSnapshot("p\\s", Now.AddDays(-4), Now.AddDays(6), [TestData.Item(5, ownerKey: "daniel", points: 5)]);

        var brief = new PreBriefBuilder(FlagEvaluator.CreateDefault()).Build(Team, Roster, sprint, Now, carry);

        var daniel = brief.Members.Single(m => m.MemberId == "m1");
        Assert.Contains(Flag.MissedCommitment, daniel.Items.Single().Flags);
        Assert.Single(daniel.PreviousCommitments!);
        Assert.Contains(brief.TeamRisks, r => r.Contains("Daniel Park committed to item 5", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_StillBlockedWithUnblocker_ListsHelpRequestForTheUnblockerAndARisk()
    {
        var s = Standup(Now.AddDays(-1), (StandupEventType.Blocker, "m1", 5, "waiting on vendor"));
        s.SetUnblocker(s.Events.Single(e => e.Type == StandupEventType.Blocker).Id, "m2");
        var sprint = new SprintSnapshot("p\\s", Now.AddDays(-4), Now.AddDays(6), [TestData.Item(5, ownerKey: "daniel", daysInState: 3, points: 5, tags: "Blocked")]);

        var brief = new PreBriefBuilder(FlagEvaluator.CreateDefault()).Build(Team, Roster, sprint, Now, CommitmentCarryOver.Collect([s], Open));

        var help = Assert.Single(brief.Members.Single(m => m.MemberId == "m2").HelpRequests!);
        Assert.Equal("Daniel Park", help.BlockedMemberName);
        Assert.Contains(brief.TeamRisks, r => r.Contains("Maya Lin was named to help", StringComparison.Ordinal));
    }

    [Fact]
    public void CarryOverNotes_CommitmentAndHelpRequest_AreSpokenAsInstructions()
    {
        var member = new MemberBrief(
            "m1",
            "Daniel Park",
            [new BriefItem(new WorkItemId(5), "Story five", "User Story", "Active", 1, [])],
            "q",
            [new PreviousCommitment(5, "finish story 5", 1)],
            [new HelpRequest(9, "Bug", "Gateway timeouts", "Maya Lin")]);

        var notes = CoverageNotes.CarryOverNotes(member);

        Assert.Contains("yesterday they said \"finish story 5\" about story 5", notes, StringComparison.Ordinal);
        Assert.Contains("help Maya Lin with bug 9", notes, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Maya can help", "m2")]
    [InlineData("I think Maya Lin could look at it", "m2")]
    [InlineData("maya", "m2")]
    [InlineData("nobody really", null)]
    [InlineData("Daniel can do it", null)]
    [InlineData("Maya or Daniel", "m2")]
    [InlineData("the vendor", null)]
    [InlineData("", null)]
    public void Resolve_Answer_MatchesExactlyOneOtherRosterMember(string answer, string? expected)
    {
        Assert.Equal(expected, UnblockerResolver.Resolve(answer, Briefs, "m1"));
    }
}

public sealed class SessionKindTests
{
    [Fact]
    public void Restore_SnapshotJsonWithoutKind_LoadsAsDailyScrum()
    {
        var snapshot = new StandupSessionSnapshot(Guid.NewGuid(), "t", DateTimeOffset.UnixEpoch, StandupStatus.Ended, null, null, null, 15, null, null, new Dictionary<string, Attendance>(), [], [], []);
        var json = System.Text.Json.JsonSerializer.Serialize(snapshot);
        var withoutKind = json.Replace(",\"Kind\":0", string.Empty, StringComparison.Ordinal);

        var restored = StandupSession.Restore(System.Text.Json.JsonSerializer.Deserialize<StandupSessionSnapshot>(withoutKind)!);

        Assert.DoesNotContain("Kind", withoutKind, StringComparison.Ordinal);
        Assert.Equal(CeremonyKind.DailyScrum, restored.Kind);
    }

    [Fact]
    public void Prepare_OtherCeremony_KeepsItsKindThroughSnapshot()
    {
        var s = StandupSession.Prepare(Guid.NewGuid(), "t", DateTimeOffset.UnixEpoch, null, CeremonyKind.Retrospective);

        Assert.Equal(CeremonyKind.Retrospective, StandupSession.Restore(s.ToSnapshot()).Kind);
    }
}
