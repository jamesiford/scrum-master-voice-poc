using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Application.Features.Teams.BindConversation;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Tests.Features.Teams;

/// <summary>Team resolution for each entry point, and binding a Teams conversation to a team (ADR 0018).</summary>
public sealed class TeamResolutionTests : IAsyncDisposable
{
    private const string Channel = "19:abc@thread.tacv2";
    private static readonly Guid Lead = Guid.Parse("a0000000-0000-0000-0000-00000000000a");
    private static readonly Guid Member = Guid.Parse("a0000000-0000-0000-0000-00000000000b");
    private static readonly Guid Stranger = Guid.Parse("c0000000-0000-0000-0000-00000000000c");

    private readonly InMemoryTeamRepository _teams = new();
    private readonly InMemoryMembershipRepository _memberships = new();
    private readonly InMemorySessionRepository _sessions = new();
    private readonly ServiceProvider _sp;

    public TeamResolutionTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddIdentityFakes();
        services.AddSingleton<ITeamRepository>(_teams);
        services.AddSingleton<ITeamMembershipRepository>(_memberships);
        services.AddSingleton<ISessionRepository>(_sessions);
        _sp = services.BuildServiceProvider();
    }

    private ISender Sender => _sp.GetRequiredService<ISender>();

    private ITeamResolver Resolver => _sp.GetRequiredService<ITeamResolver>();

    public ValueTask DisposeAsync() => _sp.DisposeAsync();

    [Fact]
    public async Task Bind_ATeamLead_BindsTheConversationAndTheBotThenResolvesIt()
    {
        await AddTeamAsync("team-a", TeamStatus.Active);
        await _memberships.UpsertAsync(new TeamMembership("team-a", Lead, TeamRole.TeamLead), default);

        var bound = await Sender.Send(Bind(Lead, "team-a"));
        var resolved = await Resolver.ForConversationAsync(Who.Tenant.ToString(), Channel + ";messageid=123", default);

        Assert.True(bound.IsSuccess);
        Assert.Equal("team-a", resolved.Value.Id);
    }

    [Fact]
    public async Task Bind_AMemberOrAStranger_IsRefusedAndNothingIsBound()
    {
        await AddTeamAsync("team-a", TeamStatus.Active);
        await _memberships.UpsertAsync(new TeamMembership("team-a", Member, TeamRole.Member), default);

        var asMember = await Sender.Send(Bind(Member, "team-a"));
        var asStranger = await Sender.Send(Bind(Stranger, "team-a"));

        Assert.Equal(ErrorType.Forbidden, asMember.Error!.Type);
        Assert.Equal(ErrorType.NotFound, asStranger.Error!.Type);
        Assert.Empty((await _teams.GetAsync("team-a", default))!.Bindings);
    }

    [Fact]
    public async Task Bind_AnAdminMayBindAnyTeam_AndAConversationMovesToTheNewTeam()
    {
        await AddTeamAsync("team-a", TeamStatus.Active);
        await AddTeamAsync("team-b", TeamStatus.Active);
        await Sender.Send(Bind(Who.AdminId, "team-a", admin: true));

        var moved = await Sender.Send(Bind(Who.AdminId, "team-b", admin: true));

        Assert.True(moved.IsSuccess);
        Assert.Empty((await _teams.GetAsync("team-a", default))!.Bindings);
        Assert.Equal("team-b", (await Resolver.ForConversationAsync(Who.Tenant.ToString(), Channel, default)).Value.Id);
    }

    [Fact]
    public async Task Bind_FromAnotherTenant_IsRefused()
    {
        await AddTeamAsync("team-a", TeamStatus.Active);

        var result = await Sender.Send(Bind(Who.AdminId, "team-a", admin: true) with { TenantId = Guid.NewGuid().ToString() });

        Assert.False(result.IsSuccess);
        Assert.Empty((await _teams.GetAsync("team-a", default))!.Bindings);
    }

    [Fact]
    public async Task ForConversation_AnUnboundOrOtherTenantConversation_ResolvesToNoTeam()
    {
        await AddTeamAsync("team-a", TeamStatus.Active);

        var unbound = await Resolver.ForConversationAsync(Who.Tenant.ToString(), "19:other@thread.tacv2", default);
        var otherTenant = await Resolver.ForConversationAsync(Guid.NewGuid().ToString(), Channel, default);

        Assert.Equal(TeamErrors.Unbound.Code, unbound.Error!.Code);
        Assert.Equal(TeamErrors.NoTeam.Code, otherTenant.Error!.Code);
    }

    [Fact]
    public async Task ForConversation_APausedTeam_DoesNotAnswer()
    {
        await AddTeamAsync("team-a", TeamStatus.Paused, Channel);

        Assert.False((await Resolver.ForConversationAsync(Who.Tenant.ToString(), Channel, default)).IsSuccess);
    }

    [Fact]
    public async Task ForUser_OneTeamResolvesItself_TwoTeamsAskWhichAndNeverGuess_AChosenTeamMustBeTheirs()
    {
        await AddTeamAsync("team-a", TeamStatus.Active);
        await AddTeamAsync("team-b", TeamStatus.Active);
        await _memberships.UpsertAsync(new TeamMembership("team-a", Member, TeamRole.Member), default);
        Assert.Equal("team-a", (await Resolver.ForUserAsync(Member, null, default)).Value.Id);

        await _memberships.UpsertAsync(new TeamMembership("team-b", Member, TeamRole.Member), default);
        var ambiguous = await Resolver.ForUserAsync(Member, null, default);
        var chosen = await Resolver.ForUserAsync(Member, "team-b", default);
        var notTheirs = await Resolver.ForUserAsync(Lead, "team-b", default);

        Assert.Equal(TeamErrors.Ambiguous([]).Code, ambiguous.Error!.Code);
        Assert.Equal("team-b", chosen.Value.Id);
        Assert.Equal(ErrorType.NotFound, notTheirs.Error!.Type);
    }

    [Fact]
    public async Task ForSession_ResolvesTheTeamOnTheSession_AndAnUnknownSessionResolvesToNone()
    {
        await AddTeamAsync("team-b", TeamStatus.Active);
        var session = StandupSession.Prepare(Guid.NewGuid(), "team-b", new DateTimeOffset(2026, 1, 8, 9, 0, 0, TimeSpan.Zero), null);
        await _sessions.SaveAsync(session, default);

        Assert.Equal("team-b", (await Resolver.ForSessionAsync(session.Id, default)).Value.Id);
        Assert.False((await Resolver.ForSessionAsync(Guid.NewGuid(), default)).IsSuccess);
    }

    [Fact]
    public async Task ForCaller_ARequestThatNamesNoTeam_IsRefused()
    {
        var result = await Resolver.ForCallerAsync(new CallerContext(Who.AdminId, true), null, TeamPermission.Read, default);

        Assert.Equal(TeamErrors.NoTeam.Code, result.Error!.Code);
    }

    private static BindTeamsConversationCommand Bind(Guid caller, string teamId, bool admin = false) =>
        new(new CallerContext(caller, admin), teamId, Who.Tenant.ToString(), null, Channel, true, BindingPurpose.Recap);

    private Task AddTeamAsync(string id, TeamStatus status, string? boundChannel = null) =>
        _teams.SaveAsync(
            new Team(id, id, "UTC", 15, new AdoSettings("https://dev.azure.com/zava", id, id, id), new TeamBehaviours(), "goal", Status: status,
                TeamsBindings: boundChannel is null ? null : [new TeamsBinding(Who.Tenant.ToString(), null, boundChannel, null, BindingPurpose.Recap)]),
            default);
}
