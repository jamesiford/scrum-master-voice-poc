using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Teams;

namespace Mcm.Scrum.Infrastructure.Tests;

public sealed class GraphCalendarWriterTests
{
    [Fact]
    public void ToRequest_IsATeamsOnlineMeetingInUtcWithTheRosterAndATransactionId()
    {
        var invite = new MeetingInvite("cal:Standup:2026-12-07", "Sprint 43: Daily standup", new DateTimeOffset(2026, 12, 7, 17, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 7, 17, 45, 0, TimeSpan.Zero), ["daniel@x.com"]);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(GraphCalendarWriter.ToRequest(invite)));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("isOnlineMeeting").GetBoolean());
        Assert.Equal("teamsForBusiness", root.GetProperty("onlineMeetingProvider").GetString());
        Assert.Equal("2026-12-07T17:30:00", root.GetProperty("start").GetProperty("dateTime").GetString());
        Assert.Equal("UTC", root.GetProperty("start").GetProperty("timeZone").GetString());
        Assert.Equal("daniel@x.com", root.GetProperty("attendees")[0].GetProperty("emailAddress").GetProperty("address").GetString());
        Assert.Equal("cal:Standup:2026-12-07", root.GetProperty("transactionId").GetString());
    }

    [Fact]
    public void Parse_ReadsTheEventIdAndJoinUrl()
    {
        using var doc = JsonDocument.Parse("{\"id\":\"AAMk1\",\"onlineMeeting\":{\"joinUrl\":\"https://teams.microsoft.com/l/meetup-join/x\"}}");

        var result = GraphCalendarWriter.Parse(doc.RootElement, "s");

        Assert.Equal("AAMk1", result.Value.EventId);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/x", result.Value.JoinUrl);
    }

    [Fact]
    public void Parse_NoJoinUrl_IsAFailureSoNoSchedulerLinkIsInvented()
    {
        using var doc = JsonDocument.Parse("{\"id\":\"AAMk1\",\"onlineMeeting\":null}");

        Assert.False(GraphCalendarWriter.Parse(doc.RootElement, "s").IsSuccess);
    }
}
