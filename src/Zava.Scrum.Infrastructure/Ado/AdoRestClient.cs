using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>Reads sprint data from Azure DevOps over REST (SPEC 9.3). Read-only; writes arrive in M3 behind the approval guard.</summary>
/// <remarks>VERIFY: https://learn.microsoft.com/azure/devops/integrate/previous-apis/work-item-tracking (api-version 7.1).</remarks>
internal sealed class AdoRestClient(HttpClient http, IOptions<DemoOptions> demo, ILogger<AdoRestClient> logger) : IWorkItemReader, IIterationReader, IBacklogReader
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
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            return Result<SprintSnapshot>.Failure(ToError(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<WorkItem>>> GetBacklogAsync(Team team, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(team);
        try
        {
            var project = team.Ado.Project;
            // The backlog is what sits in the project's root iteration (no sprint yet) under the team's area, in backlog order.
            var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{Escape(project)}' " +
                       $"AND [System.AreaPath] UNDER '{Escape(team.Ado.AreaPath)}' AND [System.IterationPath] = '{Escape(project)}' " +
                       "AND [System.WorkItemType] IN ('User Story','Product Backlog Item','Bug') AND [System.State] NOT IN ('Closed','Done','Removed','Resolved') " +
                       $"ORDER BY [{AdoFields.StackRank}] ASC, [System.Id] ASC";
            using var response = await http.PostAsJsonAsync($"{Uri.EscapeDataString(project)}/_apis/wit/wiql?{ApiVersion}", new { query = wiql }, ct);
            response.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var ids = doc.RootElement.GetProperty("workItems").EnumerateArray().Select(w => w.GetProperty("id").GetInt32()).ToList();
            var items = new List<WorkItem>();
            foreach (var chunk in ids.Chunk(BatchSize))
            {
                items.AddRange(await GetItemsAsync(chunk, ct));
            }

            // The batch call does not keep the query's order, and backlog order is the point.
            var rank = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
            IReadOnlyList<WorkItem> ordered = [.. items.OrderBy(i => rank[i.Id.Value])];
            return Result<IReadOnlyList<WorkItem>>.Success(ordered);
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            return Result<IReadOnlyList<WorkItem>>.Failure(ToError(ex));
        }
    }

    private static DateTimeOffset? StartOf(JsonElement iteration) =>
        iteration.TryGetProperty("attributes", out var a) && a.ValueKind == JsonValueKind.Object && a.TryGetProperty("startDate", out var s) && s.ValueKind == JsonValueKind.String
            ? s.GetDateTimeOffset()
            : null;

    private Error ToError(Exception ex)
    {
        if (ex is HttpRequestException http)
        {
            logger.LogWarning(ex, "Azure DevOps request failed ({Status})", http.StatusCode);
            var denied = http.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            return Error.ExternalRejected(
                denied ? "ado.unauthorized" : "ado.unreachable",
                denied
                    ? "Azure DevOps rejected the credential. Run 'az login' for the tenant that owns the organisation."
                    : $"Azure DevOps request failed ({http.StatusCode?.ToString() ?? http.Message}).");
        }

        logger.LogWarning(ex, "Could not get an Azure DevOps token");
        return Error.ExternalRejected("ado.auth_failed", "No Azure credential available. Run 'az login'.");
    }

    /// <inheritdoc />
    public async Task<string?> GetNextIterationPathAsync(Team team, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(team);
        try
        {
            var teamPath = $"{Uri.EscapeDataString(team.Ado.Project)}/{Uri.EscapeDataString(team.Ado.Team)}";
            // The iterations endpoint only filters on "current", so list them all and take the earliest one that has not started yet.
            using var doc = await GetJsonAsync($"{teamPath}/_apis/work/teamsettings/iterations?{ApiVersion}", ct);
            var now = DateTimeOffset.UtcNow;
            return doc.RootElement.GetProperty("value").EnumerateArray()
                .Select(i => (Path: i.GetProperty("path").GetString(), Start: StartOf(i)))
                .Where(i => i.Start is { } s && s > now)
                .OrderBy(i => i.Start)
                .Select(i => i.Path)
                .FirstOrDefault();
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Could not read the next iteration");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IterationInfo?> FindIterationAsync(Team team, string? name, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(team);
        try
        {
            var teamPath = $"{Uri.EscapeDataString(team.Ado.Project)}/{Uri.EscapeDataString(team.Ado.Team)}";
            using var doc = await GetJsonAsync($"{teamPath}/_apis/work/teamsettings/iterations?{ApiVersion}", ct);
            var all = doc.RootElement.GetProperty("value").EnumerateArray()
                .Select(i => (Path: i.GetProperty("path").GetString()!, Start: DateOf(i, "startDate"), End: DateOf(i, "finishDate")))
                .Where(i => i.Start is not null && i.End is not null)
                .Select(i => new IterationInfo(i.Path, i.Start!.Value, i.End!.Value))
                .OrderBy(i => i.Start)
                .ToList();
            if (name is not null)
            {
                return all.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Path, name, StringComparison.OrdinalIgnoreCase));
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return all.FirstOrDefault(i => i.Start > today) ?? all.FirstOrDefault(i => i.Start <= today && i.End >= today);
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Could not read the iteration dates");
            return null;
        }
    }

    // Iteration dates have no time of day; Azure DevOps sends them as UTC midnight, so the UTC date is the date.
    private static DateOnly? DateOf(JsonElement iteration, string field) =>
        iteration.TryGetProperty("attributes", out var a) && a.ValueKind == JsonValueKind.Object && a.TryGetProperty(field, out var s) && s.ValueKind == JsonValueKind.String
            ? DateOnly.FromDateTime(s.GetDateTimeOffset().UtcDateTime)
            : null;

    private async Task<Result<SprintSnapshot>> ReadSprintAsync(Team team, CancellationToken ct)
    {
        var teamPath = $"{Uri.EscapeDataString(team.Ado.Project)}/{Uri.EscapeDataString(team.Ado.Team)}";
        var iteration = await GetCurrentIterationAsync(teamPath, ct);
        if (iteration is null)
        {
            return Result<SprintSnapshot>.Failure(Error.NotFound("ado.no_current_iteration", $"Team '{team.Ado.Team}' has no current iteration."));
        }

        var (path, start, finish) = iteration.Value;
        var ids = await QueryIdsAsync(teamPath, team.Ado.Project, team.Ado.AreaPath, path, ct);
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

    private async Task<List<int>> QueryIdsAsync(string teamPath, string project, string areaPath, string iterationPath, CancellationToken ct)
    {
        // Deviation from SPEC 9.3: no State filter except Removed, because burndown needs completed items too.
        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{Escape(project)}' " +
                   $"AND [System.AreaPath] UNDER '{Escape(areaPath)}' " +
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
