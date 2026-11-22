using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Standups;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Retrospectives.GetRetroBoard;

/// <summary>Projects <see cref="RetroSummary"/> into the board.</summary>
internal sealed class GetRetroBoardHandler(ISessionRepository sessions, IRetroSettings settings) : IRequestHandler<GetRetroBoardQuery, Result<RetroBoardDto>>
{
    public async Task<Result<RetroBoardDto>> Handle(GetRetroBoardQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<RetroBoardDto>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var summary = RetroSummary.Build(session);
        var roster = session.PreBrief?.Members.ToDictionary(m => m.MemberId, m => Names.First(m.DisplayName)) ?? [];
        var columns = new[] { RetroCategory.Continue, RetroCategory.Stop, RetroCategory.Start }
            .Select(c => new RetroColumnDto(
                c,
                RetroFormats.Label(settings.Format, c),
                summary.In(c).Select(n => new RetroNoteDto(n.Text, n.MemberId is not null && roster.TryGetValue(n.MemberId, out var name) ? name : string.Empty)).ToList()))
            .ToList();
        return Result<RetroBoardDto>.Success(new RetroBoardDto(RetroAgenda.InActions(session) ? "Actions" : "Round", columns, summary.Themes, summary.Actions, summary.Notes.Count(n => n.Category is null)));
    }
}
