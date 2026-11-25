using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>
/// Tests a team's Azure DevOps connection step by step with the API's managed identity, and stops at the first failure so the report says
/// exactly what to fix. The owning tenant comes from the <c>X-VSS-ResourceTenant</c> header Azure DevOps puts on every response, even an
/// unauthenticated one, so a foreign organisation is recognised before any credential is sent to it.
/// </summary>
/// <remarks>VERIFY: the header was observed on an unauthenticated request to https://dev.azure.com/{org} (401 with X-VSS-ResourceTenant); it is not in a Learn reference page.</remarks>
internal sealed class AdoConnectionTester(IAdoClientFactory factory, IHttpClientFactory anonymous, ILogger<AdoConnectionTester> logger) : IAdoConnectionTester
{
    private const string Version = "api-version=7.1";

    /// <inheritdoc />
    public async Task<Guid?> GetOrganisationTenantAsync(string organisationUrl, CancellationToken ct)
    {
        var org = AdoOrganisation.Normalise(organisationUrl);
        if (org is null)
        {
            return null;
        }

        try
        {
            using var http = anonymous.CreateClient("ado-anonymous");
            using var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Head, org), ct);
            return response.Headers.TryGetValues("X-VSS-ResourceTenant", out var values) && Guid.TryParse(values.FirstOrDefault(), out var id) ? id : null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not read the tenant of {Organisation}", org);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ConnectionReport> TestAsync(AdoSettings ado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ado);
        var steps = new List<ConnectionStep>();
        var org = AdoOrganisation.Normalise(ado.Org);
        if (org is null)
        {
            return Report(steps, "organisation", false, "Not an Azure DevOps organisation URL.");
        }

        HttpClient http;
        try
        {
            http = factory.For(org);
            if (!await Probe(steps, "organisation", http, $"_apis/projects?$top=1&{Version}", $"{org} is reachable and the API's identity can read it.", ct))
            {
                return new ConnectionReport(steps);
            }

            var project = Uri.EscapeDataString(ado.Project);
            var team = Uri.EscapeDataString(ado.Team);
            if (!await Probe(steps, "project", http, $"_apis/projects/{project}?{Version}", $"Project '{ado.Project}' exists.", ct)
                || !await Probe(steps, "team", http, $"_apis/projects/{project}/teams/{team}?{Version}", $"Team '{ado.Team}' exists.", ct)
                || !await Probe(steps, "iteration", http, $"{project}/{team}/_apis/work/teamsettings/iterations?$timeframe=current&{Version}", "A current iteration was found.", ct, requireValue: true)
                || !await Probe(steps, "roster", http, $"_apis/projects/{project}/teams/{team}/members?$top=1&{Version}", "The team's members are readable.", ct))
            {
                return new ConnectionReport(steps);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Connection test for {Organisation} failed", org);
            steps.Add(new ConnectionStep(steps.Count == 0 ? "organisation" : steps[^1].Name, false, "Azure DevOps could not be reached or the credential was refused."));
        }

        return new ConnectionReport(steps);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdoProjectRef>> ListProjectsAsync(string organisationUrl, CancellationToken ct)
    {
        var org = AdoOrganisation.Normalise(organisationUrl);
        if (org is null)
        {
            return [];
        }

        using var doc = await ReadAsync(factory.For(org), $"_apis/projects?$top=500&{Version}", ct);
        return doc is null ? [] : doc.RootElement.GetProperty("value").EnumerateArray().Select(p => new AdoProjectRef(p.GetProperty("name").GetString() ?? string.Empty)).OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdoTeamRef>> ListTeamsAsync(string organisationUrl, string project, CancellationToken ct)
    {
        var org = AdoOrganisation.Normalise(organisationUrl);
        if (org is null || string.IsNullOrWhiteSpace(project))
        {
            return [];
        }

        using var doc = await ReadAsync(factory.For(org), $"_apis/projects/{Uri.EscapeDataString(project)}/teams?$top=200&{Version}", ct);
        return doc is null ? [] : doc.RootElement.GetProperty("value").EnumerateArray().Select(t => new AdoTeamRef(t.GetProperty("name").GetString() ?? string.Empty, null)).OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static ConnectionReport Report(List<ConnectionStep> steps, string name, bool ok, string detail)
    {
        steps.Add(new ConnectionStep(name, ok, detail));
        return new ConnectionReport(steps);
    }

    private static async Task<bool> Probe(List<ConnectionStep> steps, string name, HttpClient http, string path, string okDetail, CancellationToken ct, bool requireValue = false)
    {
        using var response = await http.GetAsync(path, ct);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NonAuthoritativeInformation)
        {
            steps.Add(new ConnectionStep(name, false, "Azure DevOps refused the API's managed identity. Add it to the organisation as a user (docs/DEPLOY.md)."));
            return false;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            steps.Add(new ConnectionStep(name, false, name == "iteration" ? "The team has no current iteration." : $"Not found ({name})."));
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            steps.Add(new ConnectionStep(name, false, $"Azure DevOps answered {(int)response.StatusCode}."));
            return false;
        }

        if (requireValue)
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("value", out var v) || v.GetArrayLength() == 0)
            {
                steps.Add(new ConnectionStep(name, false, "The team has no current iteration."));
                return false;
            }
        }

        steps.Add(new ConnectionStep(name, true, okDetail));
        return true;
    }

    private static async Task<JsonDocument?> ReadAsync(HttpClient http, string path, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(path, ct);
            return response.IsSuccessStatusCode ? JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            return null;
        }
    }
}
