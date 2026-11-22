using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.GetTeam;

/// <summary>Reads a team's settings.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetTeamQuery(string TeamId) : IQuery<Team>;
