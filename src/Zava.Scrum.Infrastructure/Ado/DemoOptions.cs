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

    /// <summary>
    /// Gets or sets the demo members' email addresses by member key (for example <c>daniel</c>), used as the Azure DevOps unique name when the seed file has none.
    /// Kept in configuration (user secrets or the azd environment) so no tenant identity is committed. They are who the sprint calendar invites.
    /// </summary>
    public Dictionary<string, string> MemberEmails { get; set; } = [];

    /// <summary>Gets or sets the KPI history fixture used when Azure DevOps has fewer than three past sprints (SPEC 9.5).</summary>
    public string KpiFixture { get; set; } = "demo/kpi-history.json";
}
