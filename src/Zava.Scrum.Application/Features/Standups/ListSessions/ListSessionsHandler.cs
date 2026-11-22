using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Standups.ListSessions;

/// <summary>Projects stored standup sessions to list rows. Planning and review sessions only carry their pack and are not listed; a retrospective run by voice (it has a roster brief) is.</summary>
internal sealed class ListSessionsHandler(ISessionRepository sessions)
    : IRequestHandler<ListSessionsQuery, Result<IReadOnlyList<SessionSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<SessionSummaryDto>>> Handle(ListSessionsQuery request, CancellationToken ct)
    {
        var all = await sessions.ListAsync(ct);
        IReadOnlyList<SessionSummaryDto> rows = all
            .Where(s => s.TeamId == request.TeamId && (s.Kind == Domain.Standups.CeremonyKind.DailyScrum || (s.Kind == Domain.Standups.CeremonyKind.Retrospective && s.PreBrief is not null)))
            .Select(s => new SessionSummaryDto(s.Id, s.TeamId, s.Status, s.CreatedAt, CountFlagged(s), s.Kind))
            .ToList();
        return Result<IReadOnlyList<SessionSummaryDto>>.Success(rows);
    }

    private static int CountFlagged(Domain.Standups.StandupSession session) =>
        session.PreBrief is null
            ? 0
            : session.PreBrief.Members.SelectMany(m => m.Items).Concat(session.PreBrief.UnassignedItems).DistinctBy(i => i.Id).Count(i => i.Flags.Count > 0);
}
