using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Domain.Tests.Teams;

/// <summary>Destructive scope must be explicit in both persisted data and configuration.</summary>
public sealed class DemoResetScopeTests
{
    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Check_UnmarkedForeignOrUnlistedTeam_RefusesWholeScope(bool marked, bool sameTenant, bool listed)
    {
        var tenant = Guid.NewGuid();
        var demo = Team("demo", tenant);
        var other = Team("other", sameTenant ? tenant : Guid.NewGuid()) with { IsDemo = marked };
        string[] allowed = listed ? ["demo", "other"] : ["demo"];

        var result = DemoResetScope.Check(tenant, tenant, allowed, [demo, other]);

        Assert.Equal("demo.reset_scope_refused", result.Error!.Code);
    }

    [Fact]
    public void Check_ExplicitDemoScopeWithCorrectCaller_Allows()
    {
        var tenant = Guid.NewGuid();

        Assert.True(DemoResetScope.Check(tenant, tenant, ["demo"], [Team("demo", tenant)]).IsSuccess);
        Assert.False(DemoResetScope.Check(tenant, Guid.NewGuid(), ["demo"], [Team("demo", tenant)]).IsSuccess);
        Assert.False(DemoResetScope.Check(Guid.Empty, Guid.Empty, ["demo"], [Team("demo", Guid.Empty)]).IsSuccess);
    }

    [Fact]
    public void CheckIsolation_UnselectedRealTeamSharesLegacyOrganisationAliasAndArea_Refuses()
    {
        var tenant = Guid.NewGuid();
        var target = Team("demo", tenant) with { Ado = new AdoSettings("https://dev.azure.com/demo", "Project", "Demo", "Project\\Area") };
        var real = target with
        {
            Id = "real",
            IsDemo = false,
            Ado = new AdoSettings("https://demo.visualstudio.com", "Project", "Real", "Project")
        };

        Assert.False(DemoResetScope.CheckIsolation(target, [target], [target, real]).IsSuccess);
        Assert.True(DemoResetScope.CheckIsolation(target, [target], [target, real with { Ado = real.Ado with { Project = "Another" } }]).IsSuccess);
    }

    private static Team Team(string id, Guid tenant) =>
        new(id, "Demo", "UTC", 15, new AdoSettings("org", "project", "team", "area"), new TeamBehaviours(), "goal", IsDemo: true, DemoTenantId: tenant);
}
