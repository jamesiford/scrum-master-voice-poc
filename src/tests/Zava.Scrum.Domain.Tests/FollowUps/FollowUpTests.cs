using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Domain.Tests.FollowUps;

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
        f.Approve(Actors.Dana, Now);

        var changes = f.GetApprovedChanges().Value;

        Assert.Equal("Dana", Assert.Single(changes).ApprovedBy);
    }

    [Fact]
    public void Approve_RaisesFollowUpApprovedOnce()
    {
        var f = New();

        f.Approve(Actors.Dana, Now);

        Assert.IsType<FollowUpApproved>(Assert.Single(f.DomainEvents));
    }

    [Fact]
    public void Approve_NoIdentity_Fails()
    {
        var f = New();

        Assert.Equal("followup.approver_required", f.Approve(null, Now).Error!.Code);
        Assert.Equal(FollowUpStatus.Pending, f.Status);
    }

    [Fact]
    public void Reject_NoIdentity_Fails()
    {
        var f = New();

        Assert.False(f.Reject(null, Now).IsSuccess);
        Assert.Equal(FollowUpStatus.Pending, f.Status);
    }

    [Fact]
    public void Approve_RecordsTheIdentityAndDerivesTheDisplayName()
    {
        var f = New();

        f.Approve(Actors.Dana, Now);

        Assert.Equal(Actors.Dana, f.ApprovedByIdentity);
        Assert.Equal("Dana", f.ApprovedBy);
        Assert.Equal("Dana", f.ToSnapshot().ApprovedBy);
    }

    [Fact]
    public void Restore_LegacyApprovalWithNoIdentity_ShowsUnverifiedAndIsNeverWritten()
    {
        var legacy = new FollowUpSnapshot(Guid.NewGuid(), "t1", Guid.NewGuid(), "m1", FollowUpKind.Followup, "m", [new ProposedChange(1, ChangeOp.Comment, "x", "y")], FollowUpStatus.Approved, new DateOnly(2026, 1, 8), Now, "Dana", null, Now, null, null);

        var f = FollowUp.Restore(legacy);

        Assert.Equal("unverified (legacy)", f.ApprovedBy);
        Assert.Equal("identity.legacy_unverified", f.GetApprovedChanges().Error!.Code);
        Assert.Equal(ErrorType.Conflict, f.Approve(Actors.Dana, Now).Error!.Type);
    }

    [Fact]
    public void Restore_LegacySnapshot_GetsACorrelationIdDerivedFromTheSession()
    {
        var session = Guid.NewGuid();
        var snapshot = new FollowUpSnapshot(Guid.NewGuid(), "t1", session, "m1", FollowUpKind.Followup, "m", [], FollowUpStatus.Pending, new DateOnly(2026, 1, 8), Now, null, null, null, null, null);

        Assert.Equal(Correlation.ForLegacySession(session), FollowUp.Restore(snapshot).CorrelationId);
    }

    [Fact]
    public void GetApprovedChanges_CarriesTheCorrelationTagAtTheEndOfTheAttribution()
    {
        var f = FollowUp.Create(Guid.NewGuid(), "t1", Guid.NewGuid(), "m1", FollowUpKind.Followup, "m", [new ProposedChange(1, ChangeOp.Comment, "x", "y")], new DateOnly(2026, 1, 8), Now, correlationId: "4bf92f3577b34da6a3ce929d0e0e4736");
        f.Approve(Actors.Dana, Now);

        Assert.EndsWith("Correlation: zsa-4bf92f3577b3", f.GetApprovedChanges().Value[0].Attribution);
    }

    [Fact]
    public void Approve_AfterReject_Conflicts()
    {
        var f = New();
        f.Reject(Actors.Dana, Now);

        var result = f.Approve(Actors.Dana, Now);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal(FollowUpStatus.Rejected, f.Status);
    }

    [Fact]
    public void MarkApplied_OnRejected_DoesNothing()
    {
        var f = New();
        f.Reject(Actors.Dana, Now);

        f.MarkApplied(Now);

        Assert.Equal(FollowUpStatus.Rejected, f.Status);
    }
}
