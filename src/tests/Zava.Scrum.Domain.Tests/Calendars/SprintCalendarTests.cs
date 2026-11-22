using Zava.Scrum.Domain.Calendars;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Schedules;

namespace Zava.Scrum.Domain.Tests.Calendars;

public sealed class SprintCalendarPlannerTests
{
    private static readonly TimeZoneInfo Pacific = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
    private static readonly DayOfWeek[] Weekdays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    private static readonly CalendarDurations Lengths = new(60, 15, 30, 45, false);

    // Mon 2026-12-07 to Fri 2026-12-18: ten working days, Pacific standard time (UTC-8).
    private static IReadOnlyList<CalendarMeeting> TwoWeeks(CalendarDurations? d = null, IReadOnlyCollection<DayOfWeek>? days = null) =>
        SprintCalendarPlanner.Plan("Sprint 43", new DateOnly(2026, 12, 7), new DateOnly(2026, 12, 18), Pacific, new TimeOnly(9, 30), days ?? Weekdays, d ?? Lengths);

    [Fact]
    public void Plan_TwoWeekSprint_HasADailyStandupOnEveryWorkingDay()
    {
        var standups = TwoWeeks().Where(m => m.Kind == MeetingKind.Standup).ToList();

        Assert.Equal(10, standups.Count);
        Assert.All(standups, s => Assert.Equal(TimeSpan.FromMinutes(15), s.EndsAt - s.StartsAt));
        Assert.Equal(new DateTimeOffset(2026, 12, 7, 17, 30, 0, TimeSpan.Zero), standups[0].StartsAt);
    }

    [Fact]
    public void Plan_PlanningIsOnDayOneAfterTheStandup()
    {
        var planning = Assert.Single(TwoWeeks(), m => m.Kind == MeetingKind.Planning);

        Assert.Equal("Planning:2026-12-07", planning.Key);
        Assert.Equal(new DateTimeOffset(2026, 12, 7, 17, 45, 0, TimeSpan.Zero), planning.StartsAt);
        Assert.Equal(TimeSpan.FromMinutes(60), planning.EndsAt - planning.StartsAt);
    }

    [Fact]
    public void Plan_WithoutReview_RetrospectiveIsOnTheLastDayAndNoReviewExists()
    {
        var meetings = TwoWeeks();

        var retro = Assert.Single(meetings, m => m.Kind == MeetingKind.Retrospective);
        Assert.Equal("Retrospective:2026-12-18", retro.Key);
        Assert.Equal(new DateTimeOffset(2026, 12, 18, 17, 45, 0, TimeSpan.Zero), retro.StartsAt);
        Assert.DoesNotContain(meetings, m => m.Kind == MeetingKind.Review);
    }

    [Fact]
    public void Plan_WithReview_ReviewThenRetrospectiveBackToBack()
    {
        var meetings = TwoWeeks(Lengths with { HasSprintReview = true });

        var review = Assert.Single(meetings, m => m.Kind == MeetingKind.Review);
        var retro = Assert.Single(meetings, m => m.Kind == MeetingKind.Retrospective);
        Assert.Equal(new DateTimeOffset(2026, 12, 18, 17, 45, 0, TimeSpan.Zero), review.StartsAt);
        Assert.Equal(review.EndsAt, retro.StartsAt);
    }

    [Fact]
    public void Plan_UsesTheConfiguredDurations()
    {
        var meetings = TwoWeeks(new CalendarDurations(120, 10, 20, 90, true));

        Assert.Equal(TimeSpan.FromMinutes(120), Assert.Single(meetings, m => m.Kind == MeetingKind.Planning).EndsAt - Assert.Single(meetings, m => m.Kind == MeetingKind.Planning).StartsAt);
        Assert.Equal(TimeSpan.FromMinutes(10), meetings.First(m => m.Kind == MeetingKind.Standup).EndsAt - meetings.First(m => m.Kind == MeetingKind.Standup).StartsAt);
        Assert.Equal(TimeSpan.FromMinutes(20), Assert.Single(meetings, m => m.Kind == MeetingKind.Review).EndsAt - Assert.Single(meetings, m => m.Kind == MeetingKind.Review).StartsAt);
        Assert.Equal(TimeSpan.FromMinutes(90), Assert.Single(meetings, m => m.Kind == MeetingKind.Retrospective).EndsAt - Assert.Single(meetings, m => m.Kind == MeetingKind.Retrospective).StartsAt);
    }

