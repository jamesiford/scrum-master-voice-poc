using Microsoft.Agents.Extensions.MSTeams;
using Microsoft.Teams.Apps.Clients;
using Zava.Scrum.Application.Features.Teams.ReceiveMeetingEvent;

namespace Zava.Scrum.Api.Bot;

/// <summary>Translates the SDK's typed Teams meeting activity into a team-scoped Application command.</summary>
public static class TeamsMeetingEvent
{
    /// <summary>Uses channel data for identity and typed meeting details only for the join URL.</summary>
    /// <param name="activity">The authenticated Teams system activity; a From user is not required.</param>
    /// <param name="meeting">The SDK's meeting payload. Its Id is not the channel-data meeting id.</param>
    /// <param name="kind">The lifecycle route that received the event.</param>
    /// <returns>A command that the mediator validates and correlates to stored session metadata.</returns>
    public static ReceiveMeetingEventCommand From(ITeamsActivity activity, MeetingDetails meeting, MeetingEventKind kind)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(meeting);
        // Learn's meeting-apps-apis specifies channelData.meeting.id, never activity.value.Id or conversation.Id.
        return new ReceiveMeetingEventCommand(
            activity.ChannelData?.Tenant?.Id ?? activity.Conversation?.TenantId,
            activity.Conversation?.Id,
            activity.TeamsGetMeetingInfo()?.Id,
            meeting.JoinUrl?.AbsoluteUri,
            kind);
    }
}
