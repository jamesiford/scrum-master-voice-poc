using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Calendars.ApproveSprintCalendar;
using Zava.Scrum.Application.Features.Calendars.GetSprintCalendar;
using Zava.Scrum.Application.Features.Calendars.ProposeSprintCalendar;
using Zava.Scrum.Application.Features.Calendars.RejectSprintCalendar;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Calendars;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Schedules;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features.Calendars;

public sealed class SprintCalendarFlowTests : IAsyncDisposable
{
    private static readonly DateTimeOffset T = StandupHarness.Now;
    private readonly RecordingTeamsMessenger _messenger = new();
    private readonly FakeCalendarWriter _writer = new();
    private readonly InMemoryScheduleRepository _schedules = new();
    private readonly InMemorySprintCalendarRepository _calendars = new();
    private ServiceProvider _sp = null!;

    private ISender Sender => _sp.GetRequiredService<ISender>();

    // Mon 2026-12-07 to Fri 2026-12-11 in a UTC team: five standups, planning on day one, the retrospective on day five.
    private async Task StartAsync(bool graph = true, bool hasReview = false)
    {
        _writer.Enabled = graph;
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "Payments", "UTC", 15, new AdoSettings("o", "P", "t", "a"), new TeamBehaviours(), "Goal"), default);
        var members = new InMemoryMemberRepository();
        await members.ReplaceAsync("t1", [new Member("m1", "t1", "daniel", "Daniel Park", "daniel@x.com", "Daniel", 1, true), new Member("m2", "t1", "maya", "Maya Lin", "maya@x.com", "Maya", 2, true), new Member("m3", "t1", "admin", "Admin", null, "Admin", 3, true)], default);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IMemberRepository>(members);
        services.AddSingleton<IScheduleRepository>(_schedules);
        services.AddSingleton<ISprintCalendarRepository>(_calendars);
        services.AddSingleton<ITeamsMessenger>(_messenger);
        services.AddSingleton<ICalendarWriter>(_writer);
        services.AddSingleton<ICalendarSettings>(new FakeCalendarSettings(hasReview));
        services.AddSingleton<IIterationReader>(new FakeIterationReader(null, new IterationInfo("P\\Sprint 43", new DateOnly(2026, 12, 7), new DateOnly(2026, 12, 11))));
        services.AddSingleton<IClock>(new FixedClock(T));
        _sp = services.BuildServiceProvider();
    }

    private async Task<Guid> ProposeAsync() => (await Sender.Send(new ProposeSprintCalendarCommand("t1"))).Value;

    [Fact]
    public async Task Get_ReturnsThePlanAndAnIcsFileAndStoresNothing()
    {
        await StartAsync();

        var dto = (await Sender.Send(new GetSprintCalendarQuery("t1"))).Value;

        Assert.Equal(7, dto.Meetings.Count);
        Assert.Contains("BEGIN:VCALENDAR", dto.Ics, StringComparison.Ordinal);
        Assert.Empty(await _calendars.ListAsync("t1", default));
        Assert.Empty(_messenger.Sent);
    }

    [Fact]
    public async Task Get_NoIterationWithDates_IsNotFound()
    {
        await StartAsync();
        var dto = await new ServiceCollection().AddLogging().AddApplication()
            .AddSingleton<ITeamRepository>(await TeamsAsync())
            .AddSingleton<IScheduleRepository>(_schedules)
            .AddSingleton<IIterationReader>(new FakeIterationReader(null))
            .AddSingleton<ICalendarSettings>(new FakeCalendarSettings(false))
            .AddSingleton<IClock>(new FixedClock(T))
            .BuildServiceProvider().GetRequiredService<ISender>().Send(new GetSprintCalendarQuery("t1"));

        Assert.Equal(ErrorType.NotFound, dto.Error!.Type);
    }

    [Fact]
    public async Task Propose_StoresTheCalendarAndSendsOneScrumMasterCardWithoutCreatingAnything()
    {
        await StartAsync();

        var id = await ProposeAsync();

        Assert.Equal(SprintCalendarStatus.Proposed, (await _calendars.GetAsync(id, default))!.Data.Status);
        var card = Assert.Single(_messenger.Sent);
        Assert.Equal(CardKind.SprintCalendar, card.Kind);
        Assert.StartsWith("Proposed sprint calendar: Sprint 43", card.Title, StringComparison.Ordinal);
        Assert.Equal(id, card.FollowUpId);
        Assert.Empty(_writer.Created);
    }

    [Fact]
    public async Task Approve_CreatesEveryMeetingInvitesTheRosterAndStoresJoinUrlsOnTheSchedule()
    {
        await StartAsync();
        var id = await ProposeAsync();

        var result = await Sender.Send(new ApproveSprintCalendarCommand(id, "Sam"));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, _writer.Created.Count);
        Assert.All(_writer.Created, c => Assert.Equal(["daniel@x.com", "maya@x.com"], c.Attendees));
        var calendar = (await _calendars.GetAsync(id, default))!.Data;
        Assert.Equal(SprintCalendarStatus.Approved, calendar.Status);
        Assert.All(calendar.Meetings, m => Assert.NotNull(m.JoinUrl));
        var schedule = (await _schedules.GetAsync("t1", default))!;
        Assert.True(schedule.Enabled);
        Assert.Equal("acs", schedule.Mode);
        Assert.Equal(5, schedule.Meetings!.Count);
        Assert.NotNull(schedule.JoinUrlFor(new DateOnly(2026, 12, 9)));
        Assert.Contains(_messenger.Updated, m => m.Facts.Any(f => f.Label == "Status" && f.Value.StartsWith("Approved by Sam", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Approve_WithoutANamedApprover_CreatesNothing()
    {
        await StartAsync();
        var id = await ProposeAsync();

        var result = await Sender.Send(new ApproveSprintCalendarCommand(id, " "));

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Empty(_writer.Created);
    }

    [Fact]
    public async Task Approve_GraphNotConfigured_CreatesNothingAndPointsToTheIcsFallback()
    {
        await StartAsync(graph: false);
        var id = await ProposeAsync();

        var result = await Sender.Send(new ApproveSprintCalendarCommand(id, "Sam"));

        Assert.Equal("calendar.not_configured", result.Error!.Code);
        Assert.Contains(".ics", result.Error.Message, StringComparison.Ordinal);
        Assert.Empty(_writer.Created);
        Assert.Equal(SprintCalendarStatus.Proposed, (await _calendars.GetAsync(id, default))!.Data.Status);
    }

    [Fact]
    public async Task Reject_CreatesNothingAndALaterApprovalIsAConflict()
    {
        await StartAsync();
        var id = await ProposeAsync();

        await Sender.Send(new RejectSprintCalendarCommand(id, "Sam"));
        var approve = await Sender.Send(new ApproveSprintCalendarCommand(id, "Sam"));

        Assert.Empty(_writer.Created);
        Assert.Equal(ErrorType.Conflict, approve.Error!.Type);
        Assert.Null(await _schedules.GetAsync("t1", default));
    }

    [Fact]
    public async Task Approve_GraphFailsPartWay_KeepsWhatWasCreatedAndARetryCreatesOnlyTheRest()
    {
        await StartAsync();
        var id = await ProposeAsync();
        _writer.FailAfter = 3;

        var first = await Sender.Send(new ApproveSprintCalendarCommand(id, "Sam"));

        Assert.False(first.IsSuccess);
        Assert.Equal(3, _writer.Created.Count);
        Assert.Equal(SprintCalendarStatus.Approving, (await _calendars.GetAsync(id, default))!.Data.Status);

        _writer.FailAfter = null;
        var retry = await Sender.Send(new ApproveSprintCalendarCommand(id, "Sam"));

        Assert.True(retry.IsSuccess);
        Assert.Equal(7, _writer.Created.Count);
        Assert.Equal(7, _writer.Created.Select(c => c.TransactionId).Distinct().Count());
        Assert.Equal(SprintCalendarStatus.Approved, (await _calendars.GetAsync(id, default))!.Data.Status);
    }

    [Fact]
    public async Task Approve_WithAReview_CreatesTheReviewToo()
    {
        await StartAsync(hasReview: true);
        var id = await ProposeAsync();

        await Sender.Send(new ApproveSprintCalendarCommand(id, "Sam"));

        Assert.Contains(_writer.Created, c => c.Subject.EndsWith("Sprint review", StringComparison.Ordinal));
        Assert.Equal(8, _writer.Created.Count);
    }

    [Fact]
    public async Task UpdateSchedule_AcsWithoutALink_NeedsAnApprovedCalendar()
    {
        await StartAsync();
        var save = new Zava.Scrum.Application.Features.Schedules.UpdateSchedule.UpdateScheduleCommand("t1", true, ["Monday"], "09:30", "acs", null);

        Assert.Equal("schedule.join_url_required", (await Sender.Send(save)).Error!.Code);

        await Sender.Send(new ApproveSprintCalendarCommand(await ProposeAsync(), "Sam"));

        Assert.True((await Sender.Send(save)).IsSuccess);
        Assert.Equal(5, (await _schedules.GetAsync("t1", default))!.Meetings!.Count);
    }

    private static async Task<ITeamRepository> TeamsAsync()
    {
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "Payments", "UTC", 15, new AdoSettings("o", "P", "t", "a"), new TeamBehaviours(), "Goal"), default);
        return teams;
    }

    public ValueTask DisposeAsync() => _sp.DisposeAsync();
}

