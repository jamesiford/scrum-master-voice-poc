using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Domain.Tests.Standups;

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
}
