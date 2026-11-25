using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Schedules.RunScheduleTick;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Capacity;
using Zava.Scrum.Domain.Schedules;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Tests.Features.Schedules;

/// <summary>The scheduler runs per team and starts standups only within the capacity governor's limits; a ceremony is never dropped silently (ADR 0018).</summary>
public sealed class RunScheduleTickCapacityTests : IAsyncDisposable
{
    // 09:01 UTC on a Thursday: every team below is due to start at 09:00 UTC.
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 9, 1, 0, TimeSpan.Zero);
    private readonly InMemorySessionRepository _sessions = new();
    private readonly InMemoryScheduleRepository _schedules = new();
    private readonly InMemoryTeamRepository _teams = new();
    private readonly RecordingTeamsMessenger _messenger = new();
    private ServiceProvider? _sp;

    public ValueTask DisposeAsync() => _sp?.DisposeAsync() ?? ValueTask.CompletedTask;

    [Fact]
    public async Task Tick_ThreeTeamsDueWithRoomForOne_StartsOneAndLeavesTheOthersWaitingWithAReason()
    {
        var ids = await AddDueTeamsAsync("team-a", "team-b", "team-c");

        var result = await Tick(new CapacityLimits(1, 0, 10));

        Assert.Equal(1, result.Value);
        var started = (await _schedules.ListAsync(default)).Where(s => s.StartedFor is not null).Select(s => s.TeamId).ToList();
        Assert.Equal(["team-a"], started);
        var waiting = (await _schedules.ListAsync(default)).Where(s => s.StartedFor is null).ToList();
        Assert.Equal(2, waiting.Count);
        Assert.All(waiting, s => Assert.Contains("already running", s.WaitingReason, StringComparison.Ordinal));
        Assert.Equal(StandupStatus.Live, (await _sessions.GetAsync(ids["team-a"], default))!.Status);
        Assert.NotEqual(StandupStatus.Live, (await _sessions.GetAsync(ids["team-b"], default))!.Status);
    }

    [Fact]
    public async Task Tick_RunningStandupsCountAgainstTheLimit_AcrossTeams()
    {
        await AddDueTeamsAsync("team-a");
        await AddLiveSessionAsync("team-other");

        var result = await Tick(new CapacityLimits(1, 0, 10));

        Assert.Equal(0, result.Value);
        Assert.Contains("1 of 1", (await _schedules.GetAsync("team-a", default))!.WaitingReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tick_AStandupBlockedPastTheGrace_NotifiesTheTeamsRecapInsteadOfDroppingIt()
    {
        await AddDueTeamsAsync("team-a");
        await AddLiveSessionAsync("team-other");

        await Tick(new CapacityLimits(1, 0, 1));

        var note = Assert.Single(_messenger.Sent);
        Assert.Equal("team-a", note.TeamId);
        Assert.StartsWith("capacity:team-a:", note.DedupeKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tick_PausedTeamsAndTeamsWithTheDailyScrumOff_AreNeverStarted()
    {
        await AddDueTeamsAsync("team-a", "team-paused", "team-off");
        await _teams.SaveAsync((await _teams.GetAsync("team-paused", default))! with { Status = TeamStatus.Paused }, default);
        await _teams.SaveAsync((await _teams.GetAsync("team-off", default))! with { Ceremonies = new CeremoniesEnabled(DailyScrum: false) }, default);

        var result = await Tick(new CapacityLimits(5, 0, 10));

        Assert.Equal(1, result.Value);
        Assert.Equal(["team-a"], (await _schedules.ListAsync(default)).Where(s => s.StartedFor is not null).Select(s => s.TeamId));
    }

    private async Task<Zava.Scrum.Domain.Common.Result<int>> Tick(CapacityLimits limits)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddIdentityFakes();
        services.AddSingleton<ISessionRepository>(_sessions);
        services.AddSingleton<IScheduleRepository>(_schedules);
        services.AddSingleton<ITeamRepository>(_teams);
        services.AddSingleton<ITeamsMessenger>(_messenger);
        services.AddSingleton<IMeetingBridge>(new FakeMeetingBridge());
        services.AddSingleton<IBridgeAccess>(new FakeBridgeAccess());
        services.AddSingleton<ICapacitySettings>(new FakeCapacity(limits));
        services.AddSingleton<IRetroSettings>(new FakeRetroSettings());
        services.AddSingleton<IClock>(new FixedClock(Now));
        services.AddSingleton<IMemberRepository>(new InMemoryMemberRepository());
        services.AddSingleton<IFollowUpRepository>(new InMemoryFollowUpRepository());
        _sp = services.BuildServiceProvider();
        return await _sp.GetRequiredService<ISender>().Send(new RunScheduleTickCommand());
    }

    private async Task<Dictionary<string, Guid>> AddDueTeamsAsync(params string[] teamIds)
    {
        var ids = new Dictionary<string, Guid>();
        foreach (var id in teamIds)
        {
            await _teams.SaveAsync(new Team(id, id, "UTC", 15, new AdoSettings("https://dev.azure.com/zava", id, id, id), new TeamBehaviours(), "goal"), default);
            var session = StandupSession.Prepare(Guid.NewGuid(), id, Now.AddMinutes(-5), null);
            await _sessions.SaveAsync(session, default);
            ids[id] = session.Id;
            await _schedules.SaveAsync(
                new Schedule(id, true, [DayOfWeek.Thursday], new TimeOnly(9, 0), "acs", "https://teams.microsoft.com/l/meetup-join/x",
                    PreparedFor: DateOnly.FromDateTime(Now.UtcDateTime), PreparedSessionId: session.Id),
                default);
        }

        return ids;
    }

    private async Task AddLiveSessionAsync(string teamId)
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), teamId, Now.AddMinutes(-30), null);
        session.Start("acs", "https://teams.microsoft.com/l/meetup-join/y", 15, Now.AddMinutes(-20));
        await _sessions.SaveAsync(session, default);
    }

    private sealed class FakeBridgeAccess : IBridgeAccess
    {
        public string ApiBase => "https://api.test";

        public string Issue(Guid sessionId) => "token";
    }

    private sealed class FakeCapacity(CapacityLimits limits) : ICapacitySettings
    {
        public CapacityLimits Limits { get; } = limits;
    }

    private sealed class InMemoryScheduleRepository : IScheduleRepository
    {
        private readonly Dictionary<string, Schedule> _items = [];

        public Task<Schedule?> GetAsync(string teamId, CancellationToken ct) => Task.FromResult(_items.GetValueOrDefault(teamId));

        public Task<IReadOnlyList<Schedule>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Schedule>>([.. _items.Values]);

        public Task SaveAsync(Schedule schedule, CancellationToken ct)
        {
            _items[schedule.TeamId] = schedule;
            return Task.CompletedTask;
        }
    }
}
