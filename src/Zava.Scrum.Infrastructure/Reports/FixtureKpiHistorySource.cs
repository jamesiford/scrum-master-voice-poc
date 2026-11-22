using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Infrastructure.Ado;
using Zava.Scrum.Infrastructure.Demo;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Reports;

/// <summary>Reads sprint history from <c>demo/kpi-history.json</c> and labels it illustrative (SPEC 9.5).</summary>
/// <remarks>PROD: read Azure DevOps Analytics (<c>WorkItemSnapshot</c>, <c>Iterations</c>) when the project has at least three past sprints. The demo project is new, so the fixture is used.</remarks>
public sealed class FixtureKpiHistorySource(IOptions<DemoOptions> options) : IKpiHistorySource
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<KpiHistory> GetAsync(string teamId, CancellationToken ct)
    {
        var path = RepositoryFileLocator.Resolve(options.Value.KpiFixture);
        if (path is null)
        {
            return new KpiHistory([], true);
        }

        await using var stream = File.OpenRead(path);
        var file = await JsonSerializer.DeserializeAsync<FixtureFile>(stream, Json, ct);
        return new KpiHistory(file?.Sprints ?? [], file?.Illustrative ?? true);
    }

    private sealed record FixtureFile(bool Illustrative, List<SprintHistory> Sprints);
}
