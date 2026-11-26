using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Extensions.MSTeams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps.Clients;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Teams.ReceiveMeetingEvent;

namespace Zava.Scrum.Api.Bot;

/// <summary>Thin Teams meeting lifecycle routes for the Azure-managed Scrum Agent.</summary>
public sealed partial class ScrumAgent
{
    private void RegisterMeetingEvents()
    {
        var teams = new TeamsAgentExtension(this);
        RegisterExtension(teams);
        // Azure-managed registration retains SSO. Start/end need OnlineMeeting.ReadBasic.Chat, not participant subscriptions (ADR 0015 S2).
        teams.Meetings.OnStart(OnMeetingStartedAsync).OnEnd(OnMeetingEndedAsync);
    }

    private Task OnMeetingStartedAsync(ITeamsTurnContext context, ITurnState state, MeetingDetails meeting, CancellationToken ct) =>
        ReceiveMeetingEventAsync(context, meeting, MeetingEventKind.Started, ct);

    private Task OnMeetingEndedAsync(ITeamsTurnContext context, ITurnState state, MeetingDetails meeting, CancellationToken ct) =>
        ReceiveMeetingEventAsync(context, meeting, MeetingEventKind.Ended, ct);

    private async Task ReceiveMeetingEventAsync(ITeamsTurnContext context, MeetingDetails meeting, MeetingEventKind kind, CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var command = TeamsMeetingEvent.From(context.Activity, meeting, kind);
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(command, ct);
        if (!result.IsSuccess)
        {
            scope.ServiceProvider.GetRequiredService<ILogger<ScrumAgent>>().LogWarning(
                "Refused Teams meeting {Kind} event {MeetingId} in conversation {ConversationId}: {ErrorCode}",
                kind, command.MeetingId, command.ConversationId, result.Error!.Code);
        }
    }
}
