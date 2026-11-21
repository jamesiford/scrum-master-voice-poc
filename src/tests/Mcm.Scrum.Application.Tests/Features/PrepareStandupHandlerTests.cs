using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Standups.PrepareStandup;
using Mcm.Scrum.Application.Tests.TestDoubles;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Mcm.Scrum.Application.Tests.Features;

public sealed class PrepareStandupHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);
    private static readonly Team Team = new("t1", "Payments", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "Goal");

    private static readonly Member[] Roster = [new("m1", "t1", "priya", "Priya", null, "PREE-ya", 1, true)];

    private static WorkItem Story(int id, double daysInState) =>
        new(new WorkItemId(id), "Retry", "User Story", "Active", "a@x", "priya", Now.AddDays(-daysInState), Now, 5, null, null, []);

    private static SprintSnapshot Sprint() => new("p\\s", Now.AddDays(-4), Now.AddDays(6), [Story(4521, 3)]);

    private static async Task<(Result<Guid> Result, InMemorySessionRepository Sessions)> RunAsync(
        IReadOnlyList<Member> roster,
        Result<SprintSnapshot> sprint,
        BacklogRiskSuggestion? suggestion)
    {
        var sessions = new InMemorySessionRepository();
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(Team, CancellationToken.None);
        var members = new InMemoryMemberRepository();
        await members.ReplaceAsync("t1", roster, CancellationToken.None);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<ISessionRepository>(sessions);
        services.AddSingleton<IWorkItemReader>(new FakeWorkItemReader(sprint));
        services.AddSingleton<IBacklogRiskAgent>(new FakeBacklogRiskAgent(suggestion));
        services.AddSingleton<IClock>(new FixedClock(Now));
        services.AddSingleton<ITeamsMessenger>(new RecordingTeamsMessenger());
        await using var sp = services.BuildServiceProvider();
        var result = await sp.GetRequiredService<ISender>().Send(new PrepareStandupCommand("t1"));
        return (result, sessions);
    }

    [Fact]
    public async Task Send_GroundedSuggestion_IsStoredOnTheSession()
    {
        var suggestion = new BacklogRiskSuggestion("Team summary.", new Dictionary<string, string> { ["m1"] = "Story 4521 has been Active three days. On track?" });

        var (result, sessions) = await RunAsync(Roster, Result<SprintSnapshot>.Success(Sprint()), suggestion);

        var brief = (await sessions.GetAsync(result.Value, CancellationToken.None))!.PreBrief!;
        Assert.Equal("Story 4521 has been Active three days. On track?", brief.Members.Single().SuggestedQuestion);
        Assert.Equal("Team summary.", brief.Summary);
    }

    [Fact]
    public async Task Send_SuggestionWithInventedId_FallsBackToDeterministicQuestion()
    {
        var suggestion = new BacklogRiskSuggestion("Summary mentions story 9999.", new Dictionary<string, string> { ["m1"] = "Is story 9999 done?" });

        var (result, sessions) = await RunAsync(Roster, Result<SprintSnapshot>.Success(Sprint()), suggestion);

        var brief = (await sessions.GetAsync(result.Value, CancellationToken.None))!.PreBrief!;
        Assert.DoesNotContain("9999", brief.Members.Single().SuggestedQuestion, StringComparison.Ordinal);
        Assert.DoesNotContain("9999", brief.Summary, StringComparison.Ordinal);
        Assert.Contains("4521", brief.Members.Single().SuggestedQuestion, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_AgentUnavailable_StillPreparesWithDeterministicQuestions()
    {
        var (result, sessions) = await RunAsync(Roster, Result<SprintSnapshot>.Success(Sprint()), null);

        Assert.True(result.IsSuccess);
        Assert.Single(await sessions.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Send_NoActiveMembers_ReturnsRosterEmptyConflict()
    {
        var (result, sessions) = await RunAsync([], Result<SprintSnapshot>.Success(Sprint()), null);

        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Empty(await sessions.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Send_AdoFails_PropagatesErrorAndStoresNothing()
    {
        var failure = Result<SprintSnapshot>.Failure(Error.ExternalRejected("ado.unreachable", "down"));

        var (result, sessions) = await RunAsync(Roster, failure, null);

        Assert.Equal("ado.unreachable", result.Error?.Code);
        Assert.Empty(await sessions.ListAsync(CancellationToken.None));
    }
}
