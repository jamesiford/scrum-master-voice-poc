using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Planning;

namespace Mcm.Scrum.Application.Features.Planning.GetPlanningPrep;

/// <summary>Reads the sprint planning prep: load per member, carry-over, readiness gaps and owner suggestions.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetPlanningPrepQuery(string TeamId) : IQuery<PlanningPrep>;
