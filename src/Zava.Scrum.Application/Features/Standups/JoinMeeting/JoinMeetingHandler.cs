using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Application.Behaviors;

namespace Zava.Scrum.Application.Features.Standups.JoinMeeting;

/// <summary>Validates the session and hands the join to the bridge port.</summary>
internal sealed class JoinMeetingHandler(ISessionRepository sessions, IMeetingBridge bridge) : IRequestHandler<JoinMeetingCommand, Result>
{
    private const string DisplayName = "Zava Scrum Agent";

    public async Task<Result> Handle(JoinMeetingCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        if (string.IsNullOrWhiteSpace(session.JoinUrl))
        {
            return Result.Failure(StandupErrors.JoinUrlRequired);
        }

        using var hop = ScrumTelemetry.StartHop("meeting.join", session.TeamId, session.Id, session.CorrelationId);
        var result = await bridge.JoinAsync(new MeetingJoinRequest(session.Id, session.JoinUrl, request.BridgeToken, request.ApiBase, DisplayName), ct);
        hop?.SetTag("join.outcome", result.IsSuccess ? "accepted" : "refused");
        ScrumTelemetry.Count("join", session.TeamId, result.IsSuccess ? "accepted" : "refused");
        return result;
    }
}
