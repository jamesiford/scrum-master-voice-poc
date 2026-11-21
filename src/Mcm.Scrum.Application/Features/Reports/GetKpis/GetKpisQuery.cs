using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Reports.GetKpis;

/// <summary>Reads KPIs over the latest three to six sprints.</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Sprints">Window size; clamped to three to six.</param>
public sealed record GetKpisQuery(string TeamId, int Sprints = 6) : IQuery<KpiReport>;
