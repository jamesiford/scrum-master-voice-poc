using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Teams;

namespace Mcm.Scrum.Application.Features.Teams.GetTeam;

/// <summary>Reads a team's settings.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetTeamQuery(string TeamId) : IQuery<Team>;
