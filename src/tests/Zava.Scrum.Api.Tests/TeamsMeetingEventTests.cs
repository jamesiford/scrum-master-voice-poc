using System.Text.Json;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Extensions.MSTeams;
using Microsoft.Teams.Apps.Clients;
using Zava.Scrum.Api.Bot;
using Zava.Scrum.Application.Features.Teams.ReceiveMeetingEvent;

namespace Zava.Scrum.Api.Tests;

/// <summary>Real SDK activity conversion keeps system-event identity separate from the payload and conversation ids.</summary>
public sealed class TeamsMeetingEventTests
{
    [Fact]
    public void From_SystemEventWithoutSender_UsesChannelDataMeetingIdAndTenant()
    {
        var activity = ActivityWithChannelData("""
            {"tenant":{"id":"11111111-1111-1111-1111-111111111111"},"meeting":{"id":"opaque-meeting-id"}}
            """);
        activity.Conversation = new ConversationAccount { Id = "bound-chat" };
        var meeting = new MeetingDetails { Id = "different-payload-id", JoinUrl = new Uri("https://teams.microsoft.com/l/meetup-join/one/0") };

        var command = TeamsMeetingEvent.From(activity, meeting, MeetingEventKind.Ended);

        Assert.Equal("opaque-meeting-id", command.MeetingId);
        Assert.Equal(ApiFactory.TenantId, command.TenantId);
        Assert.Equal("bound-chat", command.ConversationId);
        Assert.Equal(meeting.JoinUrl.AbsoluteUri, command.JoinUrl);
        Assert.Equal(MeetingEventKind.Ended, command.Kind);
    }

    [Fact]
    public void From_NoChannelDataMeetingId_DoesNotFallBackToPayloadOrConversationId()
    {
        var activity = ActivityWithChannelData("""{"tenant":{"id":"11111111-1111-1111-1111-111111111111"}}""");
        activity.Conversation = new ConversationAccount { Id = "conversation-not-a-meeting-id" };

        var command = TeamsMeetingEvent.From(activity, new MeetingDetails { Id = "payload-not-a-meeting-id" }, MeetingEventKind.Started);

        Assert.Null(command.MeetingId);
        Assert.Null(command.JoinUrl);
    }

    [Fact]
    public void From_TenantOnlyOnConversation_UsesItWithoutRequiringAUser()
    {
        var activity = ActivityWithChannelData("""{"meeting":{"id":"opaque-meeting-id"}}""");
        activity.Conversation = new ConversationAccount { Id = "bound-chat", TenantId = ApiFactory.TenantId };

        var command = TeamsMeetingEvent.From(activity, new MeetingDetails(), MeetingEventKind.Started);

        Assert.Equal(ApiFactory.TenantId, command.TenantId);
    }

    private static TeamsActivity ActivityWithChannelData(string json)
    {
        var activity = new TeamsActivity { Type = ActivityTypes.Event, ChannelId = "msteams" };
        ((IActivity)activity).ChannelData = JsonDocument.Parse(json).RootElement.Clone();
        return activity;
    }
}
