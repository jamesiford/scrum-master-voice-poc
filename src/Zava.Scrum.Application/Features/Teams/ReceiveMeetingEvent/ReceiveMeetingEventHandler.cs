using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Teams.ReceiveMeetingEvent;

/// <summary>Correlates system events to one team-owned live session and reuses its normal end lifecycle.</summary>
internal sealed class ReceiveMeetingEventHandler(
    ITeamResolver teams,
    ISessionRepository sessions,
    IPublisher publisher,
    IClock clock) : IRequestHandler<ReceiveMeetingEventCommand, Result>
{
    /// <summary>Records start metadata or ends the matching ceremony. Unknown meetings and late duplicates are harmless.</summary>
    /// <param name="request">The authenticated Teams activity's scope and typed meeting details.</param>
    /// <param name="ct">Cancellation token for work before the save commits.</param>
    /// <returns>Success for a matched or irrelevant meeting, or a refusal for an unresolved conversation.</returns>
    public async Task<Result> Handle(ReceiveMeetingEventCommand request, CancellationToken ct)
    {
        // System events may have no From user. Tenant and unambiguous conversation bindings authorise them (ADR 0018).
        var team = await teams.ForConversationAsync(request.TenantId, request.ConversationId, ct);
        if (!team.IsSuccess)
        {
            return Result.Failure(team.Error!);
        }

        var matches = (await sessions.ListAsync(ct))
            .Where(s => s.TeamId == team.Value.Id && s.Status == StandupStatus.Live &&
                s.MatchesMeeting(request.MeetingId!, request.ConversationId!, request.JoinUrl!)).ToList();
        if (matches.Count != 1)
        {
            return matches.Count == 0 ? Result.Success() : Result.Failure(
                Error.Conflict("meeting.ambiguous_session", "More than one active ceremony matches this meeting. Nothing was changed."));
        }

        var session = matches[0];
        var ended = request.Kind == MeetingEventKind.Ended;
        if (!session.RecordMeetingEvent(request.MeetingId!, request.ConversationId!, request.JoinUrl!, ended, clock.UtcNow))
        {
            return Result.Success();
        }

        if (ended)
        {
            session.End(clock.UtcNow);
        }

        await sessions.SaveAsync(session, ct);
        // The same committed end lifecycle as EndStandupHandler: hanging up can cancel the delivery token (ADR 0016).
        if (ended)
        {
            await session.PublishEventsAsync(publisher, CancellationToken.None);
        }

        return Result.Success();
    }
}
