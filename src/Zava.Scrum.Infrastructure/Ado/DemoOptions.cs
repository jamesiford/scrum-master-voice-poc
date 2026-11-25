namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>Binds <c>Demo__*</c> configuration (SPEC 17) plus demo file locations.</summary>
public sealed class DemoOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Demo";

    /// <summary>Gets or sets a value indicating whether <c>demo-age-N</c> and <c>demo-quiet-N</c> tags shift dates back (SPEC 9.2).</summary>
    public bool HonorAgeTags { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether <c>demo-owner-{key}</c> tags decide which cast member owns an item.</summary>
    public bool HonorOwnerTags { get; set; } = true;

    /// <summary>Gets or sets the demo backlog definition, also the source of the demo roster.</summary>
    public string SeedFile { get; set; } = "demo/seed-ado.json";

    /// <summary>Gets or sets the seed file for a team other than the default, by team id (ADR 0018). A team with no entry uses <see cref="SeedFile"/>.</summary>
    public Dictionary<string, string> TeamSeedFiles { get; set; } = [];

    /// <summary>Gets or sets the extra demo teams registered at start (Identity and Catalogue); Payments is the configured default team. Empty means Payments only.</summary>
    public List<DemoTeam> Teams { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether to register only Payments, for the film, even when <see cref="Teams"/> lists more.</summary>
    public bool PaymentsOnly { get; set; }

    /// <summary>
    /// Gets or sets the demo members' email addresses by member key (for example <c>daniel</c>), used as the Azure DevOps unique name when the seed file has none.
    /// Kept in configuration (user secrets or the azd environment) so no tenant identity is committed. They are who the sprint calendar invites.
    /// </summary>
    public Dictionary<string, string> MemberEmails { get; set; } = [];

    /// <summary>Gets or sets the KPI history fixture used when Azure DevOps has fewer than three past sprints (SPEC 9.5).</summary>
    public string KpiFixture { get; set; } = "demo/kpi-history.json";
}

/// <summary>One extra demo team: its Azure DevOps project, area and roster file. DEMO: production teams are registered through the registry API.</summary>
/// <param name="Id">Team id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Project">Azure DevOps project, in the organisation set by <c>Ado:Org</c>.</param>
/// <param name="AdoTeam">Azure DevOps team name.</param>
/// <param name="AreaPath">Area path.</param>
/// <param name="SprintGoal">Sprint goal.</param>
/// <param name="SeedFile">The seed file (<c>tools/SeedAdo</c>) that holds the roster.</param>
/// <param name="TimeZone">IANA time zone.</param>
public sealed record DemoTeam(string Id, string Name, string Project, string AdoTeam, string AreaPath, string SprintGoal, string SeedFile, string TimeZone = "UTC");
