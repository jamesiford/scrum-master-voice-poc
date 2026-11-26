using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Domain.Tests.Standups;

public sealed class StandupSessionAndLatencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(new[] { 900, 1100, 1000, 3000, 800 }, 50, 1000)]
    [InlineData(new[] { 900, 1100, 1000, 3000, 800 }, 95, 3000)]
    [InlineData(new[] { 700 }, 50, 700)]
    [InlineData(new int[0], 50, 0)]
    public void Percentile_Samples_UsesNearestRank(int[] samples, double percentile, int expected)
    {
        Assert.Equal(expected, LatencyStats.Percentile(samples, percentile));
    }

    [Fact]
    public void Start_TwiceOrAfterEnd_IsConflict()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t", Now, null);

        Assert.True(session.Start("simulated", null, 15, Now).IsSuccess);
        Assert.Equal("standup.already_started", session.Start("simulated", null, 15, Now).Error?.Code);
    }

    [Fact]
    public void End_CalledTwice_KeepsFirstEndTime()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t", Now, null);
        session.Start("simulated", null, 15, Now);

        session.End(Now.AddMinutes(5));
        session.End(Now.AddMinutes(9));

        Assert.Equal(Now.AddMinutes(5), session.EndedAt);
        Assert.Equal(StandupStatus.Ended, session.Status);
    }

    [Fact]
    public void MarkAbsent_Twice_RecordsOneEvent()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t", Now, null);

        session.MarkAbsent("m5", Now);
        session.MarkAbsent("m5", Now);

        Assert.Single(session.Events, e => e.Type == StandupEventType.Absent);
    }

    [Fact]
    public void Snapshot_RoundTrips()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t", Now, null);
        session.Start("simulated", null, 7, Now);
        session.MarkAbsent("m5", Now);
        session.CompleteTurn("m1");
        session.AddLatency(1234);
        session.RecordEvent(StandupEventType.Blocker, "m2", [12], "Blocked", Now);

        var copy = StandupSession.Restore(session.ToSnapshot());

        Assert.Equal(7, copy.StandupMinutes);
        Assert.Equal(["m1"], copy.HandledMemberIds);
        Assert.Equal([1234], copy.LatencySamplesMs);
        Assert.Equal(2, copy.Events.Count);
    }

    [Fact]
    public void RecordMeetingEvent_MatchedLiveMeeting_PreservesFirstStartAndRoundTripsIdentity()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t", Now, null);
        session.Start("acs", "https://teams.microsoft.com/l/meetup-join/chat/0", 15, Now);

        Assert.True(session.RecordMeetingEvent("meeting-id", "chat", session.JoinUrl!, false, Now));
        Assert.False(session.RecordMeetingEvent("meeting-id", "chat", session.JoinUrl!, false, Now.AddMinutes(1)));
        var copy = StandupSession.Restore(session.ToSnapshot());

        Assert.Equal(Now, copy.MeetingLifecycle!.StartedAt);
        Assert.False(copy.MatchesMeeting("other-id", "chat", session.JoinUrl!));
        Assert.False(copy.MatchesMeeting("meeting-id", "other-chat", session.JoinUrl!));
        Assert.False(copy.MatchesMeeting("meeting-id", "chat", "https://teams.microsoft.com/l/meetup-join/two/0"));
        Assert.Equal(session.MeetingLifecycle, copy.MeetingLifecycle);
    }

    [Fact]
    public void RecordMeetingEvent_PreparedOrEndedSession_DoesNotChangeMetadataOrLifecycle()
    {
        var session = StandupSession.Prepare(Guid.NewGuid(), "t", Now, null);
        const string url = "https://teams.microsoft.com/l/meetup-join/chat/0";

        Assert.False(session.RecordMeetingEvent("meeting-id", "chat", url, false, Now));
        Assert.Equal(StandupStatus.Prepared, session.Status);
        session.Start("acs", url, 15, Now);
        session.End(Now);
        Assert.False(session.RecordMeetingEvent("meeting-id", "chat", url, true, Now.AddMinutes(1)));
        Assert.Null(session.MeetingLifecycle);
        Assert.Equal(Now, session.EndedAt);
    }
}
