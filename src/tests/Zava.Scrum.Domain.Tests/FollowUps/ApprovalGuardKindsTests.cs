using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Domain.Tests.FollowUps;

public sealed class ApprovalGuardKindsTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);
    private static readonly NewWorkItem Task = new("Task", "Retro action: fix retries", "details", ["retro-action"], "P\\Sprint 2", null);

    public static TheoryData<ChangeOp> NewKinds => new()
    {
        ChangeOp.SetIteration,
        ChangeOp.Assign,
        ChangeOp.CreateWorkItem,
    };

    private static ProposedChange Change(ChangeOp op) => op switch
    {
        ChangeOp.SetIteration => ProposedChange.SetIteration(7, "P\\Sprint 2", "carry-over"),
        ChangeOp.Assign => ProposedChange.Assign(7, "dana@x.com", "fewest points"),
        _ => ProposedChange.Create(Task, "retro"),
    };

    private static FollowUp New(ChangeOp op, CeremonyKind ceremony = CeremonyKind.SprintPlanning) =>
        FollowUp.Create(Guid.NewGuid(), "t1", Guid.NewGuid(), null, FollowUpKind.PlanningPack, "m", [Change(op)], new DateOnly(2026, 1, 8), Now, ceremony, forScrumMaster: true);

    [Theory]
    [MemberData(nameof(NewKinds))]
    public void GetApprovedChanges_Pending_IsRefusedForEveryNewKind(ChangeOp op)
    {
        var result = New(op).GetApprovedChanges();

        Assert.Equal(ErrorType.NotApproved, result.Error!.Type);
    }

    [Theory]
    [MemberData(nameof(NewKinds))]
    public void GetApprovedChanges_Rejected_IsRefusedForEveryNewKind(ChangeOp op)
    {
        var f = New(op);
        f.Reject("Dana", Now);

        Assert.Equal(ErrorType.NotApproved, f.GetApprovedChanges().Error!.Type);
    }

    [Theory]
    [MemberData(nameof(NewKinds))]
    public void GetApprovedChanges_Approved_ReturnsTheChangeForEveryNewKind(ChangeOp op)
    {
        var f = New(op);
        f.Approve("Dana", Now);

        var change = Assert.Single(f.GetApprovedChanges().Value);

        Assert.Equal(op, change.Op);
        Assert.Equal("Dana", change.ApprovedBy);
        Assert.Equal(op == ChangeOp.CreateWorkItem, change.New is not null);
    }

    [Fact]
    public void Attribution_StandupApprovedInTeams_KeepsTheExistingText()
    {
        var f = FollowUp.Create(Guid.NewGuid(), "t1", Guid.NewGuid(), "m1", FollowUpKind.Followup, "m", [new ProposedChange(1, ChangeOp.Comment, "x", "y")], new DateOnly(2026, 1, 8), Now);
        f.Approve("Dana", Now);

        Assert.Equal("Approved by Dana in Teams, proposed by Zava Scrum Agent from the 2026-01-08 standup.", f.GetApprovedChanges().Value[0].Attribution);
    }

    [Theory]
    [InlineData(CeremonyKind.SprintPlanning, "sprint planning")]
    [InlineData(CeremonyKind.SprintReview, "sprint review")]
    [InlineData(CeremonyKind.Retrospective, "retrospective")]
    public void Attribution_PortalApproval_NamesTheChannelAndCeremony(CeremonyKind ceremony, string phrase)
    {
        var f = New(ChangeOp.Assign, ceremony);
        f.Approve("Dana", Now, ApprovalChannel.Portal);

        Assert.Equal($"Approved by Dana in the portal, proposed by Zava Scrum Agent from the 2026-01-08 {phrase}.", f.GetApprovedChanges().Value[0].Attribution);
    }

    [Fact]
    public void Snapshot_SavedBeforeCeremoniesExisted_LoadsAsDailyScrumForTheScrumTeam()
    {
        var snapshot = new FollowUpSnapshot(Guid.NewGuid(), "t1", Guid.NewGuid(), "m1", FollowUpKind.Followup, "m", [], FollowUpStatus.Pending, new DateOnly(2026, 1, 8), Now, null, null, null, null, null);

        var f = FollowUp.Restore(snapshot);

        Assert.Equal(CeremonyKind.DailyScrum, f.Ceremony);
        Assert.False(f.ForScrumMaster);
    }
}
