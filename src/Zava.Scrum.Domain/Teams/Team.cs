namespace Zava.Scrum.Domain.Teams;

/// <summary>Team settings and registry entry (SPEC 15, <c>teams</c> container; ADR 0018).</summary>
/// <param name="Id">Team id.</param>
/// <param name="Name">Display name.</param>
/// <param name="TimeZone">IANA time zone.</param>
/// <param name="StandupMinutes">Standup length.</param>
/// <param name="Ado">The team's Azure DevOps connection: organisation, project, team name and area path. Every read and write for the team goes to this organisation.</param>
/// <param name="Behaviours">Rule thresholds.</param>
/// <param name="SprintGoal">Current sprint goal (Azure DevOps has no goal field, so it is kept here).</param>
/// <param name="RecapChannelKey">Teams conversation id of the channel the recap is posted to, captured when the bot is installed in the team (M9-R5).</param>
/// <param name="Status">Life cycle. Data saved before the registry existed loads as <see cref="TeamStatus.Active"/>.</param>
/// <param name="TeamsBindings">The Teams channels and chats bound to the team.</param>
/// <param name="Ceremonies">Which ceremonies the team runs.</param>
/// <param name="KnowledgeBaseIds">Knowledge bases the team uses (reserved for a later step; empty).</param>
/// <param name="WorkSignalsEnabled">Whether work signals feed the team (reserved for a later step; off).</param>
/// <param name="EntraGroupId">Optional Entra group whose members are synced into the team's memberships.</param>
public sealed record Team(
    string Id,
    string Name,
    string TimeZone,
    int StandupMinutes,
    AdoSettings Ado,
    TeamBehaviours Behaviours,
    string SprintGoal,
    string? RecapChannelKey = null,
    TeamStatus Status = TeamStatus.Active,
    IReadOnlyList<TeamsBinding>? TeamsBindings = null,
    CeremoniesEnabled? Ceremonies = null,
    IReadOnlyList<string>? KnowledgeBaseIds = null,
    bool WorkSignalsEnabled = false,
    string? EntraGroupId = null)
{
    /// <summary>Gets the bindings, never <see langword="null"/>.</summary>
    public IReadOnlyList<TeamsBinding> Bindings => TeamsBindings ?? [];

    /// <summary>Gets the ceremony flags, never <see langword="null"/>.</summary>
    public CeremoniesEnabled CeremonyFlags => Ceremonies ?? new CeremoniesEnabled();

    /// <summary>Gets a value indicating whether the scheduler may start this team's ceremonies.</summary>
    public bool IsSchedulable => Status == TeamStatus.Active;

    /// <summary>Gets a value indicating whether the team still accepts changes (not archived).</summary>
    public bool IsOpen => Status != TeamStatus.Archived;
}
