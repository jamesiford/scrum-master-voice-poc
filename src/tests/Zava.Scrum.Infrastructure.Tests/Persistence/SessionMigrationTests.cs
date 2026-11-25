using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Tests.Persistence;

/// <summary>One item per session, partitioned by team, and the move from the single legacy document (ADR 0021).</summary>
public sealed class SessionMigrationTests : IDisposable
{
    private static readonly DateTimeOffset Day = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "zava-scrum-tests", Guid.NewGuid().ToString("N"));
    private readonly FileCollectionStore _legacy;
    private readonly FileSessionItemStore _items;

    public SessionMigrationTests()
    {
        var options = Options.Create(new StoreOptions { DataPath = _dir });
        _legacy = new FileCollectionStore(options);
        _items = new FileSessionItemStore(options);
    }

    [Fact]
    public async Task Run_LegacyDocumentWithSessionsOfTwoTeams_MovesEachToItsOwnItemAndRemovesTheDocument()
    {
        var a = Session("team-a");
        var b = Session("team-b");
        await _legacy.WriteAsync("sessions.json", new List<StandupSessionSnapshot> { a.ToSnapshot(), b.ToSnapshot() }, default);

        var moved = await new SessionMigration(_legacy, _items, NullLogger<SessionMigration>.Instance).RunAsync(default);

        Assert.Equal(2, moved);
        Assert.Equal([a.Id], (await _items.ListAsync("team-a", default)).Select(s => s.Id));
        Assert.Equal([b.Id], (await _items.ListAsync("team-b", default)).Select(s => s.Id));
        Assert.Empty(await _legacy.ReadAsync<List<StandupSessionSnapshot>>("sessions.json", () => [], default));
    }

    [Fact]
    public async Task Run_Twice_MovesNothingTheSecondTimeAndLosesNothing()
    {
        var a = Session("team-a");
        await _legacy.WriteAsync("sessions.json", new List<StandupSessionSnapshot> { a.ToSnapshot() }, default);
        var migration = new SessionMigration(_legacy, _items, NullLogger<SessionMigration>.Instance);

        await migration.RunAsync(default);
        var again = await migration.RunAsync(default);

        Assert.Equal(0, again);
        Assert.NotNull(await _items.GetAsync(a.Id, default));
    }

    [Fact]
    public async Task Upsert_ManySessions_AreSeparateItemsSoNoDocumentGrowsWithTheirNumber()
    {
        for (var i = 0; i < 60; i++)
        {
            await _items.UpsertAsync(Session("team-a").ToSnapshot(), default);
        }

        var files = Directory.EnumerateFiles(Path.Combine(_dir, "sessions", "team-a")).Count();

        Assert.Equal(60, files);
        Assert.Equal(60, (await _items.ListAsync("team-a", default)).Count);
        Assert.Empty(await _items.ListAsync("team-b", default));
    }

    [Fact]
    public async Task DeleteAll_RemovesEveryTeamsSessions()
    {
        await _items.UpsertAsync(Session("team-a").ToSnapshot(), default);
        await _items.UpsertAsync(Session("team-b").ToSnapshot(), default);

        await _items.DeleteAllAsync(default);

        Assert.Empty(await _items.ListAsync(null, default));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private static StandupSession Session(string team)
    {
        var brief = new PreBrief(team, "p\\s", "Goal", 6, new Burndown(40, 14, 26, 6, true), [], [], ["risk"], "Summary");
        return StandupSession.Prepare(Guid.NewGuid(), team, Day, brief);
    }
}
