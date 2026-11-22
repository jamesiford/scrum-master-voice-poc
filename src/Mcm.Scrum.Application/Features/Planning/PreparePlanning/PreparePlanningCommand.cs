using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Planning.PreparePlanning;

/// <summary>
/// Prepares the sprint planning pack: capacity from the velocity history, carry-over first, then ready backlog items in order, with
/// owner suggestions and a draft goal. Stores the pack and raises <c>PlanningPrepared</c>. Writes nothing to Azure DevOps.
/// </summary>
/// <param name="TeamId">Team id.</param>
public sealed record PreparePlanningCommand(string TeamId) : ICommand<Guid>;
