namespace Zava.Scrum.Domain.Teams;

/// <summary>Where a team's work lives in Azure DevOps.</summary>
/// <param name="Org">Organisation URL.</param>
/// <param name="Project">Project name.</param>
/// <param name="Team">Azure DevOps team name.</param>
/// <param name="AreaPath">Area path.</param>
public sealed record AdoSettings(string Org, string Project, string Team, string AreaPath);
