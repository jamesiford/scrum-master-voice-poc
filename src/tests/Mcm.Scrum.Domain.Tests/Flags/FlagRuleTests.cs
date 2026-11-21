using Mcm.Scrum.Domain.Flags;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Tests.Flags;

public sealed class FlagRuleTests
{
    private static readonly FlagContext Context = new(TestData.Now, new TeamBehaviours(StaleDays: 2));

    public static TheoryData<string, WorkItem, Flag, bool> Cases => new()
    {
        { "stale: active 3 days", TestData.Item(daysInState: 3), Flag.Stale, true },
        { "stale: active exactly 2 days", TestData.Item(daysInState: 2), Flag.Stale, false },
        { "stale: new 5 days is not in progress", TestData.Item(state: "New", daysInState: 5), Flag.Stale, false },
        { "blocked: tagged", TestData.Item(tags: "Blocked"), Flag.Blocked, true },
        { "blocked: tag is case-insensitive", TestData.Item(tags: "blocked"), Flag.Blocked, true },
        { "blocked: done item ignored", TestData.Item(state: "Closed", tags: "Blocked"), Flag.Blocked, false },
        { "overdue: due yesterday", TestData.Item(due: TestData.Now.AddDays(-1)), Flag.Overdue, true },
        { "overdue: due tomorrow", TestData.Item(due: TestData.Now.AddDays(1)), Flag.Overdue, false },
        { "overdue: no due date", TestData.Item(), Flag.Overdue, false },
        { "overdue: done ignored", TestData.Item(state: "Closed", due: TestData.Now.AddDays(-3)), Flag.Overdue, false },
        { "unassigned: nobody", TestData.Item(assignedTo: null), Flag.Unassigned, true },
        { "unassigned: assignee", TestData.Item(), Flag.Unassigned, false },
        { "unassigned: owner tag counts", TestData.Item(assignedTo: null, ownerKey: "priya"), Flag.Unassigned, false },
        { "no_recent_update: untouched 2 days", TestData.Item(daysSinceChange: 2), Flag.NoRecentUpdate, true },
        { "no_recent_update: touched today", TestData.Item(daysSinceChange: 0), Flag.NoRecentUpdate, false },
        { "no_recent_update: new item ignored", TestData.Item(state: "New", daysSinceChange: 4), Flag.NoRecentUpdate, false },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Evaluate_Case_RaisesExpectedFlag(string name, WorkItem item, Flag flag, bool expected)
    {
        Assert.NotNull(name);

        var flags = FlagEvaluator.CreateDefault().Evaluate(item, Context);

        Assert.Equal(expected, flags.Contains(flag));
    }
}
