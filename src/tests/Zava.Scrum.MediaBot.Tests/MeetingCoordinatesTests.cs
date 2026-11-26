using Microsoft.Graph.Models;
using Zava.Scrum.MediaBot;

namespace Zava.Scrum.MediaBot.Tests;

public sealed class MeetingCoordinatesTests
{
    private const string Tenant = "fb049398-a8b8-4ade-8543-1ae7a82f4869";

    [Fact]
    public void Parse_StoredCalendarLink_UsesActualChatAndOrganiser()
    {
        var organiser = Guid.NewGuid();
        var context = Uri.EscapeDataString($"{{\"Tid\":\"{Tenant}\",\"Oid\":\"{organiser}\"}}");
        var (chat, meeting) = MeetingCoordinates.Parse($"https://teams.microsoft.com/l/meetup-join/19%3Ameeting%40thread.v2/0?context={context}", Tenant);
        Assert.Equal("19:meeting@thread.v2", chat!.ThreadId);
        Assert.Equal(organiser.ToString(), Assert.IsType<OrganizerMeetingInfo>(meeting).Organizer!.User!.Id);
    }

    [Theory]
    [InlineData("https://attacker.example/l/meetup-join/thread/0?context={}")]
    [InlineData("http://teams.microsoft.com/meet/123")]
    [InlineData("https://teams.microsoft.com/meet/not-an-id")]
    public void Parse_InvalidLink_RejectsBeforeGraph(string url) =>
        Assert.Throws<ArgumentException>(() => MeetingCoordinates.Parse(url, Tenant));

    [Fact]
    public void Parse_DifferentTenant_Rejects()
    {
        var context = Uri.EscapeDataString($"{{\"Tid\":\"{Guid.NewGuid()}\",\"Oid\":\"{Guid.NewGuid()}\"}}");
        Assert.Throws<ArgumentException>(() => MeetingCoordinates.Parse($"https://teams.microsoft.com/l/meetup-join/thread/0?context={context}", Tenant));
    }

    [Theory]
    [InlineData("a%2Bb%26c")]
    [InlineData("a+b%26c")]
    public void Parse_ShortLink_PreservesPasscodeWithoutInventingChat(string passcode)
    {
        var (chat, meeting) = MeetingCoordinates.Parse($"https://teams.cloud.microsoft/meet/123456?p={passcode}", Tenant);
        Assert.Null(chat);
        Assert.Equal("a+b&c", Assert.IsType<JoinMeetingIdMeetingInfo>(meeting).Passcode);
    }
}
