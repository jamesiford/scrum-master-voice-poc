using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Infrastructure.Demo;
using Zava.Scrum.Infrastructure.Persistence;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>
/// Loads prompts from <c>agents/instructions/{name}.md</c>, re-read on every call so edits apply without a rebuild.
/// Portal edits are kept as overrides in the collection store so they survive container restarts.
/// </summary>
/// <remarks>PROD: prompts versioned in the repo and deployed with the image; overrides exist only for tuning during demos.</remarks>
public sealed class FilePromptStore(ICollectionStore store) : IPromptStore
{
    private const string OverridesName = "prompt-overrides.json";

    /// <inheritdoc />
    public async Task<string?> GetAsync(string name, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var path = PathFor(name);
        if (path is null)
        {
            return null;
        }

        var overrides = await store.ReadAsync(OverridesName, () => new Dictionary<string, string>(), ct);
        return overrides.TryGetValue(name, out var text) ? text : await File.ReadAllTextAsync(path, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListAsync(CancellationToken ct)
    {
        var probe = RepositoryFileLocator.Resolve(Path.Combine("agents", "instructions", "facilitator.md"));
        IReadOnlyList<string> names = probe is null
            ? []
            : Directory.EnumerateFiles(Path.GetDirectoryName(probe)!, "*.md").Select(Path.GetFileNameWithoutExtension).OfType<string>().Order().ToList();
        return Task.FromResult(names);
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync(string name, string text, CancellationToken ct)
    {
        var path = PathFor(name);
        if (path is null)
        {
            return false;
        }

        var overrides = await store.ReadAsync(OverridesName, () => new Dictionary<string, string>(), ct);
        overrides[name] = text;
        await store.WriteAsync(OverridesName, overrides, ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<PromptResolution?> ResolveAsync(string teamId, string name, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        var path = PathFor(name);
        if (path is null || !IsSafeTeam(teamId))
        {
            return null;
        }

        var overrides = await store.ReadAsync(OverridesName, () => new Dictionary<string, string>(), ct);
        if (overrides.TryGetValue(TeamKey(teamId, name), out var own))
        {
            return new PromptResolution(own, PromptSource.TeamOverride);
        }

        return overrides.TryGetValue(name, out var wide)
            ? new PromptResolution(wide, PromptSource.DeploymentOverride)
            : new PromptResolution(await File.ReadAllTextAsync(path, ct), PromptSource.Default);
    }

    /// <inheritdoc />
    public async Task<bool> SaveForTeamAsync(string teamId, string name, string text, CancellationToken ct)
    {
        if (PathFor(name) is null || !IsSafeTeam(teamId))
        {
            return false;
        }

        var overrides = await store.ReadAsync(OverridesName, () => new Dictionary<string, string>(), ct);
        overrides[TeamKey(teamId, name)] = text;
        await store.WriteAsync(OverridesName, overrides, ct);
        return true;
    }

    private static string TeamKey(string teamId, string name) => $"{teamId}/{name}";

    private static bool IsSafeTeam(string teamId) => teamId.All(c => char.IsLetterOrDigit(c) || c == '-');

    private static string? PathFor(string name) =>
        name.Any(c => !char.IsLetterOrDigit(c) && c != '-')
            ? null
            : RepositoryFileLocator.Resolve(Path.Combine("agents", "instructions", name + ".md"));
}
