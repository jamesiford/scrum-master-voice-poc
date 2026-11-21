using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Demo;

namespace Mcm.Scrum.Infrastructure.Agents;

/// <summary>Loads prompts from <c>agents/instructions/{name}.md</c>, re-read on every call so edits apply without a rebuild.</summary>
/// <remarks>PROD: prompts versioned in the repo and deployed with the image; portal edits are for tuning during demos and are lost when the container restarts.</remarks>
public sealed class FilePromptStore : IPromptStore
{
    /// <inheritdoc />
    public async Task<string?> GetAsync(string name, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var path = PathFor(name);
        return path is null ? null : await File.ReadAllTextAsync(path, ct);
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

        await File.WriteAllTextAsync(path, text, ct);
        return true;
    }

    private static string? PathFor(string name) =>
        name.Any(c => !char.IsLetterOrDigit(c) && c != '-')
            ? null
            : RepositoryFileLocator.Resolve(Path.Combine("agents", "instructions", name + ".md"));
}