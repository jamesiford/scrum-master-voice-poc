using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Features.Audit.GetAuditChain;

/// <summary>
/// Assembles the chain from the stores that each hold a part of it. Nothing is written. Missing links are listed, not hidden, so the
/// gate and a person can both see where a chain breaks.
/// </summary>
internal sealed class GetAuditChainHandler(ISessionRepository sessions, IFollowUpRepository followUps, IApprovalRecordStore records, ICardOutbox outbox)
    : IRequestHandler<GetAuditChainQuery, Result<AuditChainDto>>
{
    public async Task<Result<AuditChainDto>> Handle(GetAuditChainQuery request, CancellationToken ct)
    {
        var prefix = Correlation.ToPrefix(request.Reference);
        if (prefix is null)
        {
            return Result<AuditChainDto>.Failure(AuditErrors.InvalidReference);
        }

        var trail = await records.ListByCorrelationAsync(prefix, ct);
        var session = (await sessions.ListAsync(ct)).FirstOrDefault(s => s.CorrelationId.StartsWith(prefix, StringComparison.Ordinal));
        var correlationId = session?.CorrelationId ?? (trail.Count > 0 ? trail[0].CorrelationId : null);
        if (correlationId is null)
        {
            return Result<AuditChainDto>.Failure(AuditErrors.NotFound(request.Reference));
        }

        var proposals = await FollowUpsAsync(session, trail, ct);
        var teamId = session?.TeamId ?? (proposals.Count > 0 ? proposals[0].TeamId : trail[0].TeamId);
        var cards = session is null ? [] : await outbox.ListAsync(session.Id, ct);
        var dto = new AuditChainDto(
            correlationId,
            Correlation.Tag(correlationId),
            teamId,
            session is null ? null : new AuditSessionDto(session.Id, session.Kind, session.Mode, session.CreatedAt, session.StartedAt, session.EndedAt),
            session is null ? [] : session.Events.OrderBy(e => e.At).Select(e => new AuditTurnDto(e.At, e.Type, e.MemberId, e.WorkItemIds)).ToList(),
            proposals.Select(f => new AuditFollowUpDto(f.Id, f.Kind, f.Status, f.MemberId, f.ComputeProposalHash(), f.Changes.Count, f.ApprovedBy)).ToList(),
            trail,
            cards.OrderBy(c => c.SentAt).Select(c => new AuditCardDto(c.Id, c.SentAt, c.Message.Kind, c.Message.FollowUpId)).ToList(),
            proposals.Count(f => f.Status is FollowUpStatus.Approved or FollowUpStatus.Applied),
            Missing(session, proposals, trail))
        {
            Meeting = session?.MeetingLifecycle,
            JoinUrl = session?.JoinUrl,
        };
        return Result<AuditChainDto>.Success(dto);
    }

    private async Task<IReadOnlyList<FollowUp>> FollowUpsAsync(Domain.Standups.StandupSession? session, IReadOnlyList<ApprovalRecord> trail, CancellationToken ct)
    {
        var found = new Dictionary<Guid, FollowUp>();
        if (session is not null)
        {
            foreach (var f in await followUps.ListBySessionAsync(session.Id, ct))
            {
                found[f.Id] = f;
            }
        }

        foreach (var id in trail.Select(r => r.FollowUpId).Distinct().Where(id => !found.ContainsKey(id)))
        {
            if (await followUps.GetAsync(id, ct) is { } f)
            {
                found[f.Id] = f;
            }
        }

        return found.Values.OrderBy(f => f.CreatedAt).ToList();
    }

    private static List<string> Missing(Domain.Standups.StandupSession? session, IReadOnlyList<FollowUp> proposals, IReadOnlyList<ApprovalRecord> trail)
    {
        var missing = new List<string>();
        if (session is null && proposals.Any(f => f.Ceremony == Domain.Standups.CeremonyKind.DailyScrum))
        {
            missing.Add("session");
        }

        foreach (var f in proposals.Where(f => f.Status is FollowUpStatus.Approved or FollowUpStatus.Applied or FollowUpStatus.Rejected))
        {
            var own = trail.Where(r => r.FollowUpId == f.Id).ToList();
            var kind = f.Status == FollowUpStatus.Rejected ? ApprovalRecordKind.Rejected : ApprovalRecordKind.Approved;
            var decision = own.LastOrDefault(r => r.Kind == kind);
            if (decision is null)
            {
                missing.Add($"decision record for follow-up {f.Id}");
                continue;
            }

            if (decision.ProposalHash != f.ComputeProposalHash())
            {
                missing.Add($"matching proposal hash for follow-up {f.Id}");
            }

            if (f.Status == FollowUpStatus.Applied && !own.Any(r => r.Kind == ApprovalRecordKind.Applied && r.DecisionRecordId == decision.Id && (r.AdoRevisions.Count > 0 || f.Changes.Count == 0)))
            {
                missing.Add($"Azure DevOps write record for follow-up {f.Id}");
            }
        }

        return missing;
    }
}
