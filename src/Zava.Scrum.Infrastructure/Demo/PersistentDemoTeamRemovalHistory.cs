using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Demo;

/// <summary>Reads only the publication store so startup never constructs external reset/ADO dependencies for removal history.</summary>
internal sealed class PersistentDemoTeamRemovalHistory(IDemoStateStore publication) : IDemoTeamRemovalHistory
{
    /// <inheritdoc />
    public async Task<bool> IsRemovedTeamAsync(string teamId, CancellationToken ct)
    {
        var state = await publication.ReadAsync(ct);
        var registry = state.Collections.TryGetValue("teams.json", out var json)
            ? json.Deserialize<Dictionary<string, Team>>(JsonFile.SerializerOptions) ?? [] : [];
        return !registry.ContainsKey(teamId)
            && state.Operations.Values.Any(operation => operation.Completed && !operation.Aborted
                && operation.Target.Id != teamId && operation.Scope.Any(team => team.Id == teamId));
    }
}
