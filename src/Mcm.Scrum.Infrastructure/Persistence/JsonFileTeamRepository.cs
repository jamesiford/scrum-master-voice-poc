using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Infrastructure.Ado;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Persistence;

/// <summary>Stores team settings in a JSON file; before the first save it serves the configured default team.</summary>
/// <remarks>PROD: <c>CosmosTeamRepository</c> behind the same port.</remarks>
public sealed class JsonFileTeamRepository : ITeamRepository, IDisposable
{
    private readonly string _path;
    private readonly ICollectionStore _store;
    private readonly Team _default;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="JsonFileTeamRepository"/> class.</summary>
    /// <param name="store">Store options.</param>
    /// <param name="defaults">Default team settings.</param>
    /// <param name="ado">Azure DevOps options (organisation URL).</param>
    public JsonFileTeamRepository(ICollectionStore store, IOptions<TeamDefaultsOptions> defaults, IOptions<AdoOptions> ado)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        ArgumentNullException.ThrowIfNull(defaults);
        ArgumentNullException.ThrowIfNull(ado);
        _path = "teams.json";
        var d = defaults.Value;
        _default = new Team(
            d.Id,
            d.Name,
            d.TimeZone,
            15,
            new AdoSettings(ado.Value.Org, d.Project, d.AdoTeam, d.AreaPath),
            new TeamBehaviours(),
            d.SprintGoal);
    }

    /// <inheritdoc />
    public async Task<Team?> GetAsync(string id, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var all = await _store.ReadAsync<Dictionary<string, Team>>(_path, () => [], ct);
            return all.TryGetValue(id, out var team) ? team : id == _default.Id ? _default : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(Team team, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(team);
        await _gate.WaitAsync(ct);
        try
        {
            var all = await _store.ReadAsync<Dictionary<string, Team>>(_path, () => [], ct);
            all[team.Id] = team;
            await _store.WriteAsync(_path, all, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
