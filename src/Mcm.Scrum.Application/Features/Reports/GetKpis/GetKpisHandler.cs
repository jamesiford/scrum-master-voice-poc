using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Kpis;

namespace Mcm.Scrum.Application.Features.Reports.GetKpis;

/// <summary>Computes the KPIs in code from the history source.</summary>
internal sealed class GetKpisHandler(IKpiHistorySource history) : IRequestHandler<GetKpisQuery, Result<KpiReport>>
{
    public async Task<Result<KpiReport>> Handle(GetKpisQuery request, CancellationToken ct)
    {
        var h = await history.GetAsync(request.TeamId, ct);
        var window = KpiCalculator.Window(h.Sprints, request.Sprints);
        return Result<KpiReport>.Success(new KpiReport(KpiCalculator.Summarize(window), window, h.Illustrative));
    }
}
