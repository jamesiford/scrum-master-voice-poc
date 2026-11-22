using Zava.Scrum.Domain.Schedules;

namespace Zava.Scrum.Domain.Tests.Schedules;

public sealed class ScheduleEvaluatorTests
{
    // 2026-01-07 is a Wednesday.
    private static readonly DateOnly Day = new(2026, 1, 7);

    private static Schedule Make(string mode = "acs", bool enabled = true, DateOnly? prepared = null, DateOnly? started = null) =>
        new("t", enabled, [DayOfWeek.Wednesday], new TimeOnly(9, 30), mode, "https://teams/x", prepared, null, started);

    [Theory]
    [InlineData("09:19", ScheduleAction.None)]
    [InlineData("09:20", ScheduleAction.Prepare)]
    [InlineData("09:45", ScheduleAction.Prepare)]
    [InlineData("10:00", ScheduleAction.None)]
    public void Evaluate_NotYetPrepared_PreparesFromTMinus10UntilGrace(string time, ScheduleAction expected)
    {
        var action = ScheduleEvaluator.Evaluate(Make(), Day.ToDateTime(TimeOnly.Parse(time, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(expected, action);
    }

    [Theory]
    [InlineData("09:28", ScheduleAction.None)]
    [InlineData("09:29", ScheduleAction.Start)]
    [InlineData("09:35", ScheduleAction.Start)]
    public void Evaluate_Prepared_StartsFromTMinus1(string time, ScheduleAction expected)
    {
        var action = ScheduleEvaluator.Evaluate(Make(prepared: Day), Day.ToDateTime(TimeOnly.Parse(time, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(expected, action);
    }

    [Fact]
    public void Evaluate_AlreadyStarted_DoesNothing() =>
        Assert.Equal(ScheduleAction.None, ScheduleEvaluator.Evaluate(Make(prepared: Day, started: Day), Day.ToDateTime(new TimeOnly(9, 31))));

    [Fact]
    public void Evaluate_SimulatedMode_PreparesButNeverStarts() =>
        Assert.Equal(ScheduleAction.None, ScheduleEvaluator.Evaluate(Make("simulated", prepared: Day), Day.ToDateTime(new TimeOnly(9, 31))));

    [Fact]
    public void Evaluate_DisabledOrWrongDay_DoesNothing()
    {
        Assert.Equal(ScheduleAction.None, ScheduleEvaluator.Evaluate(Make(enabled: false), Day.ToDateTime(new TimeOnly(9, 25))));
        Assert.Equal(ScheduleAction.None, ScheduleEvaluator.Evaluate(Make(), Day.AddDays(1).ToDateTime(new TimeOnly(9, 25))));
    }
}
