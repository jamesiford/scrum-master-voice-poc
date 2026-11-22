using System.ComponentModel.DataAnnotations;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Binds <c>Team__*</c>: the team that exists before anyone edits it. DEMO: one team; PROD: teams are created in the portal.</summary>
public sealed class TeamDefaultsOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Team";

    /// <summary>Gets or sets the team id.</summary>
    [Required]
    public string Id { get; set; } = "team-payments";

    /// <summary>Gets or sets the display name.</summary>
    [Required]
    public string Name { get; set; } = "Payments (Demo)";

    /// <summary>Gets or sets the IANA time zone.</summary>
    [Required]
    public string TimeZone { get; set; } = "America/Los_Angeles";

    /// <summary>Gets or sets the Azure DevOps project.</summary>
    [Required]
    public string Project { get; set; } = "Zava-Scrum-Demo";

    /// <summary>Gets or sets the Azure DevOps team.</summary>
    [Required]
    public string AdoTeam { get; set; } = "Payments (Demo)";

    /// <summary>Gets or sets the area path.</summary>
    [Required]
    public string AreaPath { get; set; } = "Zava-Scrum-Demo\\Payments";

    /// <summary>Gets or sets the sprint goal (Azure DevOps has no sprint goal field).</summary>
    public string SprintGoal { get; set; } = string.Empty;
}
