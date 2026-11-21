using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Teams;
using Microsoft.Extensions.Options;
using Mcm.Scrum.Infrastructure.Ado;

namespace Mcm.Scrum.Infrastructure.Demo;

/// <summary>Reads the demo cast from <c>demo/seed-ado.json</c>.</summary>
/// <remarks>
/// DEMO: the sandbox org has one real user, so the roster comes from the seed file instead of Azure DevOps team members.
/// PROD: read the team's members from Azure DevOps and map them to Entra identities.
/// </remarks>
public sealed class SeedFileRosterSource(IOptions<DemoOptions> options) : IRosterSource
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async Task<IReadOnlyList<Member>> GetRosterAsync(Team team, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(team);
        var path = RepositoryFileLocator.Resolve(options.Value.SeedFile)
            ?? throw new FileNotFoundException($"Seed file '{options.Value.SeedFile}' was not found.");
        await using var stream = File.OpenRead(path);
        var seed = await JsonSerializer.DeserializeAsync<SeedFile>(stream, Json, ct) ?? new SeedFile([]);
        return seed.Members
            .Select((m, i) => new Member($"m-{m.Key}", team.Id, m.Key, m.DisplayName, m.AdoUniqueName, m.Pronunciation ?? m.DisplayName, m.Order ?? i + 1, true))
            .ToList();
    }

    private sealed record SeedFile(List<SeedMember> Members);

    private sealed record SeedMember(string Key, string DisplayName, string? Pronunciation, int? Order, string? AdoUniqueName);
}
