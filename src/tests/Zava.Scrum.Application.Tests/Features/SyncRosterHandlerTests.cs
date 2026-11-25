using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams.SyncRoster;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Teams;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features;

public sealed class SyncRosterHandlerTests
{
    [Fact]
    public async Task Send_ExistingMember_KeepsLocalEditsButTakesSourceIdentity()
    {
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "P", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "G"), CancellationToken.None);
        var members = new InMemoryMemberRepository();
        await members.ReplaceAsync("t1", [new Member("m1", "t1", "priya", "Old Name", null, "custom", 4, false)], CancellationToken.None);
        var source = new FakeRosterSource([
            new Member("m1", "t1", "priya", "Priya S.", "p@x", "PREE-ya", 1, true),
            new Member("m2", "t1", "marcus", "Marcus", "m@x", "MAR-kus", 2, true)]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddIdentityFakes();
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<IRosterSource>(source);
        await using var sp = services.BuildServiceProvider();

        var result = await sp.GetRequiredService<ISender>().Send(new SyncRosterCommand("t1"));

        var roster = await members.ListAsync("t1", CancellationToken.None);
        Assert.True(result.IsSuccess);
        var priya = roster.Single(m => m.Key == "priya");
        Assert.Equal(("Priya S.", "custom", 4, false), (priya.DisplayName, priya.Pronunciation, priya.SpeakingOrder, priya.Active));
        Assert.Equal(2, roster.Count);
    }

    [Fact]
    public async Task Send_MembersWithAMappedUserPrincipalName_GetTheirEntraObjectIdAndOthersGetNone()
    {
        var priyaId = Guid.NewGuid();
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "P", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "G"), CancellationToken.None);
        var members = new InMemoryMemberRepository();
        var source = new FakeRosterSource([
            new Member("m1", "t1", "priya", "Priya S.", "p@x", "PREE-ya", 1, true),
            new Member("m2", "t1", "cast", "Priya S.", null, "PREE-ya", 2, true),
            new Member("m3", "t1", "gone", "Gone", "gone@x", "Gone", 3, true)]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddIdentityFakes(users: new Dictionary<string, Guid> { ["p@x"] = priyaId });
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<IRosterSource>(source);
        await using var sp = services.BuildServiceProvider();

        await sp.GetRequiredService<ISender>().Send(new SyncRosterCommand("t1"));

        var roster = await members.ListAsync("t1", CancellationToken.None);
        Assert.Equal(priyaId, roster.Single(m => m.Key == "priya").EntraObjectId);
        Assert.Null(roster.Single(m => m.Key == "cast").EntraObjectId);
        Assert.Null(roster.Single(m => m.Key == "gone").EntraObjectId);
    }

    [Fact]
    public async Task Send_ExistingObjectIdThatStillResolvesToTheSameUser_IsKeptAndOneThatNoLongerMatchesIsReplaced()
    {
        var kept = Guid.NewGuid();
        var moved = Guid.NewGuid();
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "P", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "G"), CancellationToken.None);
        var members = new InMemoryMemberRepository();
        await members.ReplaceAsync("t1", [
            new Member("m1", "t1", "priya", "Priya", "p@x", "P", 1, true, EntraObjectId: kept),
            new Member("m2", "t1", "marcus", "Marcus", "m@x", "M", 2, true, EntraObjectId: Guid.NewGuid())], CancellationToken.None);
        var source = new FakeRosterSource([
            new Member("m1", "t1", "priya", "Priya", "p@x", "P", 1, true),
            new Member("m2", "t1", "marcus", "Marcus", "m@x", "M", 2, true)]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddIdentityFakes(users: new Dictionary<string, Guid> { ["p@x"] = kept, ["m@x"] = moved });
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<IRosterSource>(source);
        await using var sp = services.BuildServiceProvider();

        await sp.GetRequiredService<ISender>().Send(new SyncRosterCommand("t1"));

        var roster = await members.ListAsync("t1", CancellationToken.None);
        Assert.Equal(kept, roster.Single(m => m.Key == "priya").EntraObjectId);
        Assert.Equal(moved, roster.Single(m => m.Key == "marcus").EntraObjectId);
    }
}
