using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Planning;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Planning;

public sealed class SprintProposalTests
{
    private static readonly DateTimeOffset T = new(2026, 1, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly double[] History = [32, 35, 34, 40, 38, 40];

    private static WorkItem Story(int id, double? points, string state = "Active", string? owner = null, string? criteria = "Given, when, then", string type = "User Story") =>
        new(new WorkItemId(id), $"Item {id}", type, state, null, owner, T, T, points, null, null, [], null, criteria);

    private static Member Person(string key, int availability = 100) => new($"m-{key}", "t", key, key, $"{key}@x.com", key, 1, true, availability);

    private static readonly Member[] Roster = [Person("daniel"), Person("maya"), Person("admin")];

    private static SprintSnapshot Sprint(params WorkItem[] items) => new("P\\Sprint 1", T, T, items);

    private static PlanningProposal Propose(WorkItem[] sprint, WorkItem[] backlog, IReadOnlyList<Member>? roster = null, double[]? history = null) =>
        PlanningPlanner.Propose(Sprint(sprint), backlog, roster ?? Roster, history ?? History);

    [Fact]
    public void Propose_VelocityHistory_SetsTheRangeFromTheLastSixSprints()
    {
        var p = Propose([], [], history: [10, 99, 32, 35, 34, 40, 38, 40]);

        Assert.Equal(new VelocityRange(32, 40), p.Velocity);
    }

    [Fact]
    public void Propose_Capacity_IsAnEqualShareOfTheRangeTimesAvailability()
    {
        var roster = new[] { Person("daniel"), Person("maya", availability: 50), Person("admin") };

        var p = Propose([], [], roster);

        var daniel = p.Members.Single(m => m.DisplayName == "daniel");
        var maya = p.Members.Single(m => m.DisplayName == "maya");
        Assert.Equal((10.7, 13.3), (daniel.CapacityLow, daniel.CapacityHigh));
        Assert.Equal((5.3, 6.7), (maya.CapacityLow, maya.CapacityHigh));
    }

    [Fact]
    public void Propose_CarryOverFirstThenReadyBacklogInOrderUntilCapacity()
    {
        var sprint = new[] { Story(1, 10, owner: "daniel"), Story(2, 12, owner: "maya") };
        var backlog = new[] { Story(10, 5), Story(11, 5), Story(12, 5), Story(13, 5) };

        var p = Propose(sprint, backlog);

        Assert.Equal([1, 2, 10, 11, 12], p.Selected.Select(s => s.WorkItemId));
        Assert.Equal([PlanningSource.CarryOver, PlanningSource.CarryOver, PlanningSource.Backlog, PlanningSource.Backlog, PlanningSource.Backlog], p.Selected.Select(s => s.Source));
        Assert.Equal(37, p.TotalPoints);
        Assert.Equal(CommitmentLevel.Within, p.Commitment);
        Assert.Contains("Beyond capacity", Assert.Single(p.Excluded, e => e.WorkItemId == 13).Reasons[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Propose_ItemThatDoesNotFit_StopsSelectionEvenIfASmallerOneWould()
    {
        var sprint = new[] { Story(1, 36, owner: "daniel") };
        var backlog = new[] { Story(10, 8), Story(11, 2) };

        var p = Propose(sprint, backlog);

        Assert.Equal([1], p.Selected.Select(s => s.WorkItemId));
        Assert.Equal([10, 11], p.Excluded.Select(e => e.WorkItemId));
    }

    [Theory]
    [InlineData(null, "Given", "No story points")]
    [InlineData(5.0, null, "No acceptance criteria")]
    [InlineData(5.0, "  ", "No acceptance criteria")]
    [InlineData(13.0, "Given", "Too large")]
    public void Propose_NotReadyBacklogItem_IsExcludedWithTheReasonAndNeverSelected(double? points, string? criteria, string reason)
    {
        var p = Propose([], [Story(10, points, criteria: criteria)]);

        Assert.Empty(p.Selected);
        Assert.Contains(Assert.Single(p.Excluded).Reasons, r => r.StartsWith(reason, StringComparison.Ordinal));
    }

    [Fact]
    public void Propose_ItemWithTwoGaps_ListsBothReasons()
    {
        var p = Propose([], [Story(10, null, criteria: null)]);

        Assert.Equal(2, Assert.Single(p.Excluded).Reasons.Count);
    }

    [Fact]
    public void Propose_NewStoryInTheCurrentSprint_IsNotCarriedOverAndSaysWhy()
    {
        var p = Propose([Story(1, 5, state: "New", owner: "daniel")], []);

        Assert.Empty(p.Selected);
        Assert.Contains("Not started", Assert.Single(p.Excluded).Reasons[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Propose_TasksAndDoneItems_AreNeverCarriedOver()
    {
        var p = Propose([Story(1, 5, state: "Closed"), Story(2, null, type: "Task")], []);

        Assert.Empty(p.Selected);
        Assert.Empty(p.Excluded);
    }

    [Fact]
    public void Propose_CarryOverAloneAboveTheRange_IsOverCommitted()
    {
        var p = Propose([Story(1, 25, owner: "daniel"), Story(2, 20, owner: "maya")], [Story(10, 3)]);

        Assert.Equal(CommitmentLevel.Over, p.Commitment);
        Assert.Equal(45, p.TotalPoints);
        Assert.Contains(p.Warnings, w => w.StartsWith("Over-committed: 45 points", StringComparison.Ordinal));
        Assert.DoesNotContain(p.Selected, s => s.WorkItemId == 10);
    }

    [Fact]
    public void Propose_BelowTheRange_IsUnderCommittedAndSaysSo()
    {
        var p = Propose([Story(1, 5, owner: "daniel")], []);

        Assert.Equal(CommitmentLevel.Under, p.Commitment);
        Assert.Contains(p.Warnings, w => w.StartsWith("Under-committed: 5 points", StringComparison.Ordinal));
    }

    [Fact]
    public void Propose_UnassignedItems_GoToTheLeastLoadedMemberAfterOwnedItemsAreCounted()
    {
        var sprint = new[] { Story(1, 8, owner: "daniel"), Story(2, 6, owner: "maya") };
        var backlog = new[] { Story(10, 5), Story(11, 5) };

        var p = Propose(sprint, backlog);

        var first = p.Selected.Single(s => s.WorkItemId == 10);
        var second = p.Selected.Single(s => s.WorkItemId == 11);
        Assert.Equal(("admin", true), (first.OwnerName, first.OwnerSuggested));
        Assert.Equal(("admin", true), (second.OwnerName, second.OwnerSuggested));
        Assert.Equal(("daniel", false), (p.Selected[0].OwnerName, p.Selected[0].OwnerSuggested));
    }

    [Fact]
    public void Propose_MemberWithLowAvailability_IsFlaggedOverloaded()
    {
        var roster = new[] { Person("daniel"), Person("maya", availability: 50), Person("admin") };

        var p = Propose([Story(1, 12, owner: "maya")], [], roster);

        Assert.True(p.Members.Single(m => m.DisplayName == "maya").Overloaded);
        Assert.Contains(p.Warnings, w => w.StartsWith("maya has 12 points planned against a capacity of 6.7", StringComparison.Ordinal));
    }

    [Fact]
    public void Propose_NoHistory_UsesTheCurrentCommitmentAndWarns()
    {
        var p = Propose([Story(1, 20, owner: "daniel")], [Story(10, 5)], history: []);

        Assert.Equal(new VelocityRange(20, 20), p.Velocity);
        Assert.Contains(p.Warnings, w => w.StartsWith("No velocity history", StringComparison.Ordinal));
        Assert.DoesNotContain(p.Selected, s => s.WorkItemId == 10);
    }

    [Fact]
    public void Propose_InactiveMember_GetsNoCapacityAndNoItems()
    {
        var roster = new[] { Person("daniel"), Person("maya") with { Active = false } };

        var p = Propose([], [Story(10, 5)], roster);

        Assert.Equal("daniel", Assert.Single(p.Members).DisplayName);
        Assert.Equal("daniel", Assert.Single(p.Selected).OwnerName);
    }
}

public sealed class PlanningPackTests
{
    private static readonly DateTimeOffset T = new(2026, 1, 8, 0, 0, 0, TimeSpan.Zero);

    private static readonly Member[] Roster =
    [
        new("m-daniel", "t", "daniel", "Daniel Park", "daniel@x.com", "d", 1, true),
        new("m-maya", "t", "maya", "Maya Lin", null, "m", 2, true),
    ];

    private static PlanningPack Pack(string? next = "P\\Sprint 2")
    {
        PlannedItem[] selected =
        [
            new(1, "User Story", "Carry", 5, PlanningSource.CarryOver, "m-daniel", "Daniel Park", false),
            new(10, "User Story", "New one", 5, PlanningSource.Backlog, "m-maya", "Maya Lin", true),
            new(11, "Bug", "Another", 3, PlanningSource.Backlog, "m-daniel", "Daniel Park", false),
        ];
        var proposal = new PlanningProposal(new VelocityRange(32, 40), [], selected, [], 13, CommitmentLevel.Under, []);
        return PlanningPack.Prepare(Guid.NewGuid(), "t", T, "P\\Sprint 1", next, true, proposal, "goal", false);
    }

    [Fact]
    public void Prepare_RaisesPlanningPreparedOnce() =>
        Assert.IsType<PlanningPrepared>(Assert.Single(Pack().DomainEvents));

    [Fact]
    public void Approve_AllSelected_MovesEveryItemAndAssignsOnlyTheSuggestedOwner()
    {
        var pack = Pack();

        var changes = pack.Approve("Dana", [], Roster, T).Value;

        Assert.Equal([ChangeOp.SetIteration, ChangeOp.SetIteration, ChangeOp.Assign, ChangeOp.SetIteration], changes.Select(c => c.Op));
        var assign = Assert.Single(changes, c => c.Op == ChangeOp.Assign);
        Assert.Equal((10, "Maya Lin", "maya"), (assign.WorkItemId, assign.Value, assign.OwnerKey));
        Assert.All(changes.Where(c => c.Op == ChangeOp.SetIteration), c => Assert.Equal("P\\Sprint 2", c.Value));
        Assert.Equal(PlanningPackStatus.Approved, pack.Data.Status);
        Assert.Equal("Dana", pack.Data.DecidedBy);
    }

    [Fact]
    public void Approve_ASubset_PlansOnlyThoseItems()
    {
        var pack = Pack();

        var changes = pack.Approve("Dana", [1, 11], Roster, T).Value;

        Assert.Equal([1, 11], changes.Select(c => c.WorkItemId));
        Assert.Equal([1, 11], pack.Data.ApprovedItemIds);
    }

    [Fact]
    public void Approve_AnIdOutsideTheProposal_IsRefused()
    {
        var pack = Pack();

        var result = pack.Approve("Dana", [1, 999], Roster, T);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(PlanningPackStatus.Prepared, pack.Data.Status);
    }

    [Fact]
    public void Approve_NoNextIteration_IsAConflictAndNothingChanges()
    {
        var pack = Pack(next: null);

        var result = pack.Approve("Dana", [], Roster, T);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal(PlanningPackStatus.Prepared, pack.Data.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Approve_BlankApprover_IsInvalid(string approver) =>
        Assert.Equal(ErrorType.Validation, Pack().Approve(approver, [], Roster, T).Error!.Type);

    [Fact]
    public void Approve_Twice_IsAConflict()
    {
        var pack = Pack();
        pack.Approve("Dana", [], Roster, T);

        Assert.Equal(ErrorType.Conflict, pack.Approve("Dana", [], Roster, T).Error!.Type);
    }

    [Fact]
    public void Reject_ThenApprove_IsRefused()
    {
        var pack = Pack();

        Assert.True(pack.Reject("Dana", T).IsSuccess);

        Assert.Equal(ErrorType.Conflict, pack.Approve("Dana", [], Roster, T).Error!.Type);
        Assert.Equal(PlanningPackStatus.Rejected, pack.Data.Status);
    }

    [Fact]
    public void Goal_Fallback_NamesCarryOverAndTheFirstBacklogTitles()
    {
        var goal = PlanningGoal.Fallback(Pack().Data.Proposal);

        Assert.Equal("Finish the carry-over work and deliver New one and Another.", goal);
    }

    [Theory]
    [InlineData("Ship New one and Another, 13 points of work.", true)]
    [InlineData("Ship 99 points of work.", false)]
    [InlineData("Close story 7.", false)]
    public void Goal_IsGrounded_OnlyWhenEveryNumberIsInTheProposal(string goal, bool expected) =>
        Assert.Equal(expected, PlanningGoal.IsGrounded(goal, Pack().Data.Proposal));
}
