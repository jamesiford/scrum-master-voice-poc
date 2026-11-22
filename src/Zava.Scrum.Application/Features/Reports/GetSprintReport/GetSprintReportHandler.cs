using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Kpis;

namespace Zava.Scrum.Application.Features.Reports.GetSprintReport;

/// <summary>Builds the sprint summary. The model phrases only the narrative; the tables come from code.</summary>
internal sealed class GetSprintReportHandler(IKpiHistorySource history, ISessionRepository sessions, IFollowUpRepository followUps, IReportingAgent agent) : IRequestHandler<GetSprintReportQuery, Result<ReportDocument>>
{
    public async Task<Result<ReportDocument>> Handle(GetSprintReportQuery request, CancellationToken ct)
    {
        var (kpi, window, illustrative, facts) = await ReportFacts.LoadAsync(request.TeamId, KpiCalculator.MinSprints, history, sessions, followUps, ct);
        var modelText = await agent.NarrateAsync("sprint", facts, ct);
        var narrative = string.IsNullOrWhiteSpace(modelText) ? ReportComposer.FallbackNarrative(kpi, (window.Count > 0 ? window[^1] : null)) : modelText.Trim();
        var markdown = $"# Sprint summary{(illustrative ? " (illustrative data)" : string.Empty)}\n\n{narrative}\n\n{ReportComposer.KpiTable(kpi)}\n{ReportComposer.SprintTable(window)}";
        return Result<ReportDocument>.Success(new ReportDocument("Sprint summary", markdown, narrative, !string.IsNullOrWhiteSpace(modelText), illustrative));
    }
}
