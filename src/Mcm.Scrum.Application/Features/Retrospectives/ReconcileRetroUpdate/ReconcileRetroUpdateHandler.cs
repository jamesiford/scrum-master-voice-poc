using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Standups;
using Mcm.Scrum.Application.Features.Standups.ReconcileUpdate;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Retrospectives.ReconcileRetroUpdate;

/// <summary>Runs <see cref="RetroInterpreter"/> for the phase the retrospective is in.</summary>
internal sealed class ReconcileRetroUpdateHandler(ISessionRepository sessions) : IRequestHandler<ReconcileRetroUpdateQuery, Result<ReconcileResult>>
{
    public async Task<Result<ReconcileResult>> Handle(ReconcileRetroUpdateQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<ReconcileResult>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var roster = session.PreBrief?.Members ?? [];
        if (roster.All(m => m.MemberId != request.MemberId))
        {
            return Result<ReconcileResult>.Failure(StandupErrors.MemberNotInBrief(request.MemberId));
        }

        var active = session.ActiveEvents.ToList();
        var read = RetroAgenda.InActions(session)
            ? RetroInterpreter.Action(request.MemberId, request.Utterance, request.Answering, roster, active)
            : RetroInterpreter.Round(request.MemberId, request.Utterance, request.Answering, request.Correction, active);
        var events = read.Notes.Select(n => new ReconcileEvent(n.Type, n.Text, [], n.Category, n.OwnerMemberId)).ToList();
        return Result<ReconcileResult>.Success(new ReconcileResult(true, [], [], read.Question, events, null, read.RetractedEventIds));
    }
}
