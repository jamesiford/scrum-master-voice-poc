using Zava.Scrum.Domain.Capacity;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Domain.Tests.Teams;

public sealed class TeamAccessTests
{
    private static TeamMembership In(string team, TeamRole role) => new(team, Guid.NewGuid(), role);

    [Theory]
    [InlineData(TeamRole.Reader, TeamPermission.Read, true)]
    [InlineData(TeamRole.Member, TeamPermission.Read, true)]
    [InlineData(TeamRole.TeamLead, TeamPermission.Read, true)]
    [InlineData(TeamRole.Reader, TeamPermission.Operate, false)]
    [InlineData(TeamRole.Member, TeamPermission.Operate, false)]
    [InlineData(TeamRole.TeamLead, TeamPermission.Operate, true)]
    [InlineData(TeamRole.Member, TeamPermission.Approve, false)]
    [InlineData(TeamRole.TeamLead, TeamPermission.Approve, true)]
    [InlineData(TeamRole.TeamLead, TeamPermission.Registry, false)]
    public void Authorise_MemberOfTheTeam_FollowsTheRole(TeamRole role, TeamPermission permission, bool allowed)
    {
        Assert.Equal(allowed, TeamAccess.Authorise("a", false, In("a", role), permission).IsSuccess);
    }

    [Fact]
    public void Authorise_AMemberOfAnotherTeam_GetsNotFoundSoTheTeamIsNotRevealed()
    {
        var r = TeamAccess.Authorise("a", false, In("b", TeamRole.TeamLead), TeamPermission.Read);

        Assert.Equal(ErrorType.NotFound, r.Error!.Type);
    }

    [Fact]
    public void Authorise_NoMembership_IsNotFound() =>
        Assert.Equal(ErrorType.NotFound, TeamAccess.Authorise("a", false, null, TeamPermission.Read).Error!.Type);

    [Theory]
    [InlineData(TeamPermission.Read)]
    [InlineData(TeamPermission.Operate)]
    [InlineData(TeamPermission.Approve)]
    [InlineData(TeamPermission.Registry)]
    public void Authorise_Admin_ReachesEveryTeamAndPermission(TeamPermission permission) =>
        Assert.True(TeamAccess.Authorise("a", true, null, permission).IsSuccess);

    [Fact]
    public void Authorise_MemberWithoutTheRole_IsForbiddenNotNotFound() =>
        Assert.Equal(ErrorType.Forbidden, TeamAccess.Authorise("a", false, In("a", TeamRole.Member), TeamPermission.Operate).Error!.Type);
}

public sealed class WorkItemScopeTests
{
    private static readonly AdoSettings Payments = new("https://dev.azure.com/o", "Payments", "Payments Team", "Payments\\Core");

    [Theory]
    [InlineData("Payments", "Payments\\Core", true)]
    [InlineData("payments", "PAYMENTS\\core\\Sub", true)]
    [InlineData("Payments", "Payments\\CoreOther", false)]
    [InlineData("Payments", "Payments", false)]
    [InlineData("Billing", "Payments\\Core", false)]
    [InlineData("Payments", "Billing\\Core", false)]
    [InlineData(null, "Payments\\Core", false)]
    [InlineData("Payments", null, false)]
    public void Contains_RequiresTheProjectAndAnAreaAtOrBelowTheTeamsRoot(string? project, string? area, bool expected) =>
        Assert.Equal(expected, WorkItemScope.Contains(Payments, project, area));

    [Theory]
    [InlineData("Payments\\Sprint 1", true)]
    [InlineData("Billing\\Sprint 1", false)]
    [InlineData("", false)]
    public void ContainsNew_UsesTheFirstSegmentOfTheIterationAsTheProject(string path, bool expected) =>
        Assert.Equal(expected, WorkItemScope.ContainsNew(Payments, path));

    [Fact]
    public void OutOfScope_IsForbiddenAndNamesTheItem() =>
        Assert.Equal(ErrorType.Forbidden, WorkItemScope.OutOfScope(7).Type);
}

public sealed class CapacityGovernorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 9, 30, 0, TimeSpan.Zero);
    private static readonly CapacityLimits Limits = new(2, 5, 10);

    private static DueStandup Due(string team, int minutesAgo = 0) => new(team, Now.AddMinutes(-minutesAgo));

    [Fact]
    public void Plan_ThreeDueAndRoomForTwo_StartsOnePerTickInDueOrder()
    {
        var plan = CapacityGovernor.Plan([Due("c"), Due("a", 2), Due("b", 1)], 0, null, Now, Limits);

        Assert.Equal(["a", "b", "c"], plan.Select(p => p.TeamId));
        Assert.Equal([true, false, false], plan.Select(p => p.Start));
        Assert.Contains("staggered", plan[1].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_AtTheLimit_NobodyStartsAndTheReasonSaysWhy()
    {
        var plan = CapacityGovernor.Plan([Due("a")], 2, null, Now, Limits);

        Assert.False(plan[0].Start);
        Assert.Contains("2 of 2", plan[0].Reason, StringComparison.Ordinal);
        Assert.False(plan[0].Blocked);
    }

    [Fact]
    public void Plan_WaitingPastTheGraceWindowWithNoCapacity_IsBlockedSoSomeoneIsTold()
    {
        var plan = CapacityGovernor.Plan([Due("a", 11)], 2, null, Now, Limits);

        Assert.True(plan[0].Blocked);
    }

    [Fact]
    public void Plan_JustStartedAnother_WaitsForTheStaggerButIsNotBlocked()
    {
        var plan = CapacityGovernor.Plan([Due("a", 30)], 1, Now.AddSeconds(-2), Now, Limits);

        Assert.False(plan[0].Start);
        Assert.False(plan[0].Blocked);
    }

    [Fact]
    public void Plan_StaggerElapsed_StartsTheNext()
    {
        Assert.True(CapacityGovernor.Plan([Due("a")], 1, Now.AddSeconds(-6), Now, Limits)[0].Start);
    }

    [Fact]
    public void Plan_NothingDue_PlansNothing() => Assert.Empty(CapacityGovernor.Plan([], 0, null, Now, Limits));
}

public sealed class TeamsBindingTests
{
    [Fact]
    public void Matches_IgnoresTheThreadSuffixTeamsAppends()
    {
        var b = new TeamsBinding("t", "g", "19:abc@thread.tacv2", null, BindingPurpose.Recap);

        Assert.True(b.Matches("19:abc@thread.tacv2;messageid=1700000000"));
        Assert.False(b.Matches("19:other@thread.tacv2"));
        Assert.False(b.Matches(null));
    }

    [Fact]
    public void Team_SavedBeforeTheRegistryExisted_IsActiveWithNoBindingsAndTheDefaultCeremonies()
    {
        var team = new Team("t", "T", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "g");

        Assert.Equal(TeamStatus.Active, team.Status);
        Assert.Empty(team.Bindings);
        Assert.True(team.CeremonyFlags.DailyScrum);
        Assert.False(team.WorkSignalsEnabled);
        Assert.True(team.IsSchedulable);
    }

    [Theory]
    [InlineData(TeamStatus.Draft, false)]
    [InlineData(TeamStatus.Active, true)]
    [InlineData(TeamStatus.Paused, false)]
    [InlineData(TeamStatus.Archived, false)]
    public void IsSchedulable_OnlyForActiveTeams(TeamStatus status, bool expected)
    {
        var team = new Team("t", "T", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "g", Status: status);

        Assert.Equal(expected, team.IsSchedulable);
    }
}
