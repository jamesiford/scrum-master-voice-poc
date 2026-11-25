using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Teams.CreateTeam;

/// <summary>Registers a team. It starts as a draft: nothing is scheduled until an Admin activates it.</summary>
/// <param name="TeamId">The new team's id (a lower-case slug).</param>
/// <param name="Input">The team's details.</param>
public sealed record CreateTeamCommand(string TeamId, TeamInput Input) : ICommand<string>;
