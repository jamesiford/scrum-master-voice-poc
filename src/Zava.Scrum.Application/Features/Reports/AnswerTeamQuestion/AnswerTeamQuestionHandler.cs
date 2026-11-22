using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Kpis;

namespace Zava.Scrum.Application.Features.Reports.AnswerTeamQuestion;

/// <summary>
/// Answers from facts only. When the model is unavailable the user gets the KPI table, so a number is never invented.
/// </summary>
internal sealed class AnswerTeamQuestionHandler(IKpiHistorySource history, ISessionRepository sessions, IFollowUpRepository followUps, IReportingAgent agent) : IRequestHandler<AnswerTeamQuestionQuery, Result<string>>
{
    public async Task<Result<string>> Handle(AnswerTeamQuestionQuery request, CancellationToken ct)
    {
        var (kpi, _, _, facts) = await ReportFacts.LoadAsync(request.TeamId, KpiCalculator.MaxSprints, history, sessions, followUps, ct);
        var answer = await agent.AnswerAsync(request.Question, facts, ct);
        return Result<string>.Success(string.IsNullOrWhiteSpace(answer)
            ? "I could not reach the language model, so here are the latest team KPIs:\n\n" + ReportComposer.KpiTable(kpi)
            : answer.Trim());
    }
}