internal sealed class FakeCalendarWriter : ICalendarWriter
{
    public bool Enabled { get; set; } = true;

    public int? FailAfter { get; set; }

    public List<MeetingInvite> Created { get; } = [];

    public Task<Result<CreatedMeeting>> CreateMeetingAsync(MeetingInvite invite, CancellationToken ct)
    {
        if (FailAfter is { } n && Created.Count >= n)
        {
            return Task.FromResult(Result<CreatedMeeting>.Failure(Error.ExternalRejected("calendar.graph_rejected", "Graph said no.")));
        }

        Created.Add(invite);
        return Task.FromResult(Result<CreatedMeeting>.Success(new CreatedMeeting($"event-{Created.Count}", $"https://teams.microsoft.com/l/meetup-join/{Created.Count}")));
    }
}

internal sealed class FakeCalendarSettings(bool hasReview) : ICalendarSettings
{
    public int PlanningMinutes => 60;

    public int ReviewMinutes => 30;

    public int RetroMinutes => 45;

    public bool HasSprintReview => hasReview;
}

internal sealed class InMemoryScheduleRepository : IScheduleRepository
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

internal sealed class InMemorySprintCalendarRepository : ISprintCalendarRepository
{
    private readonly Dictionary<Guid, SprintCalendarData> _items = [];

    public Task<SprintCalendar?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(_items.TryGetValue(id, out var d) ? SprintCalendar.Restore(d) : null);

    public Task<IReadOnlyList<SprintCalendar>> ListAsync(string teamId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SprintCalendar>>([.. _items.Values.Where(d => d.TeamId == teamId).Select(SprintCalendar.Restore)]);

    public Task SaveAsync(SprintCalendar calendar, CancellationToken ct)
    {
        _items[calendar.Id] = calendar.Data;
        return Task.CompletedTask;
    }
}
