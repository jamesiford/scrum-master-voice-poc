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
}
