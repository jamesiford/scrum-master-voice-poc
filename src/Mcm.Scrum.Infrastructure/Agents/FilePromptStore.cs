using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Demo;
using Mcm.Scrum.Infrastructure.Persistence;

namespace Mcm.Scrum.Infrastructure.Agents;

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

    private static string? PathFor(string name) =>
        name.Any(c => !char.IsLetterOrDigit(c) && c != '-')
            ? null
            : RepositoryFileLocator.Resolve(Path.Combine("agents", "instructions", name + ".md"));
}
