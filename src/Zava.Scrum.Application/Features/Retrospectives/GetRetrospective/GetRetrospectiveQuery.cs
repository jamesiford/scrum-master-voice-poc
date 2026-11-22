using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Reports;

namespace Zava.Scrum.Application.Features.Retrospectives.GetRetrospective;

/// <summary>Drafts a start-stop-continue retrospective from the sprint and its standups (SPEC M8). Read only.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetRetrospectiveQuery(string TeamId) : IQuery<ReportDocument>;
