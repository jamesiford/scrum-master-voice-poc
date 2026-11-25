using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Domain.Tests.Identity;

/// <summary>The approver-token rule for delegated writes (ADR 0020).</summary>
public sealed class ApproverAssertionTests
{
    private static readonly Guid Oid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Tid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly ActorIdentity Approver = ActorIdentity.Create(Oid.ToString(), Tid.ToString(), "Priya", null, ActorSource.Portal, "entra-bearer").Value;

    [Fact]
    public void Check_TheApproversOwnToken_Succeeds() => Assert.True(ApproverAssertion.Check(Approver, Oid, Tid).IsSuccess);

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void Check_NoToken_IsRefusedWithTokenRequired(bool hasObjectId, bool hasTenant)
    {
        var result = ApproverAssertion.Check(Approver, hasObjectId ? Oid : null, hasTenant ? Tid : null);

        Assert.Equal(IdentityErrors.DelegatedTokenRequired, result.Error);
    }

    [Fact]
    public void Check_AnotherPersonsToken_IsRefused() =>
        Assert.Equal(IdentityErrors.DelegatedTokenNotApprover, ApproverAssertion.Check(Approver, Guid.NewGuid(), Tid).Error);

    [Fact]
    public void Check_TheSameObjectIdInAnotherTenant_IsRefused() =>
        Assert.Equal(IdentityErrors.DelegatedTokenNotApprover, ApproverAssertion.Check(Approver, Oid, Guid.NewGuid()).Error);
}
