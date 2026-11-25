using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Identity;
using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Domain.Tests.Identity;

public sealed class ApprovalAuthorityTests
{
    private static readonly Guid Daniel = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Member DanielMember = new("m-daniel", "t1", "daniel", "Daniel Park", "daniel@zava.example", "Daniel", 1, true, EntraObjectId: Daniel);

    private static ActorIdentity Teams(Guid oid, string name = "Daniel Park", Guid? tenant = null) =>
        Actors.Make(oid.ToString(), name, ActorSource.Teams, (tenant ?? Actors.Tenant).ToString());

    private static ActorIdentity Portal(string name = "Dana") =>
        Actors.Make(Guid.NewGuid().ToString(), name, ActorSource.Portal);

    [Fact]
    public void Authorise_TeamsMemberDecidingTheirOwnFollowUp_IsAllowed()
    {
        var r = ApprovalAuthority.Authorise(Teams(Daniel), Actors.Tenant, ApprovalChannel.Teams, [], false, false, "m-daniel", false, DanielMember);

        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void Authorise_TeamsMemberDecidingSomeoneElsesFollowUp_IsForbidden()
    {
        var r = ApprovalAuthority.Authorise(Teams(Daniel), Actors.Tenant, ApprovalChannel.Teams, [], false, false, "m-maya", false, DanielMember);

        Assert.Equal("identity.not_on_roster", r.Error!.Code);
    }

    [Fact]
    public void Authorise_TeamsMemberDecidingAScrumMasterFollowUp_IsForbidden()
    {
        var r = ApprovalAuthority.Authorise(Teams(Daniel), Actors.Tenant, ApprovalChannel.Teams, [], false, false, null, true, DanielMember);

        Assert.Equal(ErrorType.Forbidden, r.Error!.Type);
    }

    [Fact]
    public void Authorise_TeamsAdministrator_IsAllowedForAnyFollowUp()
    {
        var r = ApprovalAuthority.Authorise(Teams(Guid.NewGuid()), Actors.Tenant, ApprovalChannel.Teams, [], true, false, "m-maya", false, null);

        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void Authorise_ADisplayNameThatMatchesARosterMemberButNoObjectId_IsForbidden()
    {
        // Someone called "Daniel Park" with a different object id: the roster lookup finds no member, so the name decides nothing.
        var impostor = Teams(Guid.NewGuid(), "Daniel Park");

        var r = ApprovalAuthority.Authorise(impostor, Actors.Tenant, ApprovalChannel.Teams, [], false, false, "m-daniel", false, null);

        Assert.Equal("identity.not_on_roster", r.Error!.Code);
    }

    [Fact]
    public void Authorise_AnotherTenant_IsForbiddenEvenForAnAdministrator()
    {
        var foreign = Teams(Daniel, tenant: Guid.NewGuid());

        var r = ApprovalAuthority.Authorise(foreign, Actors.Tenant, ApprovalChannel.Teams, [], true, false, "m-daniel", false, DanielMember);

        Assert.Equal("identity.wrong_tenant", r.Error!.Code);
    }

    [Fact]
    public void Authorise_NoIdentity_IsRejected()
    {
        Assert.Equal("identity.object_id_required", ApprovalAuthority.Authorise(null, Actors.Tenant, ApprovalChannel.Teams, [], true, false, null, false, null).Error!.Code);
    }

    [Theory]
    [InlineData(AppRoles.Approver, true)]
    [InlineData(AppRoles.Admin, false)]
    [InlineData(AppRoles.Reader, false)]
    public void Authorise_Portal_NeedsTheApproverRole(string role, bool allowed)
    {
        var r = ApprovalAuthority.Authorise(Portal(), Actors.Tenant, ApprovalChannel.Portal, [role], false, false, "m-daniel", false, null);

        Assert.Equal(allowed, r.IsSuccess);
    }

    [Fact]
    public void Authorise_PortalWithNoRoles_IsForbidden()
    {
        Assert.Equal("identity.approver_role_required", ApprovalAuthority.Authorise(Portal(), Actors.Tenant, ApprovalChannel.Portal, [], false, false, null, false, null).Error!.Code);
    }

    [Fact]
    public void Authorise_AnIdentityFromTheWrongChannel_IsForbidden()
    {
        var portalIdentityOnTeams = Portal();
        var teamsIdentityOnPortal = Teams(Daniel);

        Assert.Equal("identity.wrong_source", ApprovalAuthority.Authorise(portalIdentityOnTeams, Actors.Tenant, ApprovalChannel.Teams, [], true, false, null, false, null).Error!.Code);
        Assert.Equal("identity.wrong_source", ApprovalAuthority.Authorise(teamsIdentityOnPortal, Actors.Tenant, ApprovalChannel.Portal, [AppRoles.Approver], false, false, null, false, null).Error!.Code);
    }
}

public sealed class ProposalHashTests
{
    private static readonly ProposedChange Comment = new(1, ChangeOp.Comment, "note", "why");

    [Fact]
    public void Of_SameChanges_SameHash()
    {
        Assert.Equal(ProposalHash.Of([Comment]), ProposalHash.Of([new ProposedChange(1, ChangeOp.Comment, "note", "different rationale")]));
    }

    [Fact]
    public void Of_ADifferentValue_ChangesTheHash()
    {
        Assert.NotEqual(ProposalHash.Of([Comment]), ProposalHash.Of([Comment with { Value = "other" }]));
    }

    [Fact]
    public void Of_IsUpperCaseSha256Hex()
    {
        var hash = ProposalHash.Of([Comment]);

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9A-F]+$", hash);
    }
}