    [Fact]
    public void Plan_SprintStartingOnASunday_PlansOnTheFirstWorkingDay()
    {
        var meetings = SprintCalendarPlanner.Plan("S", new DateOnly(2026, 12, 6), new DateOnly(2026, 12, 11), Pacific, new TimeOnly(9, 30), Weekdays, Lengths);

        Assert.Equal("Planning:2026-12-07", Assert.Single(meetings, m => m.Kind == MeetingKind.Planning).Key);
        Assert.Equal("Retrospective:2026-12-11", Assert.Single(meetings, m => m.Kind == MeetingKind.Retrospective).Key);
    }

    [Fact]
    public void Plan_OnlyTheTeamsWorkingDaysGetStandups()
    {
        var standups = TwoWeeks(days: [DayOfWeek.Monday, DayOfWeek.Wednesday]).Where(m => m.Kind == MeetingKind.Standup).ToList();

        Assert.Equal(4, standups.Count);
        Assert.All(standups, s => Assert.Contains(s.StartsAt.DayOfWeek, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }));
    }

    [Fact]
    public void Plan_AcrossDaylightSavingTime_KeepsTheLocalStandupTime()
    {
        // US clocks move forward on 2027-03-14: 09:30 local is 17:30Z before and 16:30Z after.
        var meetings = SprintCalendarPlanner.Plan("S", new DateOnly(2027, 3, 12), new DateOnly(2027, 3, 16), Pacific, new TimeOnly(9, 30), Weekdays, Lengths);

        var standups = meetings.Where(m => m.Kind == MeetingKind.Standup).ToList();
        Assert.Equal(new DateTimeOffset(2027, 3, 12, 17, 30, 0, TimeSpan.Zero), standups[0].StartsAt);
        Assert.Equal(new DateTimeOffset(2027, 3, 15, 16, 30, 0, TimeSpan.Zero), standups[1].StartsAt);
    }

    [Fact]
    public void Plan_NoWorkingDay_ReturnsNothing() =>
        Assert.Empty(TwoWeeks(days: []));

    [Fact]
    public void Plan_ReturnsMeetingsInTimeOrderWithUniqueKeys()
    {
        var meetings = TwoWeeks(Lengths with { HasSprintReview = true });

        Assert.Equal(meetings.OrderBy(m => m.StartsAt), meetings);
        Assert.Equal(meetings.Count, meetings.Select(m => m.Key).Distinct().Count());
    }
}

