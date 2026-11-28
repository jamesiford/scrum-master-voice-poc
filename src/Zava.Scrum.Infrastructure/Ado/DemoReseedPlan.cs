using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Demo;
using Zava.Scrum.Infrastructure.Persistence;
using Zava.Scrum.Application.Features.Demo.OptInDemoScope;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>Reads existing tagged seed ids and builds proposals only. Unlike SeedAdo, this never writes classifications, projects, or items.</summary>
internal sealed class DemoReseedPlan(IAdoClientFactory clients, IOptions<DemoOptions> options, IOptions<AdoOptions> ado, IOptions<AzureOptions> azure,
    IOptions<TeamDefaultsOptions> defaults)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Builds the complete approved-reset payload, refusing missing/duplicate seed ids or mismatched configured scope.</summary>
    public async Task<Result<IReadOnlyList<ProposedChange>>> BuildAsync(Team team, Guid operation, DateTimeOffset now, string? approverUpn, CancellationToken ct)
    {
        var seed = await ReadSeedAsync(team.Id, ct);
        var area = $"{seed.Project}\\{seed.AreaPath}";
        if (string.IsNullOrWhiteSpace(approverUpn))
        {
            return Refused("The Admin's validated sign-in must carry a user principal name to restore sandbox assignments.");
        }
        if (!Guid.TryParse(azure.Value.TenantId, out var tenant) || tenant != options.Value.ResetTenantId
            || team.Ado.Org.TrimEnd('/') != ado.Value.Org.TrimEnd('/') || team.Ado.Project != seed.Project
            || team.Ado.Team != seed.Team || team.Ado.AreaPath != area || seed.NextIteration != "Sprint 43")
        {
            return Refused("The team does not exactly match the configured sandbox seed and Sprint 43.");
        }

        var http = clients.For(team.Ado.Org);
        using var iteration = await http.GetAsync($"{Uri.EscapeDataString(seed.Project)}/_apis/wit/classificationnodes/iterations/Sprint%2043?api-version=7.1", ct);
        if (!iteration.IsSuccessStatusCode)
        {
            return Refused("Sprint 43 must already exist. Re-seeding does not bypass approval to create or change ADO classifications.");
        }

        var query = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{Quote(seed.Project)}' AND [System.AreaPath] = '{Quote(area)}' AND [System.Tags] CONTAINS 'zava-demo'";
        using var response = await http.PostAsJsonAsync($"{Uri.EscapeDataString(seed.Project)}/_apis/wit/wiql?api-version=7.1", new { query }, ct);
        response.EnsureSuccessStatusCode();
        using var found = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var items = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var id in found.RootElement.GetProperty("workItems").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()))
        {
            using var item = await http.GetAsync($"_apis/wit/workitems/{id}?fields=System.Tags,System.TeamProject,System.AreaPath&api-version=7.1", ct);
            item.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await item.Content.ReadAsStringAsync(ct));
            var fields = doc.RootElement.GetProperty("fields");
            if (fields.GetProperty("System.TeamProject").GetString() != seed.Project || fields.GetProperty(AdoFields.AreaPath).GetString() != area)
            {
                return Refused("Azure DevOps returned an item outside the exact demo area.");
            }

            var tags = fields.GetProperty(AdoFields.Tags).GetString()!.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (!tags.Contains("zava-demo", StringComparer.Ordinal))
            {
                continue;
            }

            foreach (var tag in tags.Where(t => t.StartsWith("demo-key-", StringComparison.Ordinal)))
            {
                if (!items.TryGetValue(tag, out var ids))
                {
                    items[tag] = ids = [];
                }

                ids.Add(id);
            }
        }

        if (seed.WorkItems.Count == 0 || seed.WorkItems.Select(i => i.Key).Distinct(StringComparer.Ordinal).Count() != seed.WorkItems.Count
            || seed.WorkItems.Any(i => !items.TryGetValue("demo-key-" + i.Key, out var ids) || ids.Count != 1))
        {
            return Refused("Every seed key must resolve to exactly one existing tagged item. No ids are invented, created, or deleted.");
        }

        if (seed.WorkItems.Select(item => items["demo-key-" + item.Key][0]).Distinct().Count() != seed.WorkItems.Count)
        {
            return Refused("Different seed keys cannot alias the same work item.");
        }

        return Result<IReadOnlyList<ProposedChange>>.Success(seed.WorkItems.Select(item =>
        {
            var tags = new List<string> { "zava-demo", "demo-key-" + item.Key };
            if (item.Owner is not null)
            {
                tags.Add("demo-owner-" + item.Owner);
            }

            tags.AddRange(item.Tags);
            var fields = new Dictionary<string, string?>
            {
                [AdoFields.Title] = item.Title,
                [AdoFields.State] = item.State,
                [AdoFields.IterationPath] = item.Backlog ? seed.Project : $"{seed.Project}\\{seed.Iteration}",
                [AdoFields.Tags] = string.Join("; ", tags),
                [AdoFields.StoryPoints] = item.Points?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [AdoFields.RemainingWork] = item.Remaining?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [AdoFields.OriginalEstimate] = item.OriginalEstimate?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [AdoFields.AcceptanceCriteria] = item.AcceptanceCriteria,
                [AdoFields.FinishDate] = item.FinishInDays is { } days ? now.UtcDateTime.Date.AddDays(days).ToString("O", System.Globalization.CultureInfo.InvariantCulture) : null,
                [AdoFields.AssignedTo] = item.Owner is null ? null : approverUpn,
            };
            // As in SeedAdo, one actual sandbox user owns assigned items; cast ownership remains the demo-owner tag.

            var baseline = new DemoWorkItemReset(operation, seed.Project, area, "demo-key-" + item.Key, fields);
            return new ProposedChange(items[baseline.KeyTag][0], ChangeOp.ResetDemoItem, item.Title, "Restore the approved film seed baseline", DemoReset: baseline);
        }).ToList());
    }

    private static string Quote(string value) => value.Replace("'", "''", StringComparison.Ordinal);
    private static Result<IReadOnlyList<ProposedChange>> Refused(string message) =>
        Result<IReadOnlyList<ProposedChange>>.Failure(Error.Conflict("demo.seed_refused", message));

    /// <summary>Verifies only configured canonical seed identities and explicitly named debris in the same sandbox; performs no ADO writes.</summary>
    public async Task<Result> ValidateManifestAsync(DemoResetScopeManifest manifest, Guid actorTenant, CancellationToken ct)
    {
        if (manifest.CanonicalSeedTeamIds is null || manifest.TestDebrisTeamIds is null || manifest.Teams is null
            || manifest.TenantId == Guid.Empty || manifest.TenantId != actorTenant || actorTenant != options.Value.ResetTenantId
            || !Guid.TryParse(azure.Value.TenantId, out var tenant) || tenant != actorTenant
            || manifest.Organisation != ado.Value.Org || manifest.CanonicalSeedTeamIds.Count == 0)
        {
            return ManifestRefused();
        }

        var ids = manifest.CanonicalSeedTeamIds.Concat(manifest.TestDebrisTeamIds).ToList();
        if (ids.Count != ids.Distinct(StringComparer.Ordinal).Count() || ids.Any(id => !AdoOrganisation.IsValidTeamId(id))
            || !ids.ToHashSet(StringComparer.Ordinal).SetEquals(options.Value.ResetTeamIds)
            || manifest.Teams.Count != ids.Count || !manifest.Teams.Select(t => t.TeamId).ToHashSet(StringComparer.Ordinal).SetEquals(ids))
        {
            return ManifestRefused();
        }

        var known = options.Value.Teams.Select(t => t.Id).Append(defaults.Value.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var id in manifest.CanonicalSeedTeamIds)
        {
            if (!known.Contains(id))
            {
                return ManifestRefused();
            }

            var seed = await ReadSeedAsync(id, ct);
            var expected = new AdoSettings(ado.Value.Org, seed.Project, seed.Team, $"{seed.Project}\\{seed.AreaPath}");
            if (manifest.Teams.Single(t => t.TeamId == id).ExpectedAdo != expected)
            {
                return ManifestRefused();
            }
        }

        var sandboxConnections = manifest.Teams.Where(t => manifest.CanonicalSeedTeamIds.Contains(t.TeamId, StringComparer.Ordinal))
            .Select(t => t.ExpectedAdo).ToList();
        return manifest.TestDebrisTeamIds.All(id => !known.Contains(id)
                && sandboxConnections.Contains(manifest.Teams.Single(t => t.TeamId == id).ExpectedAdo))
            ? Result.Success() : ManifestRefused();
    }

    private static Result ManifestRefused() => Result.Failure(Error.Forbidden("demo.manifest_refused",
        "The manifest must match the configured tenant, sandbox, exact allowlist, verified canonical seeds and explicitly named test-debris fingerprints."));

    private async Task<Seed> ReadSeedAsync(string teamId, CancellationToken ct)
    {
        var file = options.Value.TeamSeedFiles.GetValueOrDefault(teamId)
            ?? options.Value.Teams.FirstOrDefault(t => t.Id == teamId)?.SeedFile ?? options.Value.SeedFile;
        var path = RepositoryFileLocator.Resolve(file) ?? throw new FileNotFoundException("Demo seed was not found.", file);
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Seed>(stream, Json, ct) ?? throw new InvalidOperationException("The demo seed is empty.");
    }

    private sealed record Seed(string Project, string Team, string AreaPath, string Iteration, string? NextIteration, List<Item> WorkItems);
    private sealed record Item(string Key, string Title, string State, double? Points, double? Remaining, string? Owner,
        int? FinishInDays, List<string> Tags, double? OriginalEstimate = null, bool Backlog = false, string? AcceptanceCriteria = null);
}
