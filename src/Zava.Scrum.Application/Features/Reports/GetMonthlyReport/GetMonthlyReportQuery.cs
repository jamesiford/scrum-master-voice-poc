using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Reports.GetMonthlyReport;

/// <summary>Writes the monthly report over three to six sprints.</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Sprints">Window size; clamped to three to six.</param>
public sealed record GetMonthlyReportQuery(string TeamId, int Sprints = 6) : IQuery<ReportDocument>;
