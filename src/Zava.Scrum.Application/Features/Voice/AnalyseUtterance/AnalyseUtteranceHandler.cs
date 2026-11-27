using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;
using Zava.Scrum.Application.Behaviors;

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
        using var hop = ScrumTelemetry.StartHop("decision.analysis", session.TeamId, session.Id, session.CorrelationId);
        var knownIds = SprintIds.From(brief).Select(k => k.Value).ToHashSet();
        var items = brief is null ? [] : brief.Members.SelectMany(m => m.Items).Concat(brief.UnassignedItems).ToList();
        var roster = request.Roster ?? brief?.Members.Select(m => new NameRef(m.MemberId, m.DisplayName)).ToList() ?? [];
        var own = brief?.Members.FirstOrDefault(m => m.MemberId == (request.HolderId ?? request.SpeakerId))?.Items ?? [];
        items = [.. own, .. items.Where(i => !own.Contains(i))];

        // A retrospective sorts its notes by rules in code (RetroInterpreter), so only a standup asks the model.
        var modelStarted = System.Diagnostics.Stopwatch.GetTimestamp();
        var reading = session.Kind == CeremonyKind.Retrospective
            ? null
            : await model.ReadAsync(new UtteranceContext(request.Text, request.SpeakerId, request.HolderId, roster, items, request.Asked, request.FocusItemId), ct);
        var modelMs = session.Kind == CeremonyKind.Retrospective ? 0 : (int)System.Diagnostics.Stopwatch.GetElapsedTime(modelStarted).TotalMilliseconds;
        var analysis = UtteranceAnalyser.Analyse(request.Text, request.SpeakerId, request.HolderId, roster, own, knownIds, reading) with { ModelMs = modelMs };
        hop?.SetTag("analysis.source", analysis.Source.ToString());
        hop?.SetTag("analysis.confidence", reading?.Confidence);
        hop?.SetTag("analysis.rejected_ids", reading?.WorkItemIds.Count(id => !knownIds.Contains(id)) ?? 0);
        ScrumTelemetry.RecordHop("decision.model", session.TeamId, modelMs);
        return Result<UtteranceAnalysis>.Success(analysis);
    }
}
