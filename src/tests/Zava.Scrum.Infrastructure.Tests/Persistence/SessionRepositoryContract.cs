using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Infrastructure.Tests.Persistence;

/// <summary>Shared contract every <see cref="ISessionRepository"/> implementation must pass (SOLID: L).</summary>
public abstract class SessionRepositoryContract
{
    private static readonly DateTimeOffset Day1 = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

    protected abstract ISessionRepository CreateRepository();

    [Fact]
    public async Task SaveAsync_ThenGetAsync_RoundTripsSessionWithPreBrief()
    {
        var repository = CreateRepository();
        var brief = new PreBrief(
            "t1",
            "p\\s",
            "Goal",
            6,
            new Burndown(40, 14, 26, 6, true),
            [new MemberBrief("m1", "Priya", [new BriefItem(new WorkItemId(4521), "Retry", "User Story", "Active", 3, [Flag.Stale])], "Q?")],
            [],
            ["risk"],
            "Summary");
        var session = StandupSession.Prepare(Guid.NewGuid(), "t1", Day1, brief);

        await repository.SaveAsync(session, CancellationToken.None);
        var loaded = await repository.GetAsync(session.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal((session.TeamId, session.CreatedAt, StandupStatus.Prepared), (loaded.TeamId, loaded.CreatedAt, loaded.Status));
        var item = loaded.PreBrief!.Members.Single().Items.Single();
        Assert.Equal((4521, Flag.Stale), (item.Id.Value, item.Flags.Single()));
        Assert.True(loaded.PreBrief.Burndown.AtRisk);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        var repository = CreateRepository();

        Assert.Null(await repository.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_MultipleSessions_ReturnsNewestFirst()
    {
        var repository = CreateRepository();
        var older = StandupSession.Prepare(Guid.NewGuid(), "t1", Day1, null);
        var newer = StandupSession.Prepare(Guid.NewGuid(), "t1", Day1.AddDays(1), null);
        await repository.SaveAsync(older, CancellationToken.None);
        await repository.SaveAsync(newer, CancellationToken.None);

        var all = await repository.ListAsync(CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], all.Select(s => s.Id));
    }
}
