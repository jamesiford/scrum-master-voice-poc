using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Ado;

/// <summary>Reads sprint data from Azure DevOps over REST (SPEC 9.3). Read-only; writes arrive in M3 behind the approval guard.</summary>
/// <remarks>VERIFY: https://learn.microsoft.com/azure/devops/integrate/previous-apis/work-item-tracking (api-version 7.1).</remarks>
public sealed class AdoRestClient(HttpClient http, IOptions<DemoOptions> demo, ILogger<AdoRestClient> logger) : IWorkItemReader
{
    private const string ApiVersion = "api-version=7.1";
    private const int BatchSize = 200;

    /// <inheritdoc />
    public async Task<Result<SprintSnapshot>> GetCurrentSprintAsync(Team team, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(team);
        try
        {
            return await ReadSprintAsync(team, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Azure DevOps request failed ({Status})", ex.StatusCode);
            return Result<SprintSnapshot>.Failure(Error.ExternalRejected(
                ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ? "ado.unauthorized" : "ado.unreachable",
                ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? "Azure DevOps rejected the credential. Run 'az login' for the tenant that owns the organisation."
                    : $"Azure DevOps request failed ({ex.StatusCode?.ToString() ?? ex.Message})."));
        }
        catch (Exception ex) when (ex is Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Could not get an Azure DevOps token");
            return Result<SprintSnapshot>.Failure(Error.ExternalRejected("ado.auth_failed", "No Azure credential available. Run 'az login'."));
        }
    }

    private async Task<Result<SprintSnapshot>> ReadSprintAsync(Team team, CancellationToken ct)
    {
        var teamPath = $"{Uri.EscapeDataString(team.Ado.Project)}/{Uri.EscapeDataString(team.Ado.Team)}";
        var iteration = await GetCurrentIterationAsync(teamPath, ct);
        if (iteration is null)
        {
            return Result<SprintSnapshot>.Failure(Error.NotFound("ado.no_current_iteration", $"Team '{team.Ado.Team}' has no current iteration."));
        }

        var (path, start, finish) = iteration.Value;
        var ids = await QueryIdsAsync(teamPath, team.Ado.Project, path, ct);
        var items = new List<WorkItem>();
        foreach (var chunk in ids.Chunk(BatchSize))
        {
            items.AddRange(await GetItemsAsync(chunk, ct));
        }

        return Result<SprintSnapshot>.Success(new SprintSnapshot(path, start, finish, items));
    }

    private async Task<(string Path, DateTimeOffset Start, DateTimeOffset Finish)?> GetCurrentIterationAsync(string teamPath, CancellationToken ct)
    {
        using var doc = await GetJsonAsync($"{teamPath}/_apis/work/teamsettings/iterations?$timeframe=current&{ApiVersion}", ct);
        var first = doc.RootElement.GetProperty("value").EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var attributes = first.GetProperty("attributes");
        return (
            first.GetProperty("path").GetString()!,
            attributes.GetProperty("startDate").GetDateTimeOffset(),
            attributes.GetProperty("finishDate").GetDateTimeOffset());
    }

    private async Task<List<int>> QueryIdsAsync(string teamPath, string project, string iterationPath, CancellationToken ct)
    {
        // Deviation from SPEC 9.3: no State filter except Removed, because burndown needs completed items too.
        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{Escape(project)}' " +
                   $"AND [System.IterationPath] = '{Escape(iterationPath)}' " +
                   "AND [System.WorkItemType] IN ('User Story','Product Backlog Item','Bug','Task') AND [System.State] <> 'Removed'";
        using var response = await http.PostAsJsonAsync($"{teamPath}/_apis/wit/wiql?{ApiVersion}", new { query = wiql }, ct);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.GetProperty("workItems").EnumerateArray().Select(w => w.GetProperty("id").GetInt32()).ToList();
    }

    private async Task<List<WorkItem>> GetItemsAsync(int[] ids, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync($"_apis/wit/workitemsbatch?{ApiVersion}", new { ids, fields = AdoFields.SprintItemFields }, ct);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.GetProperty("value").EnumerateArray().Select(i => AdoWorkItemMapper.Map(i, demo.Value)).ToList();
    }

    private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken ct)
    {
        using var response = await http.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
