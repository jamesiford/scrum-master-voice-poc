using System.Net;
using System.Text.Json.Nodes;

namespace Mcm.Scrum.Tools.SeedAdo;

/// <summary>
/// Creates the demo team, area, current sprint and work items in a sandbox project. Idempotent: items are found by their
/// <c>demo-key-*</c> tag and reset in place, and the sprint dates are moved so the demo sprint is always current (SPEC 9.2).
/// </summary>
internal sealed class Seeder(AdoClient ado, SeedDefinition seed, Action<string> log)
{
    private const string DemoTag = "mcm-demo";
    private static readonly string[] BatchFields = ["System.Tags", "System.State"];
    private string _project = string.Empty;
    private string _areaPath = string.Empty;
    private string _iterationPath = string.Empty;

    public async Task RunAsync(CancellationToken ct)
    {
        _project = seed.Project;
        _areaPath = $"{seed.Project}\\{seed.AreaPath}";
        _iterationPath = $"{seed.Project}\\{seed.Iteration}";

        var me = await GetCurrentUserAsync(ct);
        log($"Signed in as {me}");
        await EnsureAreaAsync(ct);
        var iterationId = await EnsureIterationAsync(ct);
        await EnsureTeamAsync(iterationId, ct);
        await SeedWorkItemsAsync(me, ct);
        log("Done. Roster owners are applied through demo-owner-* tags; every assigned item is assigned to the signed-in user.");
    }

