using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Teams.UpdateTeamSettings;

/// <summary>Replaces a team's editable settings (portal Team page).</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Name">Display name.</param>
/// <param name="TimeZone">IANA time zone.</param>
/// <param name="StandupMinutes">Standup length.</param>
/// <param name="Project">Azure DevOps project.</param>
/// <param name="AdoTeam">Azure DevOps team.</param>
/// <param name="AreaPath">Area path.</param>
/// <param name="StaleDays">Days in state before an item is stale.</param>
/// <param name="EscalateBlockerAfterDays">Days before a blocker is escalated.</param>
/// <param name="SprintGoal">Current sprint goal.</param>
public sealed record UpdateTeamSettingsCommand(
    string TeamId,
    string Name,
    string TimeZone,
    int StandupMinutes,
    string Project,
    string AdoTeam,
    string AreaPath,
    int StaleDays,
    int EscalateBlockerAfterDays,
    string SprintGoal) : ICommand;
