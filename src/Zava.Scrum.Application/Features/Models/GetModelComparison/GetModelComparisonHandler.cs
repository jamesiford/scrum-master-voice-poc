using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Features.Models.GetModelComparison;

/// <summary>Combines catalogue prices with measured latency from the team's sessions.</summary>
internal sealed class GetModelComparisonHandler(IModelCatalog catalog, ITeamRepository teams, ISessionRepository sessions)
    : IRequestHandler<GetModelComparisonQuery, Result<IReadOnlyList<ModelComparisonRow>>>
{
    // Rule-of-thumb share of a standup the agent actually speaks; stated so the estimate is not mistaken for a bill.
    private const double AgentTalkShare = 0.25;

    public async Task<Result<IReadOnlyList<ModelComparisonRow>>> Handle(GetModelComparisonQuery request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result<IReadOnlyList<ModelComparisonRow>>.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var samples = (await sessions.ListAsync(ct)).Where(s => s.TeamId == team.Id).SelectMany(s => s.LatencySamplesMs).ToList();
        IReadOnlyList<ModelComparisonRow> rows = (await catalog.ListAsync(ct))
            .Select(m =>
            {
                var current = string.Equals(m.Name, catalog.CurrentModel, StringComparison.OrdinalIgnoreCase);
                return new ModelComparisonRow(
                    m.Name,
                    current,
                    ModelCostEstimator.PerStandup(m, team.StandupMinutes, AgentTalkShare),
                    current && samples.Count > 0 ? LatencyStats.Percentile(samples, 50) : null,
                    current && samples.Count > 0 ? LatencyStats.Percentile(samples, 95) : null,
                    m.Notes,
                    m.PricingTier);
            })
            .ToList();
        return Result<IReadOnlyList<ModelComparisonRow>>.Success(rows);
    }
}
