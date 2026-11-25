using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Teams.ArchiveTeam;

/// <summary>Archives a team. Never a hard delete: its sessions, follow-ups and audit trail stay readable.</summary>
/// <param name="TeamId">The team.</param>
public sealed record ArchiveTeamCommand(string TeamId) : ICommand;
