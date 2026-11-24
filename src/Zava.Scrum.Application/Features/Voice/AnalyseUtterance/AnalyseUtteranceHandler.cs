using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Features.Voice.AnalyseUtterance;

/// <summary>
/// Asks the decision model to read the utterance and hands the answer to <see cref="UtteranceAnalyser"/>, which checks every name and
/// work item id. When the model is off, slow or wrong, the same rules that ran before the model existed decide.
/// </summary>
internal sealed class AnalyseUtteranceHandler(ISessionRepository sessions, IUtteranceAnalyser model)
    : IRequestHandler<AnalyseUtteranceQuery, Result<UtteranceAnalysis>>
{
    public async Task<Result<UtteranceAnalysis>> Handle(AnalyseUtteranceQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<UtteranceAnalysis>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var brief = session.PreBrief;
        var knownIds = SprintIds.From(brief).Select(k => k.Value).ToHashSet();
        var items = brief is null ? [] : brief.Members.SelectMany(m => m.Items).Concat(brief.UnassignedItems).ToList();
        var roster = request.Roster ?? brief?.Members.Select(m => new NameRef(m.MemberId, m.DisplayName)).ToList() ?? [];
        var own = brief?.Members.FirstOrDefault(m => m.MemberId == (request.HolderId ?? request.SpeakerId))?.Items ?? [];
        items = [.. own, .. items.Where(i => !own.Contains(i))];

        // A retrospective sorts its notes by rules in code (RetroInterpreter), so only a standup asks the model.
        var reading = session.Kind == CeremonyKind.Retrospective
            ? null
            : await model.ReadAsync(new UtteranceContext(request.Text, request.SpeakerId, request.HolderId, roster, items, request.Asked, request.FocusItemId), ct);
        return Result<UtteranceAnalysis>.Success(UtteranceAnalyser.Analyse(request.Text, request.SpeakerId, request.HolderId, roster, own, knownIds, reading));
    }
}
