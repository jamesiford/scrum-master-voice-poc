using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.WorkSignals;

namespace Zava.Scrum.Domain.Tests.WorkSignals;

public sealed class WorkSignalConsentTests
{
    private static readonly Guid Owner = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 0, 0, 0, TimeSpan.Zero);
    private static Team Team => new("t1", "Team", "UTC", 15, new("o", "p", "t", "a"), new(), "", WorkSignalsEnabled: true);

    [Fact]
    public void Connect_TeamLead_ReturnsNamedOwnerWithoutCredentials()
    {
        var result = WorkSignalConsent.Connect("t1", new("t1", Owner, TeamRole.TeamLead), Tenant, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(Owner, result.Value.OwnerObjectId);
        Assert.Equal(Now, result.Value.ConnectedAt);
    }

    [Theory]
    [InlineData(TeamRole.Reader, "t1")]
    [InlineData(TeamRole.Member, "t1")]
    [InlineData(TeamRole.TeamLead, "other")]
    public void Connect_WrongRoleOrTeam_Refuses(TeamRole role, string teamId)
    {
        var result = WorkSignalConsent.Connect("t1", new(teamId, Owner, role), Tenant, Now);

        Assert.Equal("signals.owner_required", result.Error!.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Revoke_OwnerOrAdministrator_IsIdempotentAndBlocksReads(bool admin)
    {
        var consent = WorkSignalConsent.Connect("t1", new("t1", Owner, TeamRole.TeamLead), Tenant, Now).Value;
        var caller = admin ? Guid.NewGuid() : Owner;

        var revoked = consent.Revoke(caller, admin, Now.AddMinutes(1)).Value;
        var repeated = revoked.Revoke(caller, admin, Now.AddMinutes(2)).Value;
        var access = WorkSignalAccess.Authorise(Team, revoked, new("t1", Owner, TeamRole.TeamLead), Owner, Tenant, Tenant);

        Assert.Equal(Now.AddMinutes(1), repeated.RevokedAt);
        Assert.Equal("signals.unavailable", access.Error!.Code);
        Assert.Null(consent.RevokedAt);
    }

    [Fact]
    public void Revoke_UnrelatedCaller_Refuses()
    {
        var consent = new WorkSignalConsent("t1", Owner, Tenant, Now);

        var result = consent.Revoke(Guid.NewGuid(), false, Now);

        Assert.Equal("signals.revoke_forbidden", result.Error!.Code);
    }

    [Fact]
    public void Authorise_PersistedEmptyOwner_RefusesEvenMatchingEmptyAssertion()
    {
        var consent = new WorkSignalConsent("t1", Guid.Empty, Tenant, Now);

        var result = WorkSignalAccess.Authorise(Team, consent, new("t1", Guid.Empty, TeamRole.TeamLead), Guid.Empty, Tenant, Tenant);

        Assert.Equal("signals.unavailable", result.Error!.Code);
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("off")]
    [InlineData("wrong-team")]
    [InlineData("wrong-owner")]
    [InlineData("wrong-tenant")]
    [InlineData("removed")]
    [InlineData("demoted")]
    [InlineData("missing-token")]
    [InlineData("archived")]
    public void Authorise_ConnectionState_FailsClosedExceptExactOwner(string state)
    {
        var team = state == "off" ? Team with { WorkSignalsEnabled = false }
            : state == "archived" ? Team with { Status = TeamStatus.Archived } : Team;
        var consent = new WorkSignalConsent(state == "wrong-team" ? "other" : "t1", Owner, Tenant, Now);
        var membership = state == "removed" ? null
            : new TeamMembership("t1", Owner, state == "demoted" ? TeamRole.Member : TeamRole.TeamLead);
        Guid? assertion = state == "missing-token" ? null : state == "wrong-owner" ? Guid.NewGuid() : Owner;

        var result = WorkSignalAccess.Authorise(team, consent, membership, assertion,
            state == "wrong-tenant" ? Guid.NewGuid() : Tenant, Tenant);

        Assert.Equal(state == "valid", result.IsSuccess);
    }
}
