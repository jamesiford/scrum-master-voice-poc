using System.Text.Json.Nodes;

namespace Zava.Scrum.Tools.SeedAdo;

/// <summary>
/// Renames the existing demo project and its content from the old customer brand to the generic one (docs/rebrand, phase 5).
/// A dry run (the default) only reads and prints the table of changes; <c>--apply</c> writes them. It renames the project (the project id stays
/// the same, so links by id keep working), teams whose names contain a token, and the title, description, acceptance criteria and tags of
/// Epics, Features, User Stories, Tasks and Bugs. History, comments by people and attachments are not touched.
/// </summary>
internal sealed class Rebrander(AdoClient ado, string oldProject, string newProject, bool apply, Action<string> log)
{
    private static readonly string[] Types = ["Epic", "Feature", "User Story", "Task", "Bug"];
    private static readonly (string Field, string Label)[] Fields =
    [
        ("System.Title", "Title"),
        ("System.Description", "Description"),
        ("Microsoft.VSTS.Common.AcceptanceCriteria", "Acceptance criteria"),
        ("System.Tags", "Tags"),
    ];

    private readonly List<(string Id, string Field, string Old, string New)> _changes = [];

    public async Task RunAsync(CancellationToken ct)
    {
        log(apply ? "REBRAND: applying changes" : "REBRAND: dry run, nothing is written (use --apply to write)");
        var project = await RenameProjectAsync(ct);
        await RenameTeamsAsync(project, ct);
        await RebrandWorkItemsAsync(project, ct);
        PrintTable();
    }

    private async Task<string> RenameProjectAsync(CancellationToken ct)
    {
        var current = await ado.GetAsync($"_apis/projects/{Esc(oldProject)}", ct, allowNotFound: true);
        if (current is null)
        {
            var renamed = await ado.GetAsync($"_apis/projects/{Esc(newProject)}", ct, allowNotFound: true)
                ?? throw new InvalidOperationException($"Neither project '{oldProject}' nor '{newProject}' exists in this organisation.");
            log($"Project is already named '{newProject}'");
            return renamed["id"]!.GetValue<string>();
        }

        var id = current["id"]!.GetValue<string>();
        _changes.Add((id, "Project name", oldProject, newProject));
        if (!apply)
        {
            return id;
        }

        var operation = await ado.SendJsonAsync(HttpMethod.Patch, $"_apis/projects/{id}", new { name = newProject }, ct);
        await WaitAsync(operation!["id"]!.GetValue<string>(), ct);
        log($"Renamed project {id} to '{newProject}'");
        return id;
    }

    private async Task WaitAsync(string operationId, CancellationToken ct)
    {
        for (var i = 0; i < 60; i++)
        {
            var op = await ado.GetAsync($"_apis/operations/{operationId}", ct);
            switch (op!["status"]!.GetValue<string>())
            {
                case "succeeded":
                    return;
                case "failed" or "cancelled":
                    throw new InvalidOperationException($"The project rename operation {operationId} {op["status"]}.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        throw new InvalidOperationException($"The project rename operation {operationId} did not finish in two minutes.");
    }

    private async Task RenameTeamsAsync(string projectId, CancellationToken ct)
    {
        var teams = await ado.GetAsync($"_apis/projects/{projectId}/teams", ct);
        foreach (var team in teams!["value"]!.AsArray())
        {
            var name = team!["name"]!.GetValue<string>();
            if (!RebrandMap.NeedsChange(name))
            {
                continue;
            }

            var id = team["id"]!.GetValue<string>();
            var renamed = RebrandMap.Apply(name);
            _changes.Add((id, "Team name", name, renamed));
            if (apply)
            {
                await ado.SendJsonAsync(HttpMethod.Patch, $"_apis/projects/{projectId}/teams/{id}", new { name = renamed }, ct);
            }
        }
    }

    private async Task RebrandWorkItemsAsync(string projectId, CancellationToken ct)
    {
        // @project is the project of the project-scoped route, so the query works before and after the rename.
        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.WorkItemType] IN ({string.Join(", ", Types.Select(t => $"'{t}'"))})";
        var result = await ado.SendJsonAsync(HttpMethod.Post, $"{Esc(projectId)}/_apis/wit/wiql", new { query = wiql }, ct);
        var ids = result!["workItems"]!.AsArray().Select(w => w!["id"]!.GetValue<int>()).ToArray();
        log($"Checking {ids.Length} work items");
        foreach (var chunk in ids.Chunk(200))
        {
            var batch = await ado.SendJsonAsync(HttpMethod.Post, "_apis/wit/workitemsbatch", new { ids = chunk, fields = Fields.Select(f => f.Field).ToArray() }, ct);
            foreach (var item in batch!["value"]!.AsArray())
            {
                await RebrandItemAsync(projectId, item!, ct);
            }
        }
    }

    private async Task RebrandItemAsync(string projectId, JsonNode item, CancellationToken ct)
    {
        var id = item["id"]!.GetValue<int>();
        var ops = new List<object>();
        foreach (var (field, label) in Fields)
        {
            var value = item["fields"]![field]?.GetValue<string>();
            if (!RebrandMap.NeedsChange(value))
            {
                continue;
            }

            var changed = RebrandMap.Apply(value!);
            _changes.Add((id.ToString(System.Globalization.CultureInfo.InvariantCulture), label, value!, changed));
            ops.Add(new { op = "replace", path = "/fields/" + field, value = changed });
        }

        if (apply && ops.Count > 0)
        {
            await ado.SendJsonAsync(HttpMethod.Patch, $"{Esc(projectId)}/_apis/wit/workitems/{id}", ops, ct, patch: true);
        }
    }

    private void PrintTable()
    {
        log(string.Empty);
        log("| Id | Field | Old value | New value |");
        log("| --- | --- | --- | --- |");
        foreach (var (id, field, old, @new) in _changes)
        {
            log($"| {id} | {field} | {Clip(old)} | {Clip(@new)} |");
        }

        log($"{_changes.Count} change(s) {(apply ? "applied" : "planned")}.");
    }

    private static string Clip(string text)
    {
        var flat = text.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal);
        return flat.Length <= 120 ? flat : flat[..117] + "...";
    }

    private static string Esc(string value) => Uri.EscapeDataString(value);
}