    private async Task<string> GetCurrentUserAsync(CancellationToken ct)
    {
        var data = await ado.GetAsync("_apis/connectionData?api-version=7.1-preview", ct);
        return data?["authenticatedUser"]?["properties"]?["Account"]?["$value"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Could not read the signed-in user from connectionData.");
    }

    private async Task EnsureAreaAsync(CancellationToken ct)
    {
        var path = $"{Esc(_project)}/_apis/wit/classificationnodes/areas";
        if (await ado.GetAsync($"{path}/{Esc(seed.AreaPath)}", ct, allowNotFound: true) is not null)
        {
            log($"Area '{_areaPath}' exists");
            return;
        }

        await ado.SendJsonAsync(HttpMethod.Post, path, new { name = seed.AreaPath }, ct);
        log($"Created area '{_areaPath}'");
    }

    private async Task<string> EnsureIterationAsync(CancellationToken ct)
    {
        var path = $"{Esc(_project)}/_apis/wit/classificationnodes/iterations";
        var start = DateTime.UtcNow.Date.AddDays(-seed.DaysElapsed);
        var attributes = new { startDate = start.ToString("o"), finishDate = start.AddDays(seed.SprintLengthDays - 1).ToString("o") };

        var existing = await ado.GetAsync($"{path}/{Esc(seed.Iteration)}", ct, allowNotFound: true);
        if (existing is null)
        {
            existing = await ado.SendJsonAsync(HttpMethod.Post, path, new { name = seed.Iteration, attributes }, ct);
            log($"Created iteration '{_iterationPath}'");
        }
        else
        {
            await ado.SendJsonAsync(HttpMethod.Patch, $"{path}/{Esc(seed.Iteration)}", new { attributes }, ct);
            log($"Moved iteration '{_iterationPath}' to {attributes.startDate[..10]} - {attributes.finishDate[..10]}");
        }

        return existing!["identifier"]!.GetValue<string>();
    }

    private async Task EnsureTeamAsync(string iterationId, CancellationToken ct)
    {
        var teams = $"_apis/projects/{Esc(_project)}/teams";
        if (await ado.GetAsync($"{teams}/{Esc(seed.Team)}", ct, allowNotFound: true) is null)
        {
            await ado.SendJsonAsync(HttpMethod.Post, teams, new { name = seed.Team, description = "MCM Scrum Agent demo team" }, ct);
            log($"Created team '{seed.Team}'");
        }

        var settings = $"{Esc(_project)}/{Esc(seed.Team)}/_apis/work/teamsettings";
        await ado.SendJsonAsync(
            HttpMethod.Patch,
            $"{settings}/teamfieldvalues",
            new { defaultValue = _areaPath, values = new[] { new { value = _areaPath, includeChildren = true } } },
            ct);

        // A new team's backlog iteration can point at nothing valid; set it to the project root before adding sprints.
        var root = await ado.GetAsync($"{Esc(_project)}/_apis/wit/classificationnodes/iterations?$depth=0", ct);
        await ado.SendJsonAsync(HttpMethod.Patch, settings, new { backlogIteration = root!["identifier"]!.GetValue<string>() }, ct);

        var (status, body) = await ado.TrySendJsonAsync(HttpMethod.Post, $"{settings}/iterations", new { id = iterationId }, ct);
        if (status is not (HttpStatusCode.OK or HttpStatusCode.Created) && !body.Contains("already", StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpRequestException($"Could not add the iteration to the team: {(int)status} {body}");
        }

        log("Team area and current iteration are configured");
    }

    private async Task SeedWorkItemsAsync(string me, CancellationToken ct)
    {
        var existing = await FindExistingAsync(ct);
        foreach (var item in seed.WorkItems)
        {
            var ops = BuildFields(item, me, existing.ContainsKey(item.KeyTag));
            if (existing.TryGetValue(item.KeyTag, out var found))
            {
                await ado.SendJsonAsync(HttpMethod.Patch, $"{Esc(_project)}/_apis/wit/workitems/{found.Id}", ops, ct, patch: true);
                if (!string.Equals(found.State, item.State, StringComparison.OrdinalIgnoreCase))
                {
                    await SetStateAsync(found.Id, item.State, ct);
                }

                log($"Updated #{found.Id} {item.Title}");
                continue;
            }

            var created = await ado.SendJsonAsync(HttpMethod.Post, $"{Esc(_project)}/_apis/wit/workitems/${Esc(item.Type)}", ops, ct, patch: true);
            var id = created!["id"]!.GetValue<int>();
            if (!string.Equals(item.State, "New", StringComparison.OrdinalIgnoreCase))
            {
                await SetStateAsync(id, item.State, ct);
            }

            log($"Created #{id} {item.Title}");
        }
    }

    private async Task SetStateAsync(int id, string state, CancellationToken ct) =>
        await ado.SendJsonAsync(
            HttpMethod.Patch,
            $"{Esc(_project)}/_apis/wit/workitems/{id}",
            new[] { new { op = "add", path = "/fields/System.State", value = state } },
            ct,
            patch: true);

    private List<object> BuildFields(SeedWorkItem item, string me, bool update)
    {
        var tags = new List<string> { DemoTag, item.KeyTag };
        if (item.Owner is not null)
        {
            tags.Add("demo-owner-" + item.Owner);
        }

        tags.AddRange(item.Tags);

        var ops = new List<object>
        {
            Add("System.Title", item.Title),
            Add("System.AreaPath", _areaPath),
            Add("System.IterationPath", _iterationPath),
            Add("System.Tags", string.Join("; ", tags)),
        };

        if (item.Owner is not null)
        {
            ops.Add(Add("System.AssignedTo", me));
        }
        else if (update)
        {
            ops.Add(Add("System.AssignedTo", string.Empty));
        }

        if (item.Points is { } points)
        {
            ops.Add(Add("Microsoft.VSTS.Scheduling.StoryPoints", points));
        }

        if (item.Remaining is { } remaining)
        {
            ops.Add(Add("Microsoft.VSTS.Scheduling.RemainingWork", remaining));
        }

        if (item.FinishInDays is { } days)
        {
            ops.Add(Add("Microsoft.VSTS.Scheduling.FinishDate", DateTime.UtcNow.Date.AddDays(days).ToString("o")));
        }

        return ops;
    }

    private async Task<Dictionary<string, (int Id, string State)>> FindExistingAsync(CancellationToken ct)
    {
        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{_project}' AND [System.Tags] CONTAINS '{DemoTag}'";
        var result = await ado.SendJsonAsync(HttpMethod.Post, $"{Esc(_project)}/_apis/wit/wiql", new { query = wiql }, ct);
        var ids = result!["workItems"]!.AsArray().Select(w => w!["id"]!.GetValue<int>()).ToArray();
        var map = new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase);
        if (ids.Length == 0)
        {
            return map;
        }

        var batch = await ado.SendJsonAsync(HttpMethod.Post, "_apis/wit/workitemsbatch", new { ids, fields = BatchFields }, ct);
        foreach (var wi in batch!["value"]!.AsArray())
        {
            var fields = wi!["fields"]!;
            var tag = (fields["System.Tags"]?.GetValue<string>() ?? string.Empty)
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(t => t.StartsWith("demo-key-", StringComparison.OrdinalIgnoreCase));
            if (tag is not null)
            {
                map[tag] = (wi["id"]!.GetValue<int>(), fields["System.State"]?.GetValue<string>() ?? string.Empty);
            }
        }

        return map;
    }

    private static object Add(string field, object value) => new { op = "add", path = "/fields/" + field, value };

    private static string Esc(string value) => Uri.EscapeDataString(value);
}
