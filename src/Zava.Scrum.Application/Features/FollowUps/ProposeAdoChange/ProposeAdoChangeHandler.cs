using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.FollowUps.ProposeAdoChange;

/// <summary>Drops work item ids outside the sprint (rule 2), then stores a pending follow-up.</summary>
internal sealed class ProposeAdoChangeHandler(ISessionRepository sessions, IFollowUpRepository followUps, IClock clock) : IRequestHandler<ProposeAdoChangeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProposeAdoChangeCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<Guid>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        if (!SprintIds.From(session.PreBrief).Any(i => i.Value == request.WorkItemId))
        {
            return Result<Guid>.Failure(FollowUpErrors.UnknownWorkItem(request.WorkItemId));
        }

        var change = new ProposedChange(request.WorkItemId, request.Op, request.Value, request.Rationale);
        var date = DateOnly.FromDateTime((session.StartedAt ?? session.CreatedAt).UtcDateTime);
        var followUp = FollowUp.Create(Guid.NewGuid(), session.TeamId, session.Id, request.MemberId, FollowUpKind.Followup, $"Proposed change to work item {request.WorkItemId}: {request.Value}", [change], date, clock.UtcNow);
        await followUps.SaveAsync(followUp, ct);
        return Result<Guid>.Success(followUp.Id);
    }
}
