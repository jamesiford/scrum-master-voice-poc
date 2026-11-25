using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>One step of a connection test.</summary>
/// <param name="Name">The step: <c>organisation</c>, <c>tenant</c>, <c>project</c>, <c>team</c>, <c>iteration</c> or <c>roster</c>.</param>
/// <param name="Ok">Whether it passed.</param>
/// <param name="Detail">What was found, or why it failed.</param>
public sealed record ConnectionStep(string Name, bool Ok, string Detail);

/// <summary>The outcome of testing a team's Azure DevOps connection with the API's managed identity.</summary>
/// <param name="Steps">The steps that ran, in order. A failed step stops the test, so the last step is the one that failed.</param>
public sealed record ConnectionReport(IReadOnlyList<ConnectionStep> Steps)
{
    /// <summary>Gets a value indicating whether every step passed.</summary>
    public bool Ok => Steps.Count > 0 && Steps.All(s => s.Ok);

    /// <summary>Gets the first step that failed, or <see langword="null"/>.</summary>
    public ConnectionStep? Failed => Steps.FirstOrDefault(s => !s.Ok);
}

/// <summary>A project in an Azure DevOps organisation.</summary>
/// <param name="Name">Project name.</param>
public sealed record AdoProjectRef(string Name);

/// <summary>A team in an Azure DevOps project.</summary>
/// <param name="Name">Team name.</param>
/// <param name="DefaultAreaPath">The team's default area path, when known.</param>
public sealed record AdoTeamRef(string Name, string? DefaultAreaPath);

/// <summary>Reads an Azure DevOps organisation to test a team's connection and to fill the onboarding pickers.</summary>
public interface IAdoConnectionTester
{
    /// <summary>Runs the connection test: organisation reachable, in this tenant, project, team, current iteration, roster.</summary>
    /// <param name="ado">The connection to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Which steps passed and which failed.</returns>
    Task<ConnectionReport> TestAsync(AdoSettings ado, CancellationToken ct);

    /// <summary>Finds the Entra tenant that owns an organisation.</summary>
    /// <param name="organisationUrl">The organisation URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tenant id, or <see langword="null"/> when it could not be determined.</returns>
    Task<Guid?> GetOrganisationTenantAsync(string organisationUrl, CancellationToken ct);

    /// <summary>Lists the projects of an organisation.</summary>
    /// <param name="organisationUrl">The organisation URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The projects, or an empty list when it cannot be read.</returns>
    Task<IReadOnlyList<AdoProjectRef>> ListProjectsAsync(string organisationUrl, CancellationToken ct);

    /// <summary>Lists the teams of a project.</summary>
    /// <param name="organisationUrl">The organisation URL.</param>
    /// <param name="project">The project.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The teams, or an empty list when it cannot be read.</returns>
    Task<IReadOnlyList<AdoTeamRef>> ListTeamsAsync(string organisationUrl, string project, CancellationToken ct);
}
