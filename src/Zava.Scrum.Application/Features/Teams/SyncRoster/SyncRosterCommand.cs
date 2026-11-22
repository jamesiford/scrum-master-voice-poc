using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Teams.SyncRoster;

/// <summary>Refreshes the roster from its source, keeping local edits (active flag, pronunciation, order) for known members.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record SyncRosterCommand(string TeamId) : ICommand;
