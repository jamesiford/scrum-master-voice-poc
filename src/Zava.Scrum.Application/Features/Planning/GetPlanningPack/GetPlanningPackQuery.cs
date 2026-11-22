using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Planning;

namespace Mcm.Scrum.Application.Features.Planning.GetPlanningPack;

/// <summary>Reads a planning pack. Without a pack id it reads the team's latest.</summary>
/// <param name="TeamId">Team id (used when <paramref name="PackId"/> is empty).</param>
/// <param name="PackId">The pack, or <see langword="null"/> for the latest.</param>
public sealed record GetPlanningPackQuery(string TeamId, Guid? PackId = null) : IQuery<PlanningPackData>;
