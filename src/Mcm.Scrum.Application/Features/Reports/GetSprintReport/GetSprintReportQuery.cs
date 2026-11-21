using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Reports.GetSprintReport;

/// <summary>Writes the sprint summary: the latest three sprints' KPIs, the current sprint position and a short narrative.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetSprintReportQuery(string TeamId) : IQuery<ReportDocument>;