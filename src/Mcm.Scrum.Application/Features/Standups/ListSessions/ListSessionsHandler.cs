using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.ListSessions;

/// <summary>Projects stored sessions to list rows.</summary>
internal sealed class ListSessionsHandler(ISessionRepository sessions)
    : IRequestHandler<ListSessionsQuery, Result<IReadOnlyList<SessionSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<SessionSummaryDto>>> Handle(ListSessionsQuery request, CancellationToken ct)
    {
        var all = await sessions.ListAsync(ct);
        IReadOnlyList<SessionSummaryDto> rows = all
            .Where(s => s.TeamId == request.TeamId)
            .Select(s => new SessionSummaryDto(s.Id, s.TeamId, s.Status, s.CreatedAt, CountFlagged(s)))
            .ToList();
        return Result<IReadOnlyList<SessionSummaryDto>>.Success(rows);
    }

    private static int CountFlagged(Domain.Standups.StandupSession session) =>
        session.PreBrief is null
            ? 0
            : session.PreBrief.Members.SelectMany(m => m.Items).Concat(session.PreBrief.UnassignedItems).DistinctBy(i => i.Id).Count(i => i.Flags.Count > 0);
}
