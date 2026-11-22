using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Kpis;

namespace Mcm.Scrum.Application.Features.Reports.GetMonthlyReport;

/// <summary>Builds the monthly report. The model phrases only the narrative; the tables come from code.</summary>
internal sealed class GetMonthlyReportHandler(IKpiHistorySource history, ISessionRepository sessions, IFollowUpRepository followUps, IReportingAgent agent) : IRequestHandler<GetMonthlyReportQuery, Result<ReportDocument>>
{
    public async Task<Result<ReportDocument>> Handle(GetMonthlyReportQuery request, CancellationToken ct)
    {
        var (kpi, window, illustrative, facts) = await ReportFacts.LoadAsync(request.TeamId, request.Sprints, history, sessions, followUps, ct);
        var modelText = await agent.NarrateAsync("monthly", facts, ct);
        var narrative = string.IsNullOrWhiteSpace(modelText) ? ReportComposer.FallbackNarrative(kpi, (window.Count > 0 ? window[^1] : null)) : modelText.Trim();
        var markdown = $"# Monthly report, last {kpi.Sprints} sprints{(illustrative ? " (illustrative data)" : string.Empty)}\n\n{narrative}\n\n{ReportComposer.KpiTable(kpi)}\n{ReportComposer.SprintTable(window)}";
        return Result<ReportDocument>.Success(new ReportDocument("Monthly report", markdown, narrative, !string.IsNullOrWhiteSpace(modelText), illustrative));
    }
}
