using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams;

/// <summary>What an Admin enters to register or edit a team.</summary>
/// <param name="Name">Display name.</param>
/// <param name="TimeZone">IANA time zone.</param>
/// <param name="StandupMinutes">Standup length.</param>
/// <param name="Ado">The Azure DevOps organisation, project, team and area path.</param>
/// <param name="SprintGoal">Current sprint goal.</param>
/// <param name="Ceremonies">Which ceremonies run.</param>
/// <param name="Status">The status to set; <see langword="null"/> keeps it (a new team is a draft).</param>
/// <param name="TeamsBindings">The bound Teams conversations; <see langword="null"/> keeps them.</param>
/// <param name="EntraGroupId">Optional Entra group to sync memberships from.</param>
public sealed record TeamInput(
    string Name,
    string TimeZone,
    int StandupMinutes,
    AdoSettings Ado,
    string SprintGoal,
    CeremoniesEnabled? Ceremonies = null,
    TeamStatus? Status = null,
    IReadOnlyList<TeamsBinding>? TeamsBindings = null,
    string? EntraGroupId = null);