public sealed class IcsFormatterTests
{
    [Fact]
    public void Format_WritesOneEventPerMeetingInUtcWithCrLf()
    {
        var meeting = new CalendarMeeting("Standup:2026-12-07", MeetingKind.Standup, "Sprint 43: Daily standup, team", new DateTimeOffset(2026, 12, 7, 17, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 7, 17, 45, 0, TimeSpan.Zero));

        var ics = IcsFormatter.Format("Sprint 43", [meeting], new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Contains("BEGIN:VEVENT\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("DTSTART:20261207T173000Z\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("DTEND:20261207T174500Z\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("SUMMARY:Sprint 43: Daily standup\\, team\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("UID:Standup:2026-12-07@mcm-scrum-agent", ics, StringComparison.Ordinal);
        Assert.EndsWith("END:VCALENDAR\r\n", ics, StringComparison.Ordinal);
    }
}

public sealed class SprintCalendarTests
{
    private static readonly DateTimeOffset Now = new(2026, 12, 1, 10, 0, 0, TimeSpan.Zero);

    private static SprintCalendar Make() =>
        SprintCalendar.Propose(
            Guid.NewGuid(),
            "t1",
            "P\\Sprint 43",
            "UTC",
            [new CalendarMeeting("Standup:2026-12-07", MeetingKind.Standup, "Standup", Now, Now.AddMinutes(15)), new CalendarMeeting("Planning:2026-12-07", MeetingKind.Planning, "Planning", Now, Now.AddHours(1))],
            Now);

    [Fact]
    public void Propose_RaisesSprintCalendarProposed() =>
        Assert.IsType<SprintCalendarProposed>(Assert.Single(Make().DomainEvents));

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Approve_WithoutANamedApprover_ReturnsNoMeetings(string approver)
    {
        var calendar = Make();

        var result = calendar.Approve(approver, Now);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal(SprintCalendarStatus.Proposed, calendar.Data.Status);
    }

    [Fact]
    public void Approve_Named_ReturnsEveryMeetingAndStaysApprovingUntilAllExist()
    {
        var calendar = Make();

        var meetings = calendar.Approve("Sam", Now).Value;
        calendar.RecordCreated("Standup:2026-12-07", "e1", "https://join/1");
        calendar.Complete();

        Assert.Equal(2, meetings.Count);
        Assert.Equal(SprintCalendarStatus.Approving, calendar.Data.Status);
        calendar.RecordCreated("Planning:2026-12-07", "e2", "https://join/2");
        calendar.Complete();
        Assert.Equal(SprintCalendarStatus.Approved, calendar.Data.Status);
        Assert.Equal("Sam", calendar.Data.DecidedBy);
    }

    [Fact]
    public void Approve_Retry_ReturnsOnlyTheMeetingsNotYetCreated()
    {
        var calendar = Make();
        calendar.Approve("Sam", Now);
        calendar.RecordCreated("Standup:2026-12-07", "e1", "https://join/1");

        var rest = calendar.Approve("Sam", Now).Value;

        Assert.Equal("Planning:2026-12-07", Assert.Single(rest).Key);
    }

    [Fact]
    public void Reject_ThenApprove_IsAConflictAndReturnsNothing()
    {
        var calendar = Make();
        calendar.Reject("Sam", Now);

        var result = calendar.Approve("Sam", Now);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal(SprintCalendarStatus.Rejected, calendar.Data.Status);
    }

    [Fact]
    public void Reject_AfterApproval_IsAConflict()
    {
        var calendar = Make();
        calendar.Approve("Sam", Now);

        Assert.Equal(ErrorType.Conflict, calendar.Reject("Sam", Now).Error!.Type);
    }
}

public sealed class ScheduleJoinUrlTests
{
    private static readonly DateOnly Monday = new(2026, 12, 7);

    [Fact]
    public void JoinUrlFor_CalendarMeeting_WinsOverThePastedLink()
    {
        var schedule = Schedule.Default("t") with { JoinUrl = "https://pasted", Meetings = [new ScheduledMeeting(Monday, "https://calendar")] };

        Assert.Equal("https://calendar", schedule.JoinUrlFor(Monday));
        Assert.Equal("https://pasted", schedule.JoinUrlFor(Monday.AddDays(1)));
    }

    [Fact]
    public void Evaluate_AcsWithOnlyACalendarMeeting_StartsWithoutAPastedLink()
    {
        var schedule = new Schedule("t", true, [DayOfWeek.Monday], new TimeOnly(9, 30), "acs", null, Monday, Guid.NewGuid(), null, [new ScheduledMeeting(Monday, "https://calendar")]);

        Assert.Equal(ScheduleAction.Start, ScheduleEvaluator.Evaluate(schedule, Monday.ToDateTime(new TimeOnly(9, 29))));
    }

    [Fact]
    public void Evaluate_AcsWithNoLinkForTheDay_DoesNotStart()
    {
        var schedule = new Schedule("t", true, [DayOfWeek.Monday], new TimeOnly(9, 30), "acs", null, Monday, Guid.NewGuid(), null, [new ScheduledMeeting(Monday.AddDays(7), "https://calendar")]);

        Assert.Equal(ScheduleAction.None, ScheduleEvaluator.Evaluate(schedule, Monday.ToDateTime(new TimeOnly(9, 29))));
    }
}
