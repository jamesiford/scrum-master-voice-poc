using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Standups.RecordStandupEvent;

/// <summary>Appends the event to the session, keeping only work item ids that exist in the sprint.</summary>
internal sealed class RecordStandupEventHandler(ISessionRepository sessions, IClock clock)
    : IRequestHandler<RecordStandupEventCommand, Result>
{
    public async Task<Result> Handle(RecordStandupEventCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var known = SprintIds.From(session.PreBrief);
        var ids = (request.WorkItemIds ?? []).Where(id => known.Contains(new Domain.Work.WorkItemId(id))).Distinct().ToList();
        session.RecordEvent(request.Type, request.MemberId, ids, request.Text, clock.UtcNow, request.Category, request.OwnerMemberId);
        await sessions.SaveAsync(session, ct);
        return Result.Success();
    }
}
