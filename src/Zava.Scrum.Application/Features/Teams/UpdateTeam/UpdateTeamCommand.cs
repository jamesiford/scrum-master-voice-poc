using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Teams.UpdateTeam;

/// <summary>Edits a team's registry entry: connection, ceremonies, bindings, status. Archived teams cannot be edited.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="Input">The new details.</param>
public sealed record UpdateTeamCommand(string TeamId, TeamInput Input) : ICommand;
