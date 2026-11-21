namespace Mcm.Scrum.Domain.Teams;

/// <summary>Team settings (SPEC 15, <c>teams</c> container).</summary>
/// <param name="Id">Team id.</param>
/// <param name="Name">Display name.</param>
/// <param name="TimeZone">IANA time zone.</param>
/// <param name="StandupMinutes">Standup length.</param>
/// <param name="Ado">Azure DevOps location.</param>
/// <param name="Behaviours">Rule thresholds.</param>
/// <param name="SprintGoal">Current sprint goal (Azure DevOps has no goal field, so it is kept here).</param>
public sealed record Team(
    string Id,
    string Name,
    string TimeZone,
    int StandupMinutes,
    AdoSettings Ado,
    TeamBehaviours Behaviours,
    string SprintGoal);
