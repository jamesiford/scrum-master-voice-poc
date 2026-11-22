using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Domain.Tests.FollowUps;

public sealed class FollowUpTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    private static FollowUp New() => FollowUp.Create(
        Guid.NewGuid(), "t1", Guid.NewGuid(), "m1", FollowUpKind.Followup, "m",
        [new ProposedChange(1, ChangeOp.Comment, "x", "y")], new DateOnly(2026, 1, 8), Now);

    [Fact]
    public void GetApprovedChanges_Pending_IsNotApproved()
    {
        var result = New().GetApprovedChanges();

        Assert.Equal(ErrorType.NotApproved, result.Error!.Type);
    }

    [Fact]
    public void GetApprovedChanges_Approved_ReturnsChangesWithApprover()
    {
        var f = New();
        f.Approve("Dana", Now);

        var changes = f.GetApprovedChanges().Value;

        Assert.Equal("Dana", Assert.Single(changes).ApprovedBy);
    }

    [Fact]
    public void Approve_RaisesFollowUpApprovedOnce()
    {
        var f = New();

        f.Approve("Dana", Now);

        Assert.IsType<FollowUpApproved>(Assert.Single(f.DomainEvents));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Approve_BlankApprover_Fails(string approver)
    {
        var f = New();

        Assert.False(f.Approve(approver, Now).IsSuccess);
        Assert.Equal(FollowUpStatus.Pending, f.Status);
    }

    [Fact]
    public void Approve_AfterReject_Conflicts()
    {
        var f = New();
        f.Reject("Dana", Now);

        var result = f.Approve("Dana", Now);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal(FollowUpStatus.Rejected, f.Status);
    }

    [Fact]
    public void MarkApplied_OnRejected_DoesNothing()
    {
        var f = New();
        f.Reject("Dana", Now);

        f.MarkApplied(Now);

        Assert.Equal(FollowUpStatus.Rejected, f.Status);
    }
}
