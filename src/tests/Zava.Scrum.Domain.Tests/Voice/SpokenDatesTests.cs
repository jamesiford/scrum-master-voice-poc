using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Domain.Tests.Voice;

public sealed class SpokenDatesTests
{
    [Theory]
    [InlineData("that slips to Friday", "Friday")]
    [InlineData("we will have it by tomorrow", "tomorrow")]
    [InlineData("pushing it to next sprint", "next sprint")]
    [InlineData("now due March 3rd", "March 3rd")]
    [InlineData("moved to 4/12", "4/12")]
    [InlineData("done by end of the week", "end of the week")]
    [InlineData("on Monday probably", "on Monday")]
    public void Find_DatePhrase_ReturnsItAsSpoken(string said, string expected) =>
        Assert.Equal(expected, SpokenDates.Find(said));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("waiting on the vendor")]
    public void Find_NoDate_ReturnsNull(string said) =>
        Assert.Null(SpokenDates.Find(said));

    [Fact]
    public void FindAll_OldAndRevisedDates_PreservesTranscriptOrderAndWholeSprintPhrase()
    {
        Assert.Equal(["Friday", "Monday"], SpokenDates.FindAll("The target moves from Friday to Monday."));
        Assert.Equal(["end of the next sprint"], SpokenDates.FindAll("It slips to end of the next sprint."));
    }

    [Theory]
    [InlineData("tomorrow", "2026-11-27")]
    [InlineData("today", "2026-11-26")]
    [InlineData("by Friday", "2026-11-27")]
    [InlineData("until Monday", "2026-11-30")]
    [InlineData("end of sprint", "2026-11-27")]
    [InlineData("end of the next sprint", "2026-12-11")]
    public void Resolve_RelativePhrase_UsesCodeCalendar(string phrase, string expected)
    {
        var context = new SpokenDateContext(new DateOnly(2026, 11, 26),
            new("Sprint 1", new(2026, 11, 16), new(2026, 11, 27)),
            new("Sprint 2", new(2026, 11, 30), new(2026, 12, 11)));

        var resolution = SpokenDateResolver.Resolve(phrase, context);

        Assert.Equal(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), resolution.Date);
        Assert.Equal("resolved", resolution.Status);
    }

    [Theory]
    [InlineData("next sprint", "ambiguous_period_or_date")]
    [InlineData("next week", "ambiguous_period_or_date")]
    [InlineData("12/04", "ambiguous_numeric_date")]
    [InlineData("end of sprint", "current_iteration_unavailable")]
    [InlineData("this Monday", "past_date")]
    public void Resolve_AmbiguousOrMissingCalendar_AbstainsExplicitly(string phrase, string reason)
    {
        var resolution = SpokenDateResolver.Resolve(phrase, new SpokenDateContext(new DateOnly(2026, 11, 26)));

        Assert.Null(resolution.Date);
        Assert.Equal(reason, resolution.Status);
    }

    [Fact]
    public void Resolve_NextIterationAlreadyStarted_AbstainsInsteadOfUsingStaleLabel()
    {
        var context = new SpokenDateContext(new(2026, 11, 30),
            new("Sprint 1", new(2026, 11, 16), new(2026, 11, 27)),
            new("Sprint 2", new(2026, 11, 30), new(2026, 12, 11)));

        var resolution = SpokenDateResolver.Resolve("end of next sprint", context);

        Assert.Null(resolution.Date);
        Assert.Equal("next_iteration_unavailable", resolution.Status);
    }

    [Fact]
    public void Resolve_InvalidNextIterationRange_Abstains()
    {
        var context = new SpokenDateContext(new(2026, 11, 26),
            new("Sprint 1", new(2026, 11, 16), new(2026, 11, 27)),
            new("Sprint 2", new(2026, 12, 20), new(2026, 12, 11)));

        var resolution = SpokenDateResolver.Resolve("end of next sprint", context);

        Assert.Null(resolution.Date);
        Assert.Equal("next_iteration_unavailable", resolution.Status);
    }

    [Fact]
    public void Resolve_CurrentIterationNotStarted_Abstains()
    {
        var context = new SpokenDateContext(new(2026, 11, 26),
            new("future", new(2026, 11, 30), new(2026, 12, 11)));

        var resolution = SpokenDateResolver.Resolve("end of sprint", context);

        Assert.Null(resolution.Date);
        Assert.Equal("current_iteration_unavailable", resolution.Status);
    }

    [Fact]
    public void Resolve_BetweenIterations_StillResolvesVerifiedFutureNextEnd()
    {
        var context = new SpokenDateContext(new(2026, 11, 28),
            new("Sprint 1", new(2026, 11, 16), new(2026, 11, 27)),
            new("Sprint 2", new(2026, 11, 30), new(2026, 12, 11)));

        Assert.Equal(new DateOnly(2026, 12, 11), SpokenDateResolver.Resolve("end of next sprint", context).Date);
    }

    [Fact]
    public void Resolve_StaleOrNonSubsequentIteration_DoesNotInventSprintLength()
    {
        var old = new Zava.Scrum.Domain.Work.IterationInfo("old", new(2026, 10, 1), new(2026, 10, 14));
        var context = new SpokenDateContext(new(2026, 11, 26), old, old);

        Assert.Equal("past_date", SpokenDateResolver.Resolve("end of sprint", context).Status);
        Assert.Equal("next_iteration_unavailable", SpokenDateResolver.Resolve("end of next sprint", context).Status);
        Assert.Equal("calendar_unavailable", SpokenDateResolver.Resolve("tomorrow", null).Status);
        Assert.Equal(new DateOnly(2026, 11, 27), SpokenDateResolver.Resolve("Friday", new(new(2026, 11, 27))).Date);
        Assert.Equal(new DateOnly(2026, 12, 4), SpokenDateResolver.Resolve("next Friday", new(new(2026, 11, 27))).Date);
    }
}
