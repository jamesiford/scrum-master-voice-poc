using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Planning;

namespace Zava.Scrum.Application.Features.Planning.GetPlanningPack;

/// <summary>Reads a planning pack. Without a pack id it reads the team's latest.</summary>
/// <param name="TeamId">Team id (used when <paramref name="PackId"/> is empty).</param>
/// <param name="PackId">The pack, or <see langword="null"/> for the latest.</param>
public sealed record GetPlanningPackQuery(string TeamId, Guid? PackId = null) : IQuery<PlanningPackData>;
